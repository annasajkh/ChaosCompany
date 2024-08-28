using HarmonyLib;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("SceneManager_OnLoad")]
    static void SceneManager_OnLoadPatch(StartOfRound __instance, ulong clientId, ref string sceneName, ref LoadSceneMode loadSceneMode, ref AsyncOperation asyncOperation)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        Plugin.Logger.LogError($"Game Starting with level {sceneName}");

        if (sceneName != "CompanyBuilding")
        {
            RoundManagerPatch.gameOver = false;
        }
    }
}