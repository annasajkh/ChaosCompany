using ChaosCompany.Scripts.Abstracts;
using ChaosCompany.Scripts.Components;
using ChaosCompany.Scripts.Patches;
using Dissonance.Integrations.Unity_NFGO;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using ChaosCompany.Scripts.Entities;
using ChaosCompany.Scripts.Items;

namespace ChaosCompany.Scripts.Managers;

public enum EnemySpawnType
{
    Inside,
    Outside
}

public static class GameManager
{
    public static List<Timer> Timers { get; private set; } = new();
    public static bool gameOver;

    public static Timer spawnEnemyTimer = new(waitTime: Random.Range(60 * 3, 60 * 4), oneshot: false);
    public static EnemySpawnType[] spawnTypes = [EnemySpawnType.Inside, EnemySpawnType.Outside];

    public static List<Chaotic> chaoticEntities = new();

    public static int numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
    public static int modMaxEnemyNumber = Random.Range(2, 4);
    public static int modEnemyNumber = 0;
    public static int maxChaoticEnemySpawn = 2;
    public static int maxMoveEnemySpawn = 2;
    public static int maxChaoticItemSpawn = Random.Range(2, 5);

    public static bool beginChaos;
    public static float timeMultiplier;

    public static void Reset()
    {
        Plugin.Logger.LogError("Game Reset");

        Timers.Clear();

        foreach (var chaoticEntity in chaoticEntities)
        {
            if (chaoticEntity.NetworkObject is null)
            {
                Plugin.Logger.LogError("Despawning chaotic entities failed because network object is null");
                continue;
            }

            if (!chaoticEntity.NetworkObject.IsSpawned)
            {
                Plugin.Logger.LogError("chaoticEntity is not spawned on the network");
                continue;
            }

            if (chaoticEntity is ChaoticEnemy chaoticEnemy)
            {
                if (chaoticEnemy.EnemyAI is null)
                {
                    Plugin.Logger.LogError("chaoticEnemy.EnemyAI is null while trying to print the enemy");

                    chaoticEntity.NetworkObject.Despawn();
                    continue;
                }

                Plugin.Logger.LogError($"Despawning chaotic enemy {chaoticEnemy.EnemyAI.enemyType.enemyName}");
            }
            else if (chaoticEntity is MoveEnemy moveEnemy)
            {
                if (moveEnemy.EnemyAI is null)
                {
                    Plugin.Logger.LogError("moveEnemy.EnemyAI is null while trying to print the enemy");
                    chaoticEntity.NetworkObject.Despawn();
                    continue;
                }

                Plugin.Logger.LogError($"Despawning move enemy {moveEnemy.EnemyAI.enemyType.enemyName}");
            }
            else if (chaoticEntity is ChaoticItem chaoticItem)
            {
                Plugin.Logger.LogError($"Despawning chaotic item");
            }

            chaoticEntity.NetworkObject.Despawn();
        }

        PlayerControllerBPatch.isEnemyAlreadySpawnedOnItemPosition = false;
        chaoticEntities.Clear();
        modEnemyNumber = 0;

        // temp
        //  Random.Range(2, 5)
        maxChaoticItemSpawn = 30;

        modMaxEnemyNumber = Random.Range(2, 4);
        maxChaoticEnemySpawn = 2;
        maxMoveEnemySpawn = 2;

        beginChaos = false;
        numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
    }

