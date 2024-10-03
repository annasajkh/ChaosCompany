using ChaosCompany.Scripts.Components;
using ChaosCompany.Scripts.Entities;
using ChaosCompany.Scripts.Managers;
using HarmonyLib;
using Unity.Netcode;
using Random = UnityEngine.Random;

namespace ChaosCompany.Scripts.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch
{
    public static RoundManager? Instance { get; private set; }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void StartPostfix(RoundManager __instance)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        Plugin.Logger.LogError("Round Manager start method get called");
        GameManager.Reset();

        Instance = __instance;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void UpdatePrefix()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        for (int i = GameManager.Timers.Count - 1; i >= 0; i--)
        {
            GameManager.Timers[i].Update();

            if (GameManager.Timers[i].Finished)
            {
                GameManager.Timers.RemoveAt(i);
            }
        }

        if (Instance is not null && !GameManager.gameOver && Instance.dungeonFinishedGeneratingForAllPlayers && Instance.allEnemyVents.Length != 0)
        {
            for (int i = GameManager.chaoticEntities.Count - 1; i >= 0; i--)
            {
                GameManager.chaoticEntities[i].Update();

                // its joever
                if (GameManager.chaoticEntities[i].ItsJoever)
                {
                    // sike chaotic enemy can die but they will get reincarnated with different form at different time
                    if (GameManager.chaoticEntities[i] is ChaoticEnemy)
                    {
                        Timer chaoticEnemyRespawnCooldown = new Timer(waitTime: Random.Range(60 * 2, 60 * 2 + 30), oneshot: true);

                        chaoticEnemyRespawnCooldown.OnTimeout += () =>
                        {
#if DEBUG
                            Plugin.Logger.LogError("A chaotic enemy get reincarnated");
#endif
                            GameManager.SpawnChaoticEnemy(Instance);
                        };

                        chaoticEnemyRespawnCooldown.Start();
                        GameManager.Timers.Add(chaoticEnemyRespawnCooldown);
                    }

                    GameManager.chaoticEntities.RemoveAt(i);
                }
            }

            if (!GameManager.beginChaos)
            {
                GameManager.StartSpawning(Instance);
                Plugin.Logger.LogError("Chaos is starting");

                Timer spawnChaoticItemTimer = new(waitTime: 5, true);

                spawnChaoticItemTimer.OnTimeout += () =>
                {
                    for (int i = 0; i < GameManager.maxChaoticItemSpawn; i++)
                    {
                        GameManager.SpawnChaoticItem(Instance);
                    }
                };

                spawnChaoticItemTimer.Start();

                GameManager.Timers.Add(spawnChaoticItemTimer);

                if (GameManager.spawnEnemyTimer is null)
                {
                    return;
                }

                GameManager.spawnEnemyTimer.Start();
                GameManager.beginChaos = true;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("DetectElevatorIsRunning")]
    static void DetectElevatorIsRunningPrefix()
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
        GameManager.Reset();
    }
}