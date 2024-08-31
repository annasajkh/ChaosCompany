﻿using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch
{
    public static bool isEnemyAlreadySpawnedOnItemPosition;

    [HarmonyPrefix]
    [HarmonyPatch("StartSinkingServerRpc")]
    static void StartSinkingServerRpcPrefix(ref float sinkingSpeed, ref int audioClipIndex)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        sinkingSpeed *= Random.Range(0.25f, 2f);
    }

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
        if (__instance is null || NetworkManager.Singleton is null || RoundManagerPatch.Instance is null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        NetworkObject grabbedObjectResult;

        if (!grabbedObject.TryGet(out grabbedObjectResult, NetworkManager.Singleton))
        {
            return;
        }


        if (RoundManagerPatch.Instance.currentLevel.Enemies.Count == 0)
        {
            return;
        }

        if (isEnemyAlreadySpawnedOnItemPosition || __instance.isInElevator)
        {
            return;
        }

        if (Random.Range(0, 100) != 0)
        {
            return;
        }

        if (__instance.isInsideFactory)
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = RoundManagerPatch.SpawnRandomEnemy(roundManager: RoundManagerPatch.Instance, inside: true, position: grabbedObjectResult.transform.position, exclusion: ["dressedgirl"]);

            if (enemySpawnedType is null)
            {
                return;
            }

#if DEBUG
            Plugin.Logger.LogError($"Spawning {enemySpawnedType} on the item position inside the facility");
#endif
        }
        else if (!__instance.isInsideFactory && !__instance.isInHangarShipRoom)
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = RoundManagerPatch.SpawnRandomEnemy(roundManager: RoundManagerPatch.Instance, inside: false, position: grabbedObjectResult.transform.position, exclusion: ["mech", "worm", "double"]);

            if (enemySpawnedType is null)
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

        RoundManagerPatch.Timers.Add(spawnDelay);
    }
}