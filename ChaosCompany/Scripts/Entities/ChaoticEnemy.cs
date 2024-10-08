﻿using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.Managers;
using Unity.Netcode;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Entities;

public class ChaoticEnemy : ChaoticEntities
{
    Timer changeType;
    bool isChaoticEnemyAlreadyTryingToChange;

    public ChaoticEnemy(RoundManager roundManager, bool inside) : base(roundManager, inside, Plugin.ChaoticEnemySwitchExclusionList)
    {
        changeType = new(waitTime: Plugin.ChaoticEnemySwitchTime, oneshot: false);

        changeType.OnTimeout += () =>
        {
            if (GameManager.gameOver)
            {
                return;
            }

            if (isChaoticEnemyAlreadyTryingToChange)
            {
                isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
                return;
            }

            if (NetworkObject == null)
            {
                Plugin.Logger.LogError("NetworkObject == null");
                return;
            }

            if (EnemyAI == null)
            {
                Plugin.Logger.LogError("enemyAI == null");
                return;
            }

            if (NetworkObject?.gameObject == null)
            {
                Plugin.Logger.LogError("NetworkObject.gameObject == null");
                return;
            }

            if (NetworkObject.gameObject == null)
            {
                Plugin.Logger.LogError("A chaotic enemy gameObject == null");
                return;
            }

            Plugin.Logger.LogError($"An {kindString} {EnemyAI.enemyType.enemyName} chaotic enemy is changing");

            NetworkObjectReference? networkObjectReference;

            networkObjectReference = GameManager.SwitchToRandomEnemyType(roundManager, EnemyAI, inside: Inside, NetworkObject);

            if (networkObjectReference is not null)
            {
                NetworkObject = networkObjectReference.GetValueOrDefault();
                EnemyAI = NetworkObject.gameObject.GetComponent<EnemyAI>();
            }
            else
            {
                Plugin.Logger.LogError("networkObjectReference == null after switching enemy type");
            }

            isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
        };

        GameManager.Timers.Add(changeType);
    }

    public override void Update()
    {
        if (EnemyAI == null)
        {
            return;
        }

        if (EnemyAI.isEnemyDead)
        {
            Plugin.Logger.LogError($"An {kindString} chaotic enemy died as {EnemyAI.enemyType.enemyName}");
            changeType.Stop();
            changeType.Finished = true;
            ItsJoever = true;
        }
        else
        {
            Inside = !EnemyAI.isOutside;
        }
    }

    public override Chaotic? Spawn()
    {
        ChaoticEnemy? chaoticEnemy = (ChaoticEnemy?)base.Spawn();

        if (chaoticEnemy == null)
        {
            return null;
        }

        if (EnemyAI == null)
        {
            Plugin.Logger.LogError("EnemyAI shouldn't be null here");
            return null;
        }

#if DEBUG
        Plugin.Logger.LogError($"Spawning {kindString} ChaoticEnemy as {EnemyAI.enemyType.enemyName}");
#endif
        changeType.Start();
        return chaoticEnemy;
    }
}