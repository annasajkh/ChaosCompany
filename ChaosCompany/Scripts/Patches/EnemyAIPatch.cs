using HarmonyLib;
using UnityEngine;

namespace ChaosCompany.Scripts.Patches;


[HarmonyPatch(typeof(EnemyAI))]
public class EnemyAIPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("SwitchToBehaviourServerRpc")]
    static void SwitchToBehaviourServerRpcPrefix(EnemyAI __instance, ref int stateIndex)
    {
        if (Random.Range(0, 100) < 2)
        {
#if DEBUG
            Plugin.Logger.LogError($"Changing {__instance.enemyType} behaviour randomly");
#endif
            stateIndex = Random.Range(0, __instance.enemyBehaviourStates.Length);
        }
    }
}
