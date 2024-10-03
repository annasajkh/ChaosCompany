using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.DataStructures;
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
        changeType = new(waitTime: Plugin.ChaoticItemSwitchPriceTime, oneshot: false);
        changeType.OnTimeout += () =>
        {
            if (NetworkObject == null)
            {
                return;
            }

            if (NetworkObject.gameObject.GetComponent<GrabbableObject>() is GrabbableObject grabbableObject)
            {

                if (NetworkObject.gameObject.GetComponent<ChaoticItemAdditionalData>() is ChaoticItemAdditionalData chaoticItemAdditionalData)
                {
                    if (chaoticItemAdditionalData.pickedUp)
                    {
#if DEBUG
                        Plugin.Logger.LogError("Fucking stop changing");
#endif
                        changeType.Stop();
                        changeType.Finished = true;
                        ItsJoever = true;
                    }
                }

                grabbableObject.scrapValue = Random.Range(Plugin.MinChaoticItemPrice, Plugin.MaxChaoticItemPrice);
            }
        };

        GameManager.Timers.Add(changeType);
    }

    public override void Update()
    {
        if (NetworkObject == null)
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
        grabbableObjectComponent.scrapValue = Random.Range(Plugin.MinChaoticItemPrice, Plugin.MaxChaoticItemPrice);

        grabbableObjectComponent.NetworkObject.Spawn();

        NetworkObject = grabbableObjectComponent.NetworkObject;
        NetworkObject.gameObject.AddComponent<ChaoticItemAdditionalData>();

        changeType.Start();

        return this;
    }
}