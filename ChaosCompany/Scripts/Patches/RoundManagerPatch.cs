﻿using Dissonance.Integrations.Unity_NFGO;
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
    static Timer spawnEnemyTimer = new(waitTime: Random.Range(60 * 2, 60 * 2 + 30), oneshot: false);
    static EnemySpawnType[] spawnTypes = [EnemySpawnType.Inside, EnemySpawnType.Outside];
    static Timer changeEnemyTypeTimer = new(waitTime: 60 * 4, oneshot: false);
    
    public static NetworkObject? ChaoticEnemy { get; private set; }
    static Timer chaoticEnemySwitchTypeTimer = new(waitTime: 10, oneshot: false);
    static bool thereIsChaoticEnemy;

    static int maxEnemyNumber = Random.Range(4, 7);
    static int enemyNumber = 0;
    static bool gameOver;

    static void Reset()
    {
        PlayerControllerBPatch.isAlreadySpawned = false;
        enemyNumber = 0;
        maxEnemyNumber = Random.Range(4, 7);
        thereIsChaoticEnemy = false;
        chaoticEnemySwitchTypeTimer = new(waitTime: 10, oneshot: false);
        ChaoticEnemy = null;
        changeEnemyTypeTimer = new(waitTime: 60 * 4, oneshot: false);
        spawnEnemyTimer = new(waitTime: Random.Range(60 * 2, 60 * 2 + 30), oneshot: false);
        Timers.Clear();
        Instance = null;
    }


    /// <summary>
    /// Spawn random enemy inside
    /// </summary>
    /// <returns>the enemy type as an index</returns>
    public static (EnemyType?, NetworkObjectReference?) SpawnRandomInsideEnemy(RoundManager instance, Vector3 position, float yRotation, List<string>? exclusion = null)
    {
        int insideEnemiesCount = instance.currentLevel.Enemies.Count;

        if (insideEnemiesCount == 0)
        {
            Plugin.Logger.LogError("Cannot spawn enemy inside, no enemy list available");
            return (null, null);
        }

        int enemySpawnedIndex = Random.Range(0, insideEnemiesCount);

        if (exclusion is not null)
        {
            bool containExclusionEnemy = false;

            foreach (var enemyExclusionName in exclusion)
            {
                string enemySpawnedName = instance.currentLevel.Enemies[enemySpawnedIndex].enemyType.ToString().ToLower().Trim();

                if (enemySpawnedName.Contains(enemyExclusionName.ToLower().Trim()))
                {
#if DEBUG
                    Plugin.Logger.LogError($"Prevent spawning {enemySpawnedName}");
#endif
                    containExclusionEnemy = true;
                    break;
                }
            }


            int tries = 100;

            while (containExclusionEnemy)
            {
                if (tries == 0)
                {
                    break;
                }

                enemySpawnedIndex = Random.Range(0, instance.currentLevel.Enemies.Count);

                containExclusionEnemy = false;

                foreach (var enemyExclusionName in exclusion)
                {
                    string enemySpawnedName = instance.currentLevel.Enemies[enemySpawnedIndex].enemyType.ToString().ToLower().Trim();

                    if (enemySpawnedName.Contains(enemyExclusionName.ToLower().Trim()))
                    {
#if DEBUG
                        Plugin.Logger.LogError($"Prevent spawning {enemySpawnedName}");
#endif
                        containExclusionEnemy = true;
                        break;
                    }
                }

                tries--;
            }
        }

        NetworkObjectReference networkObjectReference = instance.SpawnEnemyGameObject(position, 0, enemySpawnedIndex);

        return (instance.currentLevel.Enemies[enemySpawnedIndex].enemyType, networkObjectReference);
    }

    /// <summary>
    /// Spawn random enemy outside
    /// </summary>
    /// <returns>the enemy type if the enemy can't be spawn it return null</returns>
    public static (EnemyType?, NetworkObjectReference?) SpawnRandomOutsideEnemy(RoundManager instance, Vector3 position, List<string>? exclusion = null)
    {
        if (Instance is null)
        {
            Plugin.Logger.LogError("Instance is null");

            return (null, null);
        }

        List<SpawnableEnemyWithRarity> outsideEnemies = instance.currentLevel.OutsideEnemies;
        outsideEnemies.AddRange(Instance.currentLevel.DaytimeEnemies);

        SpawnableEnemyWithRarity outsideEnemyToSpawn;

        int outsideEnemiesCount = outsideEnemies.Count;

        if (outsideEnemiesCount == 0)
        {
            Plugin.Logger.LogError("Cannot spawn enemy outside, no enemy list available");
            return (null, null);
        }

        outsideEnemyToSpawn = outsideEnemies[Random.Range(0, outsideEnemiesCount)];

        if (exclusion is not null)
        {
            bool containExclusionEnemy = false;

            foreach (var enemyExclusionName in exclusion)
            {
                string enemySpawnedName = outsideEnemyToSpawn.enemyType.ToString().ToLower().Trim();

                if (enemySpawnedName.Contains(enemyExclusionName.ToLower().Trim()))
                {
#if DEBUG
                    Plugin.Logger.LogError($"Prevent spawning {enemySpawnedName}");
#endif
                    containExclusionEnemy = true;
                    break;
                }
            }


            int tries = 100;

            while (containExclusionEnemy)
            {
                if (tries == 0)
                {
                    break;
                }

                outsideEnemyToSpawn = outsideEnemies[Random.Range(0, outsideEnemiesCount)];

                containExclusionEnemy = false;

                foreach (var enemyExclusionName in exclusion)
                {
                    string enemySpawnedName = outsideEnemyToSpawn.enemyType.ToString().ToLower().Trim();

                    if (enemySpawnedName.Contains(enemyExclusionName.ToLower().Trim()))
                    {
#if DEBUG
                        Plugin.Logger.LogError($"Prevent spawning {enemySpawnedName}");
#endif
                        containExclusionEnemy = true;
                        break;
                    }
                }

                tries--;
            }
        }

        NetworkObjectReference networkObjectReference = instance.SpawnEnemyGameObject(position, 0, -1, outsideEnemyToSpawn.enemyType);

        return (outsideEnemyToSpawn.enemyType, networkObjectReference);
    }

    /// <summary>
    /// Get random alive player
    /// </summary>
    /// <param name="inside">should search be inside or outside</param>
    /// <returns>return random alive player</returns>
    public static GameObject? GetRandomAlivePlayer(RoundManager instance, bool inside)
    {
        List<GameObject> players = instance.playersManager.allPlayerObjects.ToList();

        int playerCount = players.Count;

        if (playerCount == 0)
        {
            return null;
        }

        GameObject randomPlayer = players[Random.Range(0, playerCount)];

        int findPlayerAttempt = players.Count;
        int tries = 100;

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

        return randomPlayer;
    }

    public static void SpawnRandomEnemyNearPlayer(RoundManager instance, bool inside)
    {
        int allPlayerLength = instance.playersManager.allPlayerScripts.Length;

        if (allPlayerLength == 0)
        {
            Plugin.Logger.LogError($"Cannot spawn monster there is no player");
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
        Timer spawnMonsterWaitTimer = new Timer(waitTime: 10, oneshot: true);

        spawnMonsterWaitTimer.OnTimeout += () =>
        {
#if DEBUG
            Plugin.Logger.LogError("Checking Target");
#endif
            if (randomAlivePlayer.GetComponent<PlayerControllerB>().deadBody != null || randomAlivePlayer.GetComponent<PlayerControllerB>().isInsideFactory != inside)
            {
                return;
            }
            Vector3 targetPositionNow = randomAlivePlayer.GetComponent<NfgoPlayer>().Position;
#if DEBUG
            Plugin.Logger.LogError($"targetPositionNow: {targetPositionNow}");
#endif

#if DEBUG
            Plugin.Logger.LogError($"Distance to the previous player position is {Vector3.Distance(targetPositionPrevious, targetPositionNow)}");
#endif
            if (Vector3.Distance(targetPositionPrevious, targetPositionNow) <= 15)
            {
                return;
            }

            bool otherPlayerNear = false;

            foreach (var otherPlayer in instance.playersManager.allPlayerScripts)
            {
                if (Vector3.Distance(otherPlayer.transform.position, targetPositionPrevious) <= 15)
                {
                    otherPlayerNear = true;
                    break;
                }
            }

            // Oh no the police is coming quick everyone hide your silliness
            if (otherPlayerNear)
            {
                return;
            }

            if (inside)
            {
                // Time to be silly
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomInsideEnemy(instance, position: targetPositionPrevious, yRotation: 0, exclusion: ["girl"]);

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
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomOutsideEnemy(instance, position: targetPositionPrevious, exclusion: ["double"]);

                if (enemySpawnedType is null)
                {
                    return;
                }

#if DEBUG
                Plugin.Logger.LogError($"Spawning {enemySpawnedType} outside the facility near a player");
#endif
            }
        };

        Timers.Add(spawnMonsterWaitTimer);
        spawnMonsterWaitTimer.Start();
    }

    static NetworkObjectReference? SwitchToRandomEnemyTypeInside(EnemyAI enemyTarget, NetworkObject? networkObject = null)
    {
        if (enemyTarget is null)
        {
            Plugin.Logger.LogError("enemyTarget target is null");
            return null;
        }

        if (enemyTarget.thisNetworkObject is null)
        {
            return null;
        }

        if (Instance is null)
        {
            Plugin.Logger.LogError("Instance is null");
            return null;
        }

        if (!enemyTarget.IsSpawned)
        {
            return null;
        }

        NetworkObjectReference? networkObjectRef = null;

        if (Instance.currentLevel.Enemies.Any(enemy => enemy.enemyType == enemyTarget.enemyType))
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = (null, null);

            if (networkObject is null)
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomInsideEnemy(Instance, position: enemyTarget.thisNetworkObject.transform.position, yRotation: 0, exclusion: ["girl", "nut"]);
            }
            else
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomInsideEnemy(Instance, position: networkObject.transform.position, yRotation: 0, exclusion: ["girl", "nut"]);
            }

            if (enemySpawnedType is null || networkObjectReference is null)
            {
                return null;
            }