    public static (EnemyType?, NetworkObjectReference?) SpawnRandomEnemy(RoundManager roundManager, bool inside, Vector3 position, float yRotation = 0, List<string>? exclusion = null)
    {
        Plugin.Logger.LogError("Trying to spawn random enemy");

        List<SpawnableEnemyWithRarity> enemiesTypes;

        if (inside)
        {
            enemiesTypes = roundManager.currentLevel.Enemies;
        }
        else
        {
            List<SpawnableEnemyWithRarity> enemiesTypesTemp = roundManager.currentLevel.OutsideEnemies;
            enemiesTypesTemp.AddRange(roundManager.currentLevel.DaytimeEnemies);

            enemiesTypes = enemiesTypesTemp;
        }

        SpawnableEnemyWithRarity enemyToSpawn;

        int enemiesCount = enemiesTypes.Count;

        if (enemiesCount == 0)
        {
            Plugin.Logger.LogError("Cannot spawn enemy, enemiesTypes list is empty");
            return (null, null);
        }

        enemyToSpawn = enemiesTypes[Random.Range(0, enemiesCount)];

        bool containExclusionEnemy = false;

        if (exclusion is not null)
        {
            // check if enemyToSpawn.enemyType is in the exclusion list
            foreach (var enemyExclusionName in exclusion)
            {
                string enemySpawnedName = enemyToSpawn.enemyType.ToString().ToLower().Trim();
                string enemyExclusionNameTemp = enemyExclusionName.ToLower().Trim();

                if (enemySpawnedName.Contains(enemyExclusionNameTemp) || enemySpawnedName == enemyExclusionNameTemp)
                {
#if DEBUG
                    Plugin.Logger.LogError($"Prevent spawning {enemyToSpawn.enemyType.enemyName}");
#endif
                    containExclusionEnemy = true;
                    break;
                }
            }


            int tries = 20;

            // while the random enemy type that is picked still contain in the exclusion list
            // try to pick random enemy type again for 20 tries
            while (containExclusionEnemy)
            {
                if (tries == 0)
                {
                    break;
                }

                enemyToSpawn = enemiesTypes[Random.Range(0, enemiesCount)];

                containExclusionEnemy = false;

                foreach (var enemyExclusionName in exclusion)
                {
                    string enemySpawnedName = enemyToSpawn.enemyType.ToString().ToLower().Trim();

                    if (enemySpawnedName.Contains(enemyExclusionName.ToLower().Trim()))
                    {
#if DEBUG
                        Plugin.Logger.LogError($"Prevent spawning {enemyToSpawn.enemyType.enemyName}");
#endif
                        containExclusionEnemy = true;
                        break;
                    }
                }

                tries--;
            }
        }

        // if it still contain the exclusion enemy then just don't spawn anything
        if (containExclusionEnemy)
        {
            Plugin.Logger.LogError("Cannot spawn enemy all of the enemies level in the map is in the exclusion list");
            return (null, null);
        }
        else
        {
            NetworkObjectReference networkObjectReference = roundManager.SpawnEnemyGameObject(position, yRotation, -1, enemyToSpawn.enemyType);

            networkObjectReference.TryGet(out NetworkObject networkObject);

            return (enemyToSpawn.enemyType, networkObjectReference);
        }
    }

