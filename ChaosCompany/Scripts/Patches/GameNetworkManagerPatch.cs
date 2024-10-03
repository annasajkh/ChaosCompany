using ChaosCompany.Scripts.Managers;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("DisconnectProcess")]
    static void DisconnectProcessPostfix()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // this is a hack but it's fine, i'm just tired
        if (TimeOfDayPatch.Instance is not null)
        {
            TimeOfDayPatch.Instance.quotaVariables.deadlineDaysAmount = Random.Range(Plugin.MinDayBeforeDeadline, Plugin.MaxDayBeforeDeadline + 1);
        }

        GameManager.gameOver = true;
        Plugin.Logger.LogError("Game Ended");
        GameManager.Reset();
    }
}