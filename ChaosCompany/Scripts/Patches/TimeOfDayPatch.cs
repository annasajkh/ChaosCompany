﻿using HarmonyLib;
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
            __instance.quotaVariables.deadlineDaysAmount = Random.Range(Plugin.MinDayBeforeDeadline, Plugin.MaxDayBeforeDeadline + 1);
            __instance.SyncTimeClientRpc(__instance.globalTime, (int)__instance.timeUntilDeadline);
        }

        Instance = __instance;

        Instance.globalTimeSpeedMultiplier = Random.Range(Plugin.MinTimeMultiplier, Plugin.MaxTimeMultiplier);
    }
}