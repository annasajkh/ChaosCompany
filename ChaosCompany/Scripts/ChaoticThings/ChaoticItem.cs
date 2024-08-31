using ChaosCompany.Scripts.Patches;
using Unity.Netcode;
using UnityEngine;

namespace ChaosCompany.Scripts.ChaoticThings;

public class ChaoticItem : Chaotic
{
    public ChaoticItem(RoundManager roundManager) : base(roundManager, new(waitTime: 3, oneshot: false))
    {
        changeType.OnTimeout += () =>
        {
            if (NetworkObject is null)
            {
                return;
            }

            if (NetworkObject.gameObject.GetComponent<GrabbableObject>().isHeld)
            {
                Plugin.Logger.LogError("An chaotic item has been pickup by a player");
                changeType.Stop();
                changeType.Finished = true;
                ItsJoever = true;
                return;
            }


            Plugin.Logger.LogError("An chaotic item is changing");

            NetworkObject = RoundManagerPatch.SwitchToRandomItemType(roundManager, NetworkObject);
        };

        RoundManagerPatch.Timers.Add(changeType);
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

        GameObject spawnedScrap = UnityEngine.Object.Instantiate(spawnableItemWithRarity.spawnableItem.spawnPrefab, position, Quaternion.identity);
        GrabbableObject grabbableObjectComponent = spawnedScrap.GetComponent<GrabbableObject>();

        grabbableObjectComponent.transform.rotation = Quaternion.Euler(grabbableObjectComponent.itemProperties.restingRotation);
        grabbableObjectComponent.fallTime = 0;
        grabbableObjectComponent.scrapValue = Random.Range(0, 300);

        NetworkObject grabbableObjectNetworkObject = grabbableObjectComponent.GetComponent<NetworkObject>();
        grabbableObjectNetworkObject.Spawn();

        NetworkObject = grabbableObjectNetworkObject;

        changeType.Start();

        return this;
    }
}
