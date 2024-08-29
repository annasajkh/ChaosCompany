using ChaosCompany.Scripts.Patches;
using Unity.Netcode;
using UnityEngine;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Entities;

public class ChaoticEnemy
{
    public RoundManager RoundManager { get; private set; }
    public bool Inside { get; private set; }
    public NetworkObject? NetworkObject { get; private set; }
    public bool Dead { get; private set; }
    string kindString;

    Timer changeType = new(waitTime: 10, oneshot: false);
    bool isChaoticEnemyAlreadyTryingToChange;

    public ChaoticEnemy(RoundManager roundManager, bool inside)
    {
        Inside = inside;
        kindString = Inside ? "inside" : "outside";
        RoundManager = roundManager;

        changeType.OnTimeout += () =>
        {
            if (isChaoticEnemyAlreadyTryingToChange)
            {
                isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
                return;
            }

            if (NetworkObject is null)
            {
                return;
            }

            if (NetworkObject.gameObject.GetComponent<EnemyAI>().isEnemyDead)
            {
                Plugin.Logger.LogError($"An {kindString} chaotic enemy died as {NetworkObject.gameObject.GetComponent<EnemyAI>().enemyType.enemyName}");
                Dead = true;
                changeType.Stop();
                changeType.Finished = true;
                return;
            }

            if (NetworkObject.gameObject is null)
            {
                Plugin.Logger.LogError("A chaotic enemy gameObject is null");
                return;
            }

            Plugin.Logger.LogError($"An {kindString} chaotic enemy is changing");

            var enemyAIComponent = NetworkObject.gameObject.GetComponent<EnemyAI>();
            if (enemyAIComponent is null)
            {
                Plugin.Logger.LogError("A chaotic enemy does not have an EnemyAI component");
                return;
            }

            NetworkObjectReference? networkObjectReference;

            networkObjectReference = RoundManagerPatch.SwitchToRandomEnemyType(enemyAIComponent, inside: Inside, NetworkObject);

            if (networkObjectReference is not null)
            {
                NetworkObject = networkObjectReference.GetValueOrDefault();
            }
            else
            {
                Plugin.Logger.LogError("networkObjectReference is null after switching enemy type");
            }

            isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
        };

        RoundManagerPatch.Timers.Add(changeType);
    }

    public ChaoticEnemy? Spawn()
    {
        Vector3 position;
        float y;

        if (Inside)
        {
            int allEnemyVentsLength = RoundManager.allEnemyVents.Length;

            if (allEnemyVentsLength == 0)
            {
#if DEBUG
                Plugin.Logger.LogError("Cannot spawn chaotic enemy on the vent because there is no vent available");
#endif
                return null;
            }

            EnemyVent ventToSpawnEnemy = RoundManager.allEnemyVents[Random.Range(0, allEnemyVentsLength)];

            position = ventToSpawnEnemy.floorNode.position;
            y = ventToSpawnEnemy.floorNode.eulerAngles.y;

            ventToSpawnEnemy.OpenVentClientRpc();
        }
        else
        {
            int outsideEnemiesCount = RoundManager.currentLevel.OutsideEnemies.Count;

            if (outsideEnemiesCount == 0)
            {
                Plugin.Logger.LogError("Cannot spawn chaotic enemy outside, no enemy list available");
                return null;
            }

            SpawnableEnemyWithRarity outsideEnemyToSpawn = RoundManager.currentLevel.OutsideEnemies[Random.Range(0, outsideEnemiesCount)];

            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");


            int spawnPointLength = spawnPoints.Length;

            if (spawnPointLength == 0)
            {
                Plugin.Logger.LogError("Cannot chaotic enemy outside, no spawn point available");
                return null;
            }

            Vector3 spawnPosition = spawnPoints[Random.Range(0, spawnPointLength)].transform.position;

            y = 0;

            position = RoundManager.GetRandomNavMeshPositionInBoxPredictable(spawnPosition, 10f, default, RoundManager.AnomalyRandom, RoundManager.GetLayermaskForEnemySizeLimit(outsideEnemyToSpawn.enemyType));
            position = RoundManager.PositionWithDenialPointsChecked(position, spawnPoints, outsideEnemyToSpawn.enemyType);
        }

        (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = RoundManagerPatch.SpawnRandomEnemy(RoundManager, inside: Inside, position, yRotation: y, exclusion: ["DressGirl", "Nutcracker", "Spider", "double", "Red"]);

        if (enemySpawnedType is null || networkObjectReference is null)
        {
            return null;
        }

#if DEBUG
        Plugin.Logger.LogError($"Spawning {kindString} chaotic enemy");
#endif

        NetworkObject = networkObjectReference.GetValueOrDefault();
        changeType.Start();

        return this;
    }
}
