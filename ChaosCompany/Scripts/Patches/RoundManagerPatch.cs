﻿// Ignore Spelling: Teleport

using ChaosCompany.Scripts.Entities;
using Dissonance.Integrations.Unity_NFGO;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.Patches;

enum EnemySpawnType
{
    Inside,
    Outside
}

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch
{
    public static RoundManager? Instance { get; private set; }
    public static List<Timer> Timers { get; private set; } = new();
    public static bool gameOver;

    static Timer spawnEnemyTimer = new(waitTime: Random.Range(60 * 2, 60 * 2 + 30), oneshot: false);
    static EnemySpawnType[] spawnTypes = [EnemySpawnType.Inside, EnemySpawnType.Outside];

    static List<ChaoticEnemy> chaoticEnemies = new();

    static int numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
    static int maxEnemyNumber = Random.Range(4, 7);
    static int enemyNumber = 0;
    static int maxChaoticEnemySpawn = 2;
    static bool beginChaos;

    static void Reset()
    {
        PlayerControllerBPatch.isAlreadySpawned = false;
        chaoticEnemies.Clear();
        enemyNumber = 0;
        maxEnemyNumber = Random.Range(4, 7);
        maxChaoticEnemySpawn = 2;
        spawnEnemyTimer = new(waitTime: Random.Range(60 * 2, 60 * 2 + 30), oneshot: false);
        beginChaos = false;
        numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
        Timers.Clear();
    }

    public static (EnemyType?, NetworkObjectReference?) SpawnRandomEnemy(RoundManager instance, bool inside, Vector3 position, float yRotation = 0, List<string>? exclusion = null)
    {
        Plugin.Logger.LogError("Trying to spawn random enemy");

        List<SpawnableEnemyWithRarity> enemiesTypes;

        if (inside)
        {
            enemiesTypes = instance.currentLevel.Enemies;
        }
        else
        {
            List<SpawnableEnemyWithRarity> enemiesTypesTemp = instance.currentLevel.OutsideEnemies;
            enemiesTypesTemp.AddRange(instance.currentLevel.DaytimeEnemies);

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


            int tries = 100;

            // while the random enemy type that is picked still contain in the exclusion list
            // try to pick random enemy type again for 100 tries
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
            NetworkObjectReference networkObjectReference = instance.SpawnEnemyGameObject(position, yRotation, -1, enemyToSpawn.enemyType);
            return (enemyToSpawn.enemyType, networkObjectReference);
        }
    }

    public static GameObject? GetRandomAlivePlayer(RoundManager instance, bool inside)
    {
        Plugin.Logger.LogError("Trying to get random alive player");

        List<GameObject> players = instance.playersManager.allPlayerObjects.ToList();

        int playerCount = players.Count;

        if (playerCount == 0)
        {
            Plugin.Logger.LogError($"No player is online lmao");
            return null;
        }

        GameObject randomPlayer = players[Random.Range(0, playerCount)];

        int findPlayerAttempt = players.Count;
        int tries = 100;

        // tries to get random player 100 times 
        while (randomPlayer.GetComponent<PlayerControllerB>().isInsideFactory != inside || randomPlayer.GetComponent<PlayerControllerB>().deadBody != null)
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
        if (randomPlayer.GetComponent<PlayerControllerB>().isInsideFactory != inside && randomPlayer.GetComponent<PlayerControllerB>().deadBody != null)
        {
            return null;
        }

        return randomPlayer;
    }

    public static void SpawnRandomEnemyNearRandomPlayer(RoundManager instance, bool inside)
    {
        int allPlayerLength = instance.playersManager.allPlayerScripts.Length;

        if (allPlayerLength == 0)
        {
            Plugin.Logger.LogError($"Cannot spawn enemy there is no player");
            return;
        }

        GameObject? randomAlivePlayer = GetRandomAlivePlayer(instance, inside);

        if (randomAlivePlayer is null)
        {
            return;
        }
#if DEBUG
        Plugin.Logger.LogError("Targeting a player");
#endif

        Vector3 targetPositionPrevious = randomAlivePlayer.GetComponent<NfgoPlayer>().Position;

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
            if (randomAlivePlayer.GetComponent<PlayerControllerB>().deadBody != null || randomAlivePlayer.GetComponent<PlayerControllerB>().isInsideFactory != inside)
            {
                // try it with other player
                if (numberOfTriesOfSpawningRandomEnemyNearPlayer != 0)
                {
                    SpawnRandomEnemyNearRandomPlayer(instance, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }

                return;
            }

            Vector3 targetPositionNow = randomAlivePlayer.GetComponent<NfgoPlayer>().Position;
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
                    SpawnRandomEnemyNearRandomPlayer(instance, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }
                return;
            }

            bool otherPlayerNear = false;

            // check if other player are near
            foreach (var otherPlayer in instance.playersManager.allPlayerScripts)
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
                    SpawnRandomEnemyNearRandomPlayer(instance, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }
                return;
            }

            if (inside)
            {
                // Time to be silly
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(instance, inside: true, position: targetPositionPrevious, exclusion: ["DressGirl"]);

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
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(instance, inside: false, position: targetPositionPrevious, exclusion: ["mech", "worm", "double"]);

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

    public static NetworkObjectReference? SwitchToRandomEnemyType(EnemyAI? enemyTarget, bool inside, NetworkObject? networkObject = null)
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

        if (Instance?.currentLevel.Enemies is null)
        {
            Plugin.Logger.LogError("Instance?.currentLevel.Enemies is null");
            return null;
        }

        if (Instance is null)
        {
            Plugin.Logger.LogError("Instance is null");
            return null;
        }

        if (!enemyTarget.IsSpawned)
        {
            Plugin.Logger.LogError("!enemyTarget.IsSpawned");
            return null;
        }

        #endregion

        (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = (null, null);

        if (networkObject is null)
        {
            (enemySpawnedType, networkObjectReference) = SpawnRandomEnemy(Instance, inside: inside, position: enemyTarget.thisNetworkObject.transform.position, exclusion: ["double", "redlocust", "DressGirl", "Nutcracker", "Spider"]);
        }
        else
        {
            (enemySpawnedType, networkObjectReference) = SpawnRandomEnemy(Instance, inside: inside, position: networkObject.transform.position, exclusion: ["double", "redlocust", "DressGirl", "Nutcracker", "Spider"]);
        }

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

        NetworkObjectReference? networkObjectRef = networkObjectReference;

        // despawn the old enemy
        if (networkObject is null)
        {
            if (enemyTarget.thisNetworkObject is null)
            {
                Plugin.Logger.LogError("enemyTarget.thisNetworkObject is null");
                return null;
            }

            enemyTarget.thisNetworkObject.Despawn();
        }
        else
        {
            networkObject.Despawn();
        }

        return networkObjectRef;
    }

    static void SpawnChaoticEnemy()
    {
        if (Instance is null)
        {
            Plugin.Logger.LogError("Instance is null");
            return;
        }

        ChaoticEnemy chaoticEnemy = new ChaoticEnemy(Instance, inside: Random.Range(0.0f, 1.0f) > 0.2f);

        if (chaoticEnemy.Spawn() is null)
        {
            Plugin.Logger.LogError("Cannot spawn chaotic enemy");
            return;
        }

        chaoticEnemies.Add(chaoticEnemy);
    }

    static void StartSpawning()
    {
        Plugin.Logger.LogError("Start spawning enemies");

        if (Instance is null)
        {
            Plugin.Logger.LogError("Cannot spawn monster Instance is null");
            return;
        }


        spawnEnemyTimer.OnTimeout += () =>
        {
            if (enemyNumber >= maxEnemyNumber)
            {
                spawnEnemyTimer.Stop();
                return;
            }

            // spawn the silliest
            if (maxChaoticEnemySpawn != 0)
            {
                SpawnChaoticEnemy();
                maxChaoticEnemySpawn--;
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

            try
            {
                switch (spawnType)
                {
                    case EnemySpawnType.Inside:
                        if (Random.Range(0, 100) <= 20)
                        {
                            SpawnRandomEnemyNearRandomPlayer(Instance, inside: true);
                            enemyNumber++;
                            return;
                        }

                        int allEnemyVentsLength = Instance.allEnemyVents.Length;

                        if (allEnemyVentsLength == 0)
                        {
                            Plugin.Logger.LogError("Cannot spawn enemy on the vent because there is no vent available");
                            return;
                        }

                        EnemyVent ventToSpawnEnemy = Instance.allEnemyVents[Random.Range(0, allEnemyVentsLength)];

                        Vector3 position = ventToSpawnEnemy.floorNode.position;
                        float y = ventToSpawnEnemy.floorNode.eulerAngles.y;


                        if (Random.Range(0.0f, 1.0f) <= 0.5f)
                        {
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(Instance, inside: true, position, yRotation: y);

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
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(Instance, inside: false, position, exclusion: ["giant", "worm", "double"]);

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

                        enemyNumber++;
                        break;

                    case EnemySpawnType.Outside:
                        if (Random.Range(0, 100) <= 20)
                        {
                            SpawnRandomEnemyNearRandomPlayer(Instance, inside: false);
                            enemyNumber++;
                            return;
                        }

                        int outsideEnemiesCount = Instance.currentLevel.OutsideEnemies.Count;

                        if (outsideEnemiesCount == 0)
                        {
                            Plugin.Logger.LogError("Cannot spawn enemy outside, no enemy list available");
                            return;
                        }

                        SpawnableEnemyWithRarity outsideEnemyToSpawn = Instance.currentLevel.OutsideEnemies[Random.Range(0, outsideEnemiesCount)];

                        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");


                        int spawnPointLength = spawnPoints.Length;

                        if (spawnPointLength == 0)
                        {
                            Plugin.Logger.LogError("Cannot spawn enemy outside, no spawn point available");
                            return;
                        }

                        Vector3 spawnPosition = spawnPoints[Random.Range(0, spawnPointLength)].transform.position;

                        position = Instance.GetRandomNavMeshPositionInBoxPredictable(spawnPosition, 10f, default(NavMeshHit), Instance.AnomalyRandom, Instance.GetLayermaskForEnemySizeLimit(outsideEnemyToSpawn.enemyType));
                        position = Instance.PositionWithDenialPointsChecked(position, spawnPoints, outsideEnemyToSpawn.enemyType);

                        Instance.SpawnEnemyGameObject(position, 0, -1, outsideEnemyToSpawn.enemyType);
#if DEBUG
                        Plugin.Logger.LogError($"Spawning {outsideEnemyToSpawn.enemyType.enemyName} Outside");
#endif
                        enemyNumber++;
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

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void StartPostfix(RoundManager __instance)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        Plugin.Logger.LogError("Round Manager start method get called");
        Reset();

        Instance = __instance;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void UpdatePrefix()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            for (int i = Timers.Count - 1; i >= 0; i--)
            {
                Timers[i].Update();

                if (Timers[i].Finished)
                {
                    Timers.RemoveAt(i);
                }
            }
        }

        if (Instance is not null && !gameOver && Instance.IsServer && Instance.dungeonFinishedGeneratingForAllPlayers && Instance.allEnemyVents.Length != 0)
        {
            for (int i = chaoticEnemies.Count - 1; i >= 0; i--)
            {
                if (chaoticEnemies[i].Dead)
                {
                    // chaotic enemy can die but they will get reincarnated with different form at different time
                    Timer chaoticEnemyRespawnCooldown = new Timer(waitTime: Random.Range(60 * 2, 60 * 2 + 30), oneshot: true);

                    chaoticEnemyRespawnCooldown.OnTimeout += () =>
                    {
#if DEBUG
                        Plugin.Logger.LogError("An chaotic enemy get reincarnated");
#endif
                        SpawnChaoticEnemy();
                    };

                    chaoticEnemyRespawnCooldown.Start();
                    Timers.Add(chaoticEnemyRespawnCooldown);

                    chaoticEnemies.RemoveAt(i);
                }
            }

            if (!beginChaos)
            {
                StartSpawning();
                Plugin.Logger.LogError("Chaos is starting");

                spawnEnemyTimer.Start();
                beginChaos = true;
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
            TimeOfDayPatch.Instance.quotaVariables.deadlineDaysAmount = Random.Range(4, 7);
        }

        gameOver = true;
        Plugin.Logger.LogError("Game Ended");
        Reset();
    }
}