#if DEBUG
            Plugin.Logger.LogError($"Changing {enemyTarget.enemyType} to {enemySpawnedType}");

            networkObjectRef = networkObjectReference;
#endif
        }
        else
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = (null, null);

            if (networkObject is null)
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomOutsideEnemy(Instance, position: enemyTarget.thisNetworkObject.transform.position, exclusion: ["worm", "double", "redlocust"]);
            }
            else
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomOutsideEnemy(Instance, position: networkObject.transform.position, exclusion: ["worm", "double", "redlocust"]);

            }

            if (enemySpawnedType is null || networkObjectReference is null)
            {
                return null;
            }

#if DEBUG
            Plugin.Logger.LogError($"Changing {enemyTarget.enemyType} to {enemySpawnedType}");
#endif

            networkObjectRef = networkObjectReference;
        }

        if (networkObject is null)
        {
            enemyTarget.thisNetworkObject.Despawn();
        }
        else
        {
            networkObject.Despawn();
        }

        return networkObjectRef;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void StartPostfix(RoundManager __instance)
    {
        if (!__instance.IsServer)
        {
            return;
        }

        gameOver = false;

        Instance = __instance;
        Timers.Clear();

        changeEnemyTypeTimer.OnTimeout += () =>
        {
            EnemyAI[] enemiesAis = UnityEngine.Object.FindObjectsOfType<EnemyAI>();

            int enemyCount = enemiesAis.Length;

            if (enemyCount == 0)
            {
                Plugin.Logger.LogError("Cannot change enemy, no enemy spawned yet");
                return;
            }

            int enemyChangeIndex = Random.Range(0, enemiesAis.Length);

            SwitchToRandomEnemyTypeInside(enemiesAis[enemyChangeIndex]);
        };

        chaoticEnemySwitchTypeTimer.OnTimeout += () =>
        {
            if (ChaoticEnemy is null)
            {
                return;
            }

            Plugin.Logger.LogError("Chaotic Enemy changing");

            NetworkObjectReference? networkObjectReference = SwitchToRandomEnemyTypeInside(ChaoticEnemy.gameObject.GetComponent<EnemyAI>(), ChaoticEnemy);

            if (networkObjectReference is not null)
            {
                ChaoticEnemy = networkObjectReference.GetValueOrDefault();
            }
        };

        spawnEnemyTimer.OnTimeout += () =>
        {
            if (enemyNumber >= maxEnemyNumber)
            {
                EnemyAI[] enemiesAis = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                
                for (int i = 0; i < enemiesAis.Length; i++)
                {
                    if (enemiesAis[i].thisNetworkObject.IsSpawned)
                    {
                        enemiesAis[i].thisNetworkObject.Despawn();
                    }
                    else
                    {
                        Plugin.Logger.LogError($"{enemiesAis[i].thisNetworkObject} was not spawned on network, so it could not be removed.");
                    }
                }

                Instance.SpawnedEnemies.Clear();

                EnemyAINestSpawnObject[] enemyAINestSpawnObjects = UnityEngine.Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsSortMode.None);

                for (int j = 0; j < enemyAINestSpawnObjects.Length; j++)
                {
                    NetworkObject component = enemyAINestSpawnObjects[j].gameObject.GetComponent<NetworkObject>();
                    if (component != null && component.IsSpawned)
                    {
                        component.Despawn();
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(enemyAINestSpawnObjects[j].gameObject);
                    }
                }

                Plugin.Logger.LogInfo("Despawning enemies on the map");

                enemyNumber = 0;
            }

            if (Random.Range(0.0f, 1.0f) <= 0.5f && !thereIsChaoticEnemy)
            {
                int allEnemyVentsLength = Instance.allEnemyVents.Length;

                if (allEnemyVentsLength == 0)
                {
                    Plugin.Logger.LogError("Cannot spawn enemy on the vent because there is no vent available");
                    return;
                }

                EnemyVent ventToSpawnEnemy = Instance.allEnemyVents[Random.Range(0, allEnemyVentsLength)];

                Vector3 position = ventToSpawnEnemy.floorNode.position;
                float y = ventToSpawnEnemy.floorNode.eulerAngles.y;

                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomInsideEnemy(Instance, position, y);

                if (enemySpawnedType is null)
                {
                    return;
                }

                ventToSpawnEnemy.OpenVentClientRpc();
#if DEBUG
                Plugin.Logger.LogError($"Spawning chaotic enemy");
#endif
                if (networkObjectReference is not null)
                {
                    ChaoticEnemy = networkObjectReference.GetValueOrDefault();
                }

                chaoticEnemySwitchTypeTimer.Start();
                thereIsChaoticEnemy = true;

                return;
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
                enemyNumber++;

                switch (spawnType)
                {
                    case EnemySpawnType.Inside:
                        if (Random.Range(0, 100) <= Random.Range(10, 25))
                        {
                            SpawnRandomEnemyNearPlayer(Instance, inside: true);
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
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomInsideEnemy(Instance, position, y);

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
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomOutsideEnemy(Instance, position, exclusion: ["worm", "double"]);

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
                        break;

                    case EnemySpawnType.Outside:
                        if (Random.Range(0, 100) <= Random.Range(10, 25))
                        {
                            SpawnRandomEnemyNearPlayer(Instance, inside: false);
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

                        GameObject gameObject = UnityEngine.Object.Instantiate(outsideEnemyToSpawn.enemyType.enemyPrefab, position, Quaternion.Euler(Vector3.zero));
                        gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);

                        Instance.SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
                        gameObject.GetComponent<EnemyAI>().enemyType.numberSpawned++;
#if DEBUG
                        Plugin.Logger.LogError($"Spawning {outsideEnemyToSpawn.enemyType} Outside");
#endif
                        break;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(exception);
            }
        };

        Timers.Add(spawnEnemyTimer);
        Timers.Add(changeEnemyTypeTimer);
        Timers.Add(chaoticEnemySwitchTypeTimer);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void UpdatePrefix()
    {
        if (!gameOver)
        {
            if (Instance is null)
            {
                return;
            }

            if (!Instance.IsServer || !Instance.dungeonFinishedGeneratingForAllPlayers || Instance.currentLevel.OutsideEnemies.Count == 0)
            {
                return;
            }

            for (int i = Timers.Count - 1; i >= 0; i--)
            {
                Timers[i].Update();

                if (Timers[i].Finished)
                {
                    Timers.RemoveAt(i);
                }
            }

            if (!Instance.begunSpawningEnemies)
            {
                Plugin.Logger.LogError("Chaos is starting");

                changeEnemyTypeTimer.Start();

                spawnEnemyTimer.Start();
                Instance.begunSpawningEnemies = true;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("DetectElevatorIsRunning")]
    static void DetectElevatorIsRunningPrefix()
    {
        Plugin.Logger.LogError("Game over");
        gameOver = true;
        Reset();
    }

}