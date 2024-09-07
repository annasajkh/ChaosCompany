using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.DataStructures;
using ChaosCompany.Scripts.Managers;
using GameNetcodeStuff;
using UnityEngine.AI;
using Vector2 = UnityEngine.Vector2;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Entities;

public class MoveEnemy : ChaoticEntities
{
    PlayerControllerB? closestPlayer;
    NavMeshAgent? navMeshAgent;
    Timer findClosestPlayerDelay;

    string playerUsername = "";
    string previousPlayerUsername = "";

    public MoveEnemy(RoundManager roundManager, bool inside) : base(roundManager, inside, ["RedLocust", "Doublewing", "mech", "FlowerSnake", "DocileLocust", "double", "Centipede", "blob", "worm", "puffer"])
    {
        findClosestPlayerDelay = new Timer(waitTime: 1, false);

        findClosestPlayerDelay.OnTimeout += () =>
        {
            if (NetworkObject is null)
            {
                return;
            }

            closestPlayer = GameManager.GetClosestPlayer(RoundManager, NetworkObject.gameObject.transform.position);

            if (closestPlayer is null)
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

        if (EnemyAI is null)
        {
            return;
        }

        if (NetworkObject is null)
        {
            return;
        }

        if (EnemyAI.isEnemyDead)
        {
            Plugin.Logger.LogError($"A {kindString} move enemy died as {EnemyAI.enemyType.enemyName}");
            ItsJoever = true;
            return;
        }

        if (closestPlayer is null)
        {
            return;
        }

        EnemyAIAdditionalData enemyAIAdditionalData = NetworkObject.gameObject.GetComponent<EnemyAIAdditionalData>();

        if (new Vector2(closestPlayer.thisController.velocity.x, closestPlayer.thisController.velocity.z).magnitude >= 0.5f)
        {
            enemyAIAdditionalData.paused = false;
        }
        else
        {
            enemyAIAdditionalData.paused = true;
        }

        if (enemyAIAdditionalData.paused)
        {
            if (EnemyAI.stunnedByPlayer == null)
            {
                EnemyAI.SetEnemyStunned(true, 10000, closestPlayer);
            }
        }
        else
        {
            if (EnemyAI.stunnedByPlayer != null)
            {
                EnemyAI.SetEnemyStunned(false, 10000, closestPlayer);
            }
        }
    }

    public override Chaotic? Spawn()
    {
        MoveEnemy? moveEnemy = (MoveEnemy?)base.Spawn();

        if (EnemyAI is null)
        {
            Plugin.Logger.LogError("EnemyAI shouldn't be null here");
            return null;
        }

        if (moveEnemy is null)
        {
            Plugin.Logger.LogError("Move enemy is null");
            return null;
        }

        if (moveEnemy.NetworkObject is null)
        {
            Plugin.Logger.LogError("Move enemy NetworkObject is null");
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
