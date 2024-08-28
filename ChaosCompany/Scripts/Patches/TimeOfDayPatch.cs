using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(TimeOfDay))]
static class TimeOfDayPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    static void AwakePrefix(TimeOfDay __instance)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        __instance.quotaVariables.increaseSteepness = Random.Range(4, 7);
        __instance.quotaVariables.deadlineDaysAmount = Random.Range(4, 7);
        __instance.globalTimeSpeedMultiplier = Random.Range(0.25f, 1.0f);
    }
}
