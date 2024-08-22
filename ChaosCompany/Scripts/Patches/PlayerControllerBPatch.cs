using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch
{
    static bool isAlreadySpawned;

    [HarmonyPrefix]
    [HarmonyPatch("DamagePlayerServerRpc")]
    static void DamagePlayerServerRpcPrefix(ref int damageNumber,ref int newHealthAmount)
    {
        damageNumber = (int)(damageNumber  * Random.Range(0.25f, 1.25f));
    }

    [HarmonyPrefix]
    [HarmonyPatch("StartSinkingServerRpc")]
    static void StartSinkingServerRpc(ref float sinkingSpeed, ref int audioClipIndex)
    {
        sinkingSpeed *= Random.Range(0.25f, 2f);
    }

    [HarmonyPostfix]
    [HarmonyPatch("GrabObjectServerRpc")]
    static void GrabObjectServerRpcPostfix(PlayerControllerB __instance, ref NetworkObjectReference grabbedObject)
    {
        if (__instance is null || NetworkManager.Singleton is null || RoundManagerPatch.Instance is null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (RoundManagerPatch.Instance.currentLevel.Enemies.Count == 0)
        {
            return;
        }

        if (isAlreadySpawned)
        {
            return;
        }

        if (Random.Range(0, 100) > 2)
        {
            return;
        }

        NetworkObject grabbedObjectResult;

        if (!grabbedObject.TryGet(out grabbedObjectResult, NetworkManager.Singleton))
        {
            return;
        }

        if (__instance.isInsideFactory)
        {
            EnemyType? enemySpawnedType = RoundManagerPatch.SpawnRandomInsideEnemy(instance: RoundManagerPatch.Instance, position: grabbedObjectResult.transform.position, yRotation: 0, exclusion: ["girl"]);

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
            EnemyType? enemySpawnedType = RoundManagerPatch.SpawnRandomOutsideEnemy(instance: RoundManagerPatch.Instance, position: grabbedObjectResult.transform.position, exclusion: ["worm", "double"]);

            if (enemySpawnedType is null)
            {
                return;
            }

#if DEBUG
            Plugin.Logger.LogError($"Spawning {enemySpawnedType} on the item position outside the facility");
#endif
        }

        isAlreadySpawned = true;

        Timer spawnDelay = new Timer(1f, true);
        spawnDelay.OnTimeout += () =>
        {
            isAlreadySpawned = false;
        };
        spawnDelay.Start();

        RoundManagerPatch.Timers.Add(spawnDelay);
    }
}