    public static PlayerControllerB? GetClosestPlayerWithLineOfSight(RoundManager roundManager, EnemyAI enemyAI)
    {
        List<PlayerControllerB> players = roundManager.playersManager.allPlayerScripts.ToList();

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].isPlayerDead)
            {
                players.RemoveAt(i);
            }
        }

        if (players.Count == 0)
        {
#if DEBUG
            Plugin.Logger.LogError($"No player found");
#endif
            return null;
        }

        PlayerControllerB closestPlayer = players[0];

        for (int i = 0; i < players.Count; i++)
        {
            if (!enemyAI.CheckLineOfSightForPosition(players[i].NetworkObject.transform.position, width: 360, range: 100000))
            {
                continue;
            }

            if (players[i].isPlayerDead)
            {
                continue;
            }

            if (Vector3.Distance(players[i].NetworkObject.transform.position, enemyAI.NetworkObject.transform.position) < Vector3.Distance(closestPlayer.NetworkObject.transform.position, enemyAI.NetworkObject.transform.position))
            {
                closestPlayer = players[i];
            }
        }

        return closestPlayer;
    }

    public static PlayerControllerB? GetRandomAlivePlayer(RoundManager roundManager, bool inside)
    {
        Plugin.Logger.LogError("Trying to get random alive player");

        List<PlayerControllerB> players = roundManager.playersManager.allPlayerScripts.ToList();

        int playerCount = players.Count;

        if (playerCount == 0)
        {
            Plugin.Logger.LogError($"No player is online lmao");
            return null;
        }

        PlayerControllerB randomPlayer = players[Random.Range(0, playerCount)];

        int findPlayerAttempt = players.Count;
        int tries = 20;

        // tries to get random player 20 times 
        while (randomPlayer.isInsideFactory != inside || randomPlayer.deadBody != null)
        {
            if (tries == 0)
            {
                break;
            }

            players.Remove(randomPlayer);

            playerCount = players.Count;

            if (playerCount == 0)
            {
                return null;
            }

            randomPlayer = players[Random.Range(0, players.Count)];

            findPlayerAttempt--;

            if (findPlayerAttempt <= 0)
            {
                return null;
            }

            tries--;
        }

        // if there is no player meet the condition just return null
        if (randomPlayer.isInsideFactory != inside && randomPlayer.deadBody != null)
        {
            return null;
        }

        return randomPlayer;
    }

    public static void SpawnRandomEnemyNearRandomPlayer(RoundManager roundManager, bool inside)
    {
        int allPlayerLength = roundManager.playersManager.allPlayerScripts.Length;

        if (allPlayerLength == 0)
        {
            Plugin.Logger.LogError($"Cannot spawn enemy there is no player");
            return;
        }

        PlayerControllerB? randomAlivePlayer = GetRandomAlivePlayer(roundManager, inside);

        if (randomAlivePlayer is null)
        {
            return;
        }
#if DEBUG
        Plugin.Logger.LogError("Targeting a player");
#endif

        Vector3 targetPositionPrevious = randomAlivePlayer.gameObject.GetComponent<NfgoPlayer>().Position;

#if DEBUG
        Plugin.Logger.LogError($"targetPositionPrevious: {targetPositionPrevious}");
#endif
        Timer spawnEnemyWaitTimer = new Timer(waitTime: 10, oneshot: true);

        // wait 10 second before getting the targeted player new position
        spawnEnemyWaitTimer.OnTimeout += () =>
        {

#if DEBUG
            Plugin.Logger.LogError("Checking Target");
#endif
            // check if the player is the the same status after 10 seconds
            if (randomAlivePlayer.deadBody != null || randomAlivePlayer.isInsideFactory != inside)
            {
                // try it with other player
                if (numberOfTriesOfSpawningRandomEnemyNearPlayer != 0)
                {
                    SpawnRandomEnemyNearRandomPlayer(roundManager, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }

                return;
            }

            Vector3 targetPositionNow = randomAlivePlayer.gameObject.GetComponent<NfgoPlayer>().Position;
#if DEBUG
            Plugin.Logger.LogError($"targetPositionNow: {targetPositionNow}");
#endif

#if DEBUG
            Plugin.Logger.LogError($"Distance to the previous player position is {Vector3.Distance(targetPositionPrevious, targetPositionNow)}");
#endif

            int distanceToPlayer = 20;

            // check if the previous targeted player and the current distance is <= distanceToPlayer
            if (Vector3.Distance(targetPositionPrevious, targetPositionNow) <= distanceToPlayer)
            {
                // try it with other player
                if (numberOfTriesOfSpawningRandomEnemyNearPlayer != 0)
                {
                    SpawnRandomEnemyNearRandomPlayer(roundManager, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }
                return;
            }

            bool otherPlayerNear = false;

            // check if other player are near
            foreach (var otherPlayer in roundManager.playersManager.allPlayerScripts)
            {
                if (Vector3.Distance(otherPlayer.transform.position, targetPositionPrevious) <= distanceToPlayer)
                {
                    otherPlayerNear = true;
                    break;
                }
            }

            // Oh no the police is coming quick everyone hide your silliness
            if (otherPlayerNear)
            {
                // try it with other player
                if (numberOfTriesOfSpawningRandomEnemyNearPlayer != 0)
                {
                    SpawnRandomEnemyNearRandomPlayer(roundManager, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }
                return;
            }

            if (inside)
            {
                // Time to be silly
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(roundManager, inside: true, position: targetPositionPrevious, exclusion: ["DressGirl"]);

                if (enemySpawnedType is null)
                {
                    return;
                }
#if DEBUG
                Plugin.Logger.LogError($"Spawning {enemySpawnedType} inside the facility near a player");
#endif
            }
            else
            {
                // Time to be silly
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(roundManager, inside: false, position: targetPositionPrevious, exclusion: ["mech", "worm", "double"]);

                if (enemySpawnedType is null)
                {
                    return;
                }

#if DEBUG
                Plugin.Logger.LogError($"Spawning {enemySpawnedType} outside the facility near a player");
#endif
            }

            // reset numberOfTriesOfSpawningRandomEnemyNearPlayer
            numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
        };

        Timers.Add(spawnEnemyWaitTimer);
        spawnEnemyWaitTimer.Start();
    }

    public static void SpawnChaoticItem(RoundManager roundManager)
    {
        if (roundManager is null)
        {
            Plugin.Logger.LogError("roundManager is null");
            return;
        }

        ChaoticItem? chaoticItem = new ChaoticItem(roundManager);

        if (chaoticItem.Spawn() is null)
        {
            Plugin.Logger.LogError("Cannot spawn chaotic item");
            return;
        }

        chaoticEntities.Add(chaoticItem);
    }

    public static NetworkObject? SwitchToRandomItemType(RoundManager roundManager, NetworkObject scrapTarget)
    {
        Vector3 scrapOldPosition = scrapTarget.gameObject.transform.position;

        GrabbableObject grabbableObject = scrapTarget.gameObject.GetComponent<GrabbableObject>();

        if (grabbableObject.isHeld || grabbableObject.playerHeldBy is not null)
        {
            return null;
        }

        if (scrapTarget.IsSpawned)
        {
            scrapTarget.Despawn();
        }

        return SpawnRandomItem(roundManager, scrapOldPosition + new Vector3(0, 0.5f, 0), Random.Range(5, 150));
    }

    public static NetworkObject? SpawnRandomItem(RoundManager roundManager, Vector3 position, int worth)
    {
        if (roundManager.currentLevel.spawnableScrap.Count == 0)
        {
            Plugin.Logger.LogError("No spawnable scrap in the level");
            return null;
        }

        SpawnableItemWithRarity spawnableItemWithRarity = roundManager.currentLevel.spawnableScrap[Random.Range(0, roundManager.currentLevel.spawnableScrap.Count)];

        GameObject spawnedScrap = UnityEngine.Object.Instantiate(spawnableItemWithRarity.spawnableItem.spawnPrefab, position, Quaternion.identity);
        GrabbableObject grabbableObjectComponent = spawnedScrap.GetComponent<GrabbableObject>();

        grabbableObjectComponent.transform.rotation = Quaternion.Euler(grabbableObjectComponent.itemProperties.restingRotation);
        grabbableObjectComponent.fallTime = 0;
        grabbableObjectComponent.SetScrapValue(worth);

        grabbableObjectComponent.NetworkObject.Spawn();

        return grabbableObjectComponent.NetworkObject;
    }

    public static NetworkObjectReference? SwitchToRandomEnemyType(RoundManager roundManager, EnemyAI? enemyTarget, bool inside, NetworkObject networkObject)
    {
        Plugin.Logger.LogError("Trying to switch an enemy type to random enemy");

        #region null checking

        if (enemyTarget is null)
        {
            Plugin.Logger.LogError("enemyTarget target is null");
            return null;
        }

        if (enemyTarget.thisNetworkObject is null)
        {
            Plugin.Logger.LogError("enemyTarget.thisNetworkObject is null");
            return null;
        }

        if (roundManager?.currentLevel.Enemies is null)
        {
            Plugin.Logger.LogError("roundManager?.currentLevel.Enemies is null");
            return null;
        }

        if (!enemyTarget.IsSpawned)
        {
            Plugin.Logger.LogError("!enemyTarget.IsSpawned");
            return null;
        }

        #endregion

        (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(roundManager, inside: inside, position: enemyTarget.thisNetworkObject.transform.position, exclusion: ["cave", "double", "redlocust", "DressGirl", "Nutcracker", "Spider"]);

        if (enemySpawnedType is null)
        {
            Plugin.Logger.LogError("enemySpawnedType is null");
            return null;
        }

        if (networkObjectReference is null)
        {
            Plugin.Logger.LogError("networkObjectReference is null");
            return null;
        }

#if DEBUG
        Plugin.Logger.LogError($"Changing {enemyTarget.enemyType.enemyName} to {enemySpawnedType.enemyName}");
#endif

        enemyTarget.KillEnemyOnOwnerClient(overrideDestroy: true);

        if (enemyTarget.IsSpawned)
        {
            networkObject.RemoveOwnership();
            networkObject.Despawn();
        }

        return networkObjectReference;
    }

    public static void SpawnChaoticEnemy(RoundManager roundManager)
    {
        ChaoticEnemy chaoticEnemy = new ChaoticEnemy(roundManager, inside: Random.Range(0.0f, 1.0f) > 0.2f);

        if (chaoticEnemy.Spawn() is null)
        {
            Plugin.Logger.LogError("Cannot spawn chaotic enemy");
            return;
        }

        chaoticEntities.Add(chaoticEnemy);
    }

    public static void SpawnMoveEnemy(RoundManager roundManager)
    {
        MoveEnemy moveEnemy = new MoveEnemy(roundManager, inside: Random.Range(0.0f, 1.0f) > 0.5f);

        if (moveEnemy.Spawn() is null)
        {
            Plugin.Logger.LogError("Cannot spawn move enemy");
            return;
        }

        chaoticEntities.Add(moveEnemy);
    }

    public static void StartSpawning(RoundManager roundManager)
    {
        Plugin.Logger.LogError("Start spawning enemies");

        if (roundManager is null)
        {
            Plugin.Logger.LogError("Cannot spawn monster roundManager is null");
            return;
        }

        spawnEnemyTimer.OnTimeout += () =>
        {
            if (modEnemyNumber >= modMaxEnemyNumber)
            {
                spawnEnemyTimer.Stop();
                return;
            }

            if (Random.Range(0.0f, 1.0f) > 0.5f)
            {
                if (Random.Range(0.0f, 1.0f) > 0.5f)
                {
                    // spawn the silliest
                    if (maxChaoticEnemySpawn != 0)
                    {
                        SpawnChaoticEnemy(roundManager);

                        maxChaoticEnemySpawn--;
                        return;
                    }

                }
                else
                {
                    // spawn another silly 
                    if (maxMoveEnemySpawn != 0)
                    {
                        SpawnMoveEnemy(roundManager);

                        maxMoveEnemySpawn--;
                        return;
                    }
                }
            }

            EnemySpawnType spawnType;

            float spawnInsideOrOutsideProbability = Random.Range(0, 100);

            if (spawnInsideOrOutsideProbability <= 30)
            {
                spawnType = EnemySpawnType.Outside;
            }
            else
            {
                spawnType = EnemySpawnType.Inside;
            }

            // every time an enemy spawn there is a chance of power outage, that can last for 1 - 3 minutes
            if (Random.Range(0, 100) < 10)
            {
                roundManager.SwitchPower(false);

                int powerOutageDuration = Random.Range(30, 60 * 3);

                HUDManager.Instance.AddTextToChatOnServer($"Warning: There is a power outage for {TimeSpan.FromSeconds(powerOutageDuration).ToString(@"mm\:ss")}");

                Timer powerOutageTimer = new(powerOutageDuration, true);

                powerOutageTimer.OnTimeout += () =>
                {
                    roundManager.SwitchPower(true);
                };

                powerOutageTimer.Start();
                Timers.Add(powerOutageTimer);
            }

            try
            {
                switch (spawnType)
                {
                    case EnemySpawnType.Inside:
                        if (Random.Range(0, 100) <= 20)
                        {
                            SpawnRandomEnemyNearRandomPlayer(roundManager, inside: true);
                            modEnemyNumber++;
                            return;
                        }

                        int allEnemyVentsLength = roundManager.allEnemyVents.Length;

                        if (allEnemyVentsLength == 0)
                        {
                            Plugin.Logger.LogError("Cannot spawn enemy on the vent because there is no vent available");
                            return;
                        }

                        EnemyVent ventToSpawnEnemy = roundManager.allEnemyVents[Random.Range(0, allEnemyVentsLength)];

                        Vector3 position = ventToSpawnEnemy.floorNode.position;
                        float y = ventToSpawnEnemy.floorNode.eulerAngles.y;


                        if (Random.Range(0.0f, 1.0f) <= 0.5f)
                        {
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(roundManager, inside: true, position, yRotation: y);

                            if (enemySpawnedType is null)
                            {
                                return;
                            }

                            ventToSpawnEnemy.OpenVentClientRpc();
#if DEBUG
                            Plugin.Logger.LogError($"Spawning {enemySpawnedType} inside");
#endif   
                        }
                        else
                        {
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(roundManager, inside: false, position, exclusion: ["giant", "worm", "double"]);

                            if (enemySpawnedType is null)
                            {
                                Plugin.Logger.LogError($"Cannot spawn enemy inside");
                                return;
                            }

                            ventToSpawnEnemy.OpenVentClientRpc();
#if DEBUG
                            Plugin.Logger.LogError($"Spawning {enemySpawnedType} inside");
#endif
                        }

                        modEnemyNumber++;
                        break;

                    case EnemySpawnType.Outside:
                        if (Random.Range(0, 100) <= 20)
                        {
                            SpawnRandomEnemyNearRandomPlayer(roundManager, inside: false);
                            modEnemyNumber++;
                            return;
                        }

                        int outsideEnemiesCount = roundManager.currentLevel.OutsideEnemies.Count;

                        if (outsideEnemiesCount == 0)
                        {
                            Plugin.Logger.LogError("Cannot spawn enemy outside, no enemy list available");
                            return;
                        }

                        SpawnableEnemyWithRarity outsideEnemyToSpawn = roundManager.currentLevel.OutsideEnemies[Random.Range(0, outsideEnemiesCount)];

                        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");


                        int spawnPointLength = spawnPoints.Length;

                        if (spawnPointLength == 0)
                        {
                            Plugin.Logger.LogError("Cannot spawn enemy outside, no spawn point available");
                            return;
                        }

                        Vector3 spawnPosition = spawnPoints[Random.Range(0, spawnPointLength)].transform.position;

                        position = roundManager.GetRandomNavMeshPositionInBoxPredictable(spawnPosition, 10f, default(NavMeshHit), roundManager.AnomalyRandom, roundManager.GetLayermaskForEnemySizeLimit(outsideEnemyToSpawn.enemyType));
                        position = roundManager.PositionWithDenialPointsChecked(position, spawnPoints, outsideEnemyToSpawn.enemyType);

                        roundManager.SpawnEnemyGameObject(position, 0, -1, outsideEnemyToSpawn.enemyType);
#if DEBUG
                        Plugin.Logger.LogError($"Spawning {outsideEnemyToSpawn.enemyType.enemyName} Outside");
#endif
                        modEnemyNumber++;
                        break;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(exception);
            }
        };

        Timers.Add(spawnEnemyTimer);
    }
}
