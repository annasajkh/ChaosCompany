using ChaosCompany.Scripts.DataStructures;
using ChaosCompany.Scripts.Managers;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Weighted_Randomizer;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch
{
    public static bool isEnemyAlreadySpawnedOnItemPosition;

    [HarmonyPrefix]
    [HarmonyPatch("DamagePlayerServerRpc")]
    static void DamagePlayerServerRpcPrefix(ref int damageNumber,ref int newHealthAmount)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        damageNumber = (int)(damageNumber  * Random.Range(0.25f, 1f));
    }

    [HarmonyPostfix]
    [HarmonyPatch("GrabObjectServerRpc")]
    static void GrabObjectServerRpcPostfix(PlayerControllerB __instance, ref NetworkObjectReference grabbedObject)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (__instance == null || NetworkManager.Singleton == null || RoundManagerPatch.Instance == null)
        {
            return;
        }

        NetworkObject grabbedObjectResult;

        if (!grabbedObject.TryGet(out grabbedObjectResult, NetworkManager.Singleton))
        {
            return;
        }

        if (grabbedObjectResult.GetComponent<ChaoticItemAdditionalData>() is ChaoticItemAdditionalData chaoticItemAdditionalData)
        {
#if DEBUG
            Plugin.Logger.LogError("Fucking stop switching");
#endif
            chaoticItemAdditionalData.pickedUp = true;
        }

        if (RoundManagerPatch.Instance.currentLevel.Enemies.Count == 0)
        {
            return;
        }

        if (isEnemyAlreadySpawnedOnItemPosition || __instance.isInElevator)
        {
            return;
        }

        StaticWeightedRandomizer<bool> chanceNotSpawnEnemyOnItem = new();

        chanceNotSpawnEnemyOnItem.Add(true, Plugin.ChanceOfSpawnEnemyOnItem);
        chanceNotSpawnEnemyOnItem.Add(false, 100 - Plugin.ChanceOfSpawnEnemyOnItem);

        if (chanceNotSpawnEnemyOnItem.NextWithReplacement())
        {
            return;
        }

        if (__instance.isInsideFactory)
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = GameManager.SpawnRandomEnemy(roundManager: RoundManagerPatch.Instance, inside: true, position: grabbedObjectResult.transform.position, exclusion: ["Masked", "Spring", "Flowerman", "ClaySurgeon", "DressedGirl", "NutCracker", Plugin.SpawnEnemyOnItemExclusionListInside1!, Plugin.SpawnEnemyOnItemExclusionListInside2!, Plugin.SpawnEnemyOnItemExclusionListInside3!, Plugin.SpawnEnemyOnItemExclusionListInside4!, Plugin.SpawnEnemyOnItemExclusionListInside5!]);

            if (enemySpawnedType == null)
            {
                return;
            }

#if DEBUG
            Plugin.Logger.LogError($"Spawning {enemySpawnedType} on the item position inside the facility");
#endif
        }
        else if (!__instance.isInsideFactory && !__instance.isInHangarShipRoom)
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = GameManager.SpawnRandomEnemy(roundManager: RoundManagerPatch.Instance, inside: false, position: grabbedObjectResult.transform.position, exclusion: ["RedLocust", "Mech", "Worm", "Dog", "Double", Plugin.SpawnEnemyOnItemExclusionListOutside1!, Plugin.SpawnEnemyOnItemExclusionListOutside2!, Plugin.SpawnEnemyOnItemExclusionListOutside3!, Plugin.SpawnEnemyOnItemExclusionListOutside4!, Plugin.SpawnEnemyOnItemExclusionListOutside5!]);

            if (enemySpawnedType == null)
            {
                return;
            }

#if DEBUG
            Plugin.Logger.LogError($"Spawning {enemySpawnedType} on the item position outside the facility");
#endif
        }

        isEnemyAlreadySpawnedOnItemPosition = true;

        Timer spawnDelay = new Timer(1f, true);
        spawnDelay.OnTimeout += () =>
        {
            isEnemyAlreadySpawnedOnItemPosition = false;
        };
        spawnDelay.Start();

        GameManager.Timers.Add(spawnDelay);
    }
}