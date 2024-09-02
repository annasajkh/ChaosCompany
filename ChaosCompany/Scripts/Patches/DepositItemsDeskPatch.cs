using HarmonyLib;
using System;
using System.Collections.Generic;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(DepositItemsDesk))]
static class DepositItemsDeskPatch
{
    public static HashSet<float> soundVolumes = new();
    static int timesHearingNoise = 0;
    public static Timer consecutiveNoiseDelay = new(waitTime: 3, oneshot: false);

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void StartPostfix(DepositItemsDesk __instance)
    {
        if (!__instance.IsServer)
        {
            return;
        }

        RoundManagerPatch.Timers.Add(consecutiveNoiseDelay);
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetTimesHeardNoiseServerRpc")]
    static void SetTimesHeardNoiseServerRpcPostfix(DepositItemsDesk __instance, ref float valueChange)
    {
        if (!__instance.IsServer)
        {
            return;
        }

        int numberOfSoundSmallerThanThisSound = 0;
        bool thereIsSomethingEqual = false;

        foreach (var soundVolume in soundVolumes)
        {
            if (soundVolume < valueChange)
            {
                numberOfSoundSmallerThanThisSound++;
            }

            if (MathF.Abs(soundVolume - valueChange) <= 0.1f)
            {
                thereIsSomethingEqual = true;
            }
        }

        if (thereIsSomethingEqual && numberOfSoundSmallerThanThisSound < 1)
        {
            return;
        }

        if (numberOfSoundSmallerThanThisSound < 1 || soundVolumes.Count == 0)
        {
            soundVolumes.Add(valueChange);
            return;
        }

        soundVolumes.Add(valueChange);

#if DEBUG
        Plugin.Logger.LogError($"hearing noise {timesHearingNoise + 1} times");
#endif

        consecutiveNoiseDelay.Restart();
        consecutiveNoiseDelay.OnTimeout += () =>
        {
            timesHearingNoise = 0;
            consecutiveNoiseDelay.Stop();
        };

        if (timesHearingNoise > 1 * 2 && timesHearingNoise < 4 * 2)
        {
            __instance.MakeWarningNoiseClientRpc();
        }
        else if (timesHearingNoise >= 4 * 2)
        {
            __instance.AttackPlayersServerRpc();
            timesHearingNoise = 0;
            return;
        }

        timesHearingNoise++;
    }
}
