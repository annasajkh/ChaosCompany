using ChaosCompany.Scripts.Managers;
using HarmonyLib;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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

        Random.InitState(__instance.randomMapSeed);

        Plugin.Logger.LogError($"Game Starting with level {sceneName}");

        if (sceneName != "CompanyBuilding")
        {
            GameManager.gameOver = false;
        }
    }
}