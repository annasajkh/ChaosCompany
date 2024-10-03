using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.DataStructures;
using ChaosCompany.Scripts.Managers;
using GameNetcodeStuff;
using UnityEngine.AI;
using Timer = ChaosCompany.Scripts.Components.Timer;
using UnityEngine;
using Unity.Netcode;

namespace ChaosCompany.Scripts.Entities;

public class MoveEnemy : ChaoticEntities
{
    PlayerControllerB? closestPlayer;
    Vector3 lastClosestPlayerPosition;
    NavMeshAgent? navMeshAgent;
    Timer findClosestPlayerDelay;
    Vector3? freezePosition;

    string playerUsername = "";
    string previousPlayerUsername = "";

    public MoveEnemy(RoundManager roundManager, bool inside) : base(roundManager, inside, ["forest", "RedLocust", "Doublewing", "FlowerSnake", "DocileLocust", "double", "Centipede", "puffer", "CaveDweller"])
    {
        findClosestPlayerDelay = new Timer(waitTime: 1, false);

        findClosestPlayerDelay.OnTimeout += () =>
        {
            if (NetworkObject == null)
            {
                return;
            }

            if (EnemyAI == null)
            {
                return;
            }

            closestPlayer = GameManager.GetClosestPlayerWithLineOfSight(RoundManager, EnemyAI);

            if (closestPlayer == null)
            {
                return;
            }

            previousPlayerUsername = playerUsername;
            playerUsername = closestPlayer.playerUsername;

            if (playerUsername != previousPlayerUsername)
            {
                Plugin.Logger.LogError($"Changing target to {closestPlayer.playerUsername}");
            }

        };

        findClosestPlayerDelay.Start();
        GameManager.Timers.Add(findClosestPlayerDelay);
    }

    public override void Update()
    {
        if (GameManager.gameOver)
        {
            return;
        }

        if (EnemyAI == null)
        {
            return;
        }

        if (NetworkObject == null)
        {
            return;
        }

        if (EnemyAI.isEnemyDead)
        {
            Plugin.Logger.LogError($"A {kindString} move enemy died as {EnemyAI.enemyType.enemyName}");
            ItsJoever = true;
            return;
        }

        EnemyAIAdditionalData enemyAIAdditionalData = NetworkObject.gameObject.GetComponent<EnemyAIAdditionalData>();

        if (closestPlayer == null)
        {
            enemyAIAdditionalData.paused = false;
            return;
        }

        enemyAIAdditionalData.paused = closestPlayer.timeSincePlayerMoving > 0.05f;

        if (enemyAIAdditionalData.paused)
        {
            if (freezePosition is Vector3 position)
            {
                NetworkObject.transform.position = position;
                EnemyAI.SyncPositionToClients();
            }
        }
        else
        {
            freezePosition = NetworkObject.transform.position;
        }
    }

    public override Chaotic? Spawn()
    {
        MoveEnemy? moveEnemy = (MoveEnemy?)base.Spawn();

        if (EnemyAI == null)
        {
            Plugin.Logger.LogError("EnemyAI shouldn't be null here");
            return null;
        }

        if (moveEnemy == null)
        {
            Plugin.Logger.LogError("Move enemy == null");
            return null;
        }

        if (moveEnemy.NetworkObject == null)
        {
            Plugin.Logger.LogError("Move enemy NetworkObject == null");
            return null;
        }

        moveEnemy.NetworkObject.gameObject.AddComponent<EnemyAIAdditionalData>();
        navMeshAgent = EnemyAI.GetComponent<NavMeshAgent>();

#if DEBUG
        Plugin.Logger.LogError($"Spawning {kindString} MoveEnemy as {EnemyAI.enemyType.enemyName}");
#endif

        return moveEnemy;
    }
}
