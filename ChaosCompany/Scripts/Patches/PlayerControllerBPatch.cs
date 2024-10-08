﻿using ChaosCompany.Scripts.DataStructures;
using ChaosCompany.Scripts.Managers;
using GameNetcodeStuff;
using HarmonyLib;
using KaimiraGames;
using Unity.Netcode;
using UnityEngine;
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

        WeightedList<bool> chanceNotSpawnEnemyOnItem = new();

        chanceNotSpawnEnemyOnItem.Add(true, 100 - Plugin.ChanceOfSpawnEnemyOnItem);
        chanceNotSpawnEnemyOnItem.Add(false,Plugin.ChanceOfSpawnEnemyOnItem);

        if (chanceNotSpawnEnemyOnItem.Next())
        {
            return;
        }

        if (__instance.isInsideFactory)
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = GameManager.SpawnRandomEnemy(roundManager: RoundManagerPatch.Instance, inside: true, position: grabbedObjectResult.transform.position, exclusion: Plugin.SpawnEnemyOnItemExclusionListInside);

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
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = GameManager.SpawnRandomEnemy(roundManager: RoundManagerPatch.Instance, inside: false, position: grabbedObjectResult.transform.position, exclusion: Plugin.SpawnEnemyOnItemExclusionListOutside);
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