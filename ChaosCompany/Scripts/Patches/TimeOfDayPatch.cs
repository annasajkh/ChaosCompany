using HarmonyLib;
using UnityEngine;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(TimeOfDay))]
static class TimeOfDayPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static void StartPrefix(TimeOfDay __instance)
    {
        __instance.globalTimeSpeedMultiplier = Random.Range(0.25f, 1.0f);
    }
}
