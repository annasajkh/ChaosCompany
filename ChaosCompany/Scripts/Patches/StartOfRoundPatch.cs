using HarmonyLib;
using System.ComponentModel;
using UnityEngine.SceneManagement;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("SceneManager_OnLoad")]
    static void SceneManager_OnLoadPatch(StartOfRound __instance, ulong clientId, ref string sceneName, ref LoadSceneMode loadSceneMode, ref AsyncOperation asyncOperation)
    {
        Plugin.Logger.LogError($"Game Starting with level {sceneName}");

        if (sceneName != "CompanyBuilding")
        {
            RoundManagerPatch.gameOver = false;
        }
    }
}