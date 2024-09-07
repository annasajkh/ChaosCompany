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

        __instance.globalTimeSpeedMultiplier = Random.Range(0.25f, 1.0f);
    }
}