using ChaosCompany.Scripts.Managers;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(TimeOfDay))]
static class TimeOfDayPatch
{
    public static TimeOfDay? Instance { get; set; }

    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    static void AwakePrefix(TimeOfDay __instance)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // my precious little hack
        if (Instance == null)
        {
            __instance.quotaVariables.deadlineDaysAmount = Random.Range(4, 7);
            __instance.SyncTimeClientRpc(__instance.globalTime, (int)__instance.timeUntilDeadline);
        }

        Instance = __instance;

        GameManager.timeMultiplier = Random.Range(0.5f, 1.25f);
        GameManager.spawnEnemyTimer = new(waitTime: 60 * 3, oneshot: false);

        __instance.globalTimeSpeedMultiplier = GameManager.timeMultiplier;
    }
}