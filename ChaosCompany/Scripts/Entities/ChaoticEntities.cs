using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.Managers;
using Unity.Netcode;
using UnityEngine;

namespace ChaosCompany.Scripts.Entities;

public class ChaoticEntities : Chaotic
{
    public EnemyAI? EnemyAI { get; protected set; }

    protected string kindString;
    string[]? exclusion;

    public bool Inside { get; protected set; }

    public ChaoticEntities(RoundManager roundManager, bool inside, string[]? exclusion) : base(roundManager)
    {
        this.exclusion = exclusion;

        Inside = inside;
        kindString = Inside ? "inside" : "outside";
    }

    public override Chaotic? Spawn()
    {
        Vector3 position;
        float y;

        if (Inside)
        {
            int allEnemyVentsLength = RoundManager.allEnemyVents.Length;

            if (allEnemyVentsLength == 0)
            {
#if DEBUG
                Plugin.Logger.LogError("Cannot spawn enemy on the vent because there is no vent available");
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
                Plugin.Logger.LogError("Cannot spawn enemy outside, no enemy list available");
                return null;
            }

            SpawnableEnemyWithRarity outsideEnemyToSpawn = RoundManager.currentLevel.OutsideEnemies[Random.Range(0, outsideEnemiesCount)];

            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");


            int spawnPointLength = spawnPoints.Length;

            if (spawnPointLength == 0)
            {
                Plugin.Logger.LogError("Cannot chaotic outside, no spawn point available");
                return null;
            }

            Vector3 spawnPosition = spawnPoints[Random.Range(0, spawnPointLength)].transform.position;

            y = 0;

            position = RoundManager.GetRandomNavMeshPositionInBoxPredictable(spawnPosition, 10f, default, RoundManager.AnomalyRandom, RoundManager.GetLayermaskForEnemySizeLimit(outsideEnemyToSpawn.enemyType));
            position = RoundManager.PositionWithDenialPointsChecked(position, spawnPoints, outsideEnemyToSpawn.enemyType);
        }

        (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = GameManager.SpawnRandomEnemy(RoundManager, inside: Inside, position, yRotation: y, exclusion);

        if (enemySpawnedType == null || networkObjectReference == null)
        {
            return null;
        }

        if (networkObjectReference == null)
        {
            Plugin.Logger.LogError("Error networkObjectReference == null when trying to spawn chaotic entity");
            return null;
        }

        NetworkObject = networkObjectReference;
        EnemyAI = NetworkObject.gameObject.GetComponent<EnemyAI>();

        return this;
    }
}
