using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.Managers;
using Unity.Netcode;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Entities;

public class ChaoticEnemy : ChaoticEntities
{
    Timer changeType;
    bool isChaoticEnemyAlreadyTryingToChange;

    public ChaoticEnemy(RoundManager roundManager, bool inside) : base(roundManager, inside, ["DocileLocust", "cave", "DressGirl", "Nutcracker", "Spider", "double", "Red"])
    {
        changeType = new(waitTime: 10, oneshot: false);

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

            if (NetworkObject is null)
            {
                Plugin.Logger.LogError("NetworkObject is null");
                return;
            }

            if (EnemyAI is null)
            {
                Plugin.Logger.LogError("enemyAI is null");
                return;
            }

            if (NetworkObject?.gameObject is null)
            {
                Plugin.Logger.LogError("NetworkObject.gameObject is null");
                return;
            }

            if (NetworkObject.gameObject is null)
            {
                Plugin.Logger.LogError("A chaotic enemy gameObject is null");
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
                Plugin.Logger.LogError("networkObjectReference is null after switching enemy type");
            }

            isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
        };

        GameManager.Timers.Add(changeType);
    }

    public override void Update()
    {
        if (EnemyAI is null)
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
    }

    public override Chaotic? Spawn()
    {
        ChaoticEnemy? chaoticEnemy = (ChaoticEnemy?)base.Spawn();

        if (chaoticEnemy is null)
        {
            return null;
        }

        if (EnemyAI is null)
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
