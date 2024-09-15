using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.Managers;
using Unity.Netcode;
using UnityEngine;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Items;

public class ChaoticItem : Chaotic
{
    Timer changeType;

    public ChaoticItem(RoundManager roundManager) : base(roundManager)
    {
        changeType = new(waitTime: 1, oneshot: false);
        changeType.OnTimeout += () =>
        {
            if (NetworkObject is null)
            {
                return;
            }

            NetworkObject = GameManager.SwitchToRandomItemType(roundManager, NetworkObject);

            if (NetworkObject is null)
            {
#if DEBUG
                Plugin.Logger.LogError("A chaotic item has been pickup by a player");
#endif
                changeType.Stop();
                changeType.Finished = true;
                ItsJoever = true;
            }
        };

        GameManager.Timers.Add(changeType);
    }

    public override void Update()
    {
        if (NetworkObject is null)
        {
            return;
        }

        if (NetworkObject.gameObject.GetComponent<GrabbableObject>().isHeld)
        {
#if DEBUG
            Plugin.Logger.LogError("A chaotic item has been pickup by a player");
#endif
            changeType.Stop();
            changeType.Finished = true;
            ItsJoever = true;
        }
    }

    public override Chaotic? Spawn()
    {
        if (RoundManager.currentLevel.spawnableScrap.Count == 0)
        {
            Plugin.Logger.LogError("No spawnable scrap in the level");
            return null;
        }

        SpawnableItemWithRarity spawnableItemWithRarity = RoundManager.currentLevel.spawnableScrap[Random.Range(0, RoundManager.currentLevel.spawnableScrap.Count)];

        RandomScrapSpawn[] randomScrapSpawnArray = Object.FindObjectsOfType<RandomScrapSpawn>();

        if (randomScrapSpawnArray.Length == 0)
        {
            Plugin.Logger.LogError("randomScrapSpawnArray is empty");
            return null;
        }

        RandomScrapSpawn randomScrapSpawn = randomScrapSpawnArray[Random.Range(0, randomScrapSpawnArray.Length)];

        Vector3 position = RoundManager.GetRandomNavMeshPositionInBoxPredictable(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, RoundManager.navHit, RoundManager.AnomalyRandom) + Vector3.up * spawnableItemWithRarity.spawnableItem.verticalOffset;

        GameObject spawnedScrap = Object.Instantiate(spawnableItemWithRarity.spawnableItem.spawnPrefab, position + new Vector3(0, 0.5f, 0), Quaternion.identity);

#if DEBUG
        Plugin.Logger.LogError($"Spawning a chaotic item of name {spawnableItemWithRarity.spawnableItem.itemName}");
#endif

        GrabbableObject grabbableObjectComponent = spawnedScrap.GetComponent<GrabbableObject>();

        grabbableObjectComponent.transform.rotation = Quaternion.Euler(grabbableObjectComponent.itemProperties.restingRotation);
        grabbableObjectComponent.fallTime = 0;
        grabbableObjectComponent.SetScrapValue(Random.Range(5, 150));

        grabbableObjectComponent.NetworkObject.Spawn();

        NetworkObject = grabbableObjectComponent.NetworkObject;

        changeType.Start();

        return this;
    }
}