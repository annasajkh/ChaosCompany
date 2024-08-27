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

    public static NetworkObject? ChaoticEnemy { get; private set; }

    static Timer chaoticEnemySwitchTypeTimer = new(waitTime: 10, oneshot: false);
    static bool thereIsChaoticEnemy;
    static int numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;

    static int maxEnemyNumber = Random.Range(4, 7);
    static int enemyNumber = 0;
    static bool isChaoticEnemyAlreadyTryingToChange = false;
    static bool beginChaos;

    static void Reset()
    {
        PlayerControllerBPatch.isAlreadySpawned = false;
        enemyNumber = 0;
        maxEnemyNumber = Random.Range(4, 7);
        thereIsChaoticEnemy = false;
        chaoticEnemySwitchTypeTimer = new(waitTime: 10, oneshot: false);
        ChaoticEnemy = null;
        // Random.Range(60 * 2, 60 * 2 + 30)
        spawnEnemyTimer = new(waitTime: 70, oneshot: false);
        beginChaos = false;
        numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
        Timers.Clear();
    }

    /// <summary>
    /// Spawn random enemy from a list 
    /// </summary>
    /// <returns>the enemy type if the enemy can't be spawn it return null</returns>
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
                        Plugin.Logger.LogError($"Prevent spawning {enemySpawnedName}");
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

    /// <summary>
    /// Get random alive player
    /// </summary>
    /// <param name="inside">should search be inside or outside</param>
    /// <returns>return random alive player</returns>
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
        if (randomPlayer.GetComponent<PlayerControllerB>().isInsideFactory == inside || randomPlayer.GetComponent<PlayerControllerB>().deadBody == null)
        {
            return null;
        }

        return randomPlayer;
    }

    public static void SpawnRandomEnemyNearPlayer(RoundManager instance, bool inside)
    {
        Plugin.Logger.LogError("Trying to spawn random enemy near a player");

#if DEBUG
        Plugin.Logger.LogError($"Trying to spawn enemy near player attempt {numberOfTriesOfSpawningRandomEnemyNearPlayer}");
#endif
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
            bool teleportChaoticEnemyNearPlayer = Random.Range(0.0f, 1.0f) > 0.5f;

#if DEBUG
            Plugin.Logger.LogError("Checking Target");
#endif
            // check if the player is the the same status after 10 seconds
            if (randomAlivePlayer.GetComponent<PlayerControllerB>().deadBody != null || randomAlivePlayer.GetComponent<PlayerControllerB>().isInsideFactory != inside)
            {
                // try it with other player
                if (numberOfTriesOfSpawningRandomEnemyNearPlayer != 0)
                {
                    SpawnRandomEnemyNearPlayer(instance, inside);
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

            int distanceToPlayer = 15;

            if (teleportChaoticEnemyNearPlayer)
            {
                distanceToPlayer = 5;
            }

            // check if the previous targeted player and the current distance is <= distanceToPlayer
            if (Vector3.Distance(targetPositionPrevious, targetPositionNow) <= distanceToPlayer)
            {
                // try it with other player
                if (numberOfTriesOfSpawningRandomEnemyNearPlayer != 0)
                {
                    SpawnRandomEnemyNearPlayer(instance, inside);
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
                    SpawnRandomEnemyNearPlayer(instance, inside);
                    numberOfTriesOfSpawningRandomEnemyNearPlayer--;
                }
                return;
            }

            if (inside)
            {
                // teleport the sillies near a player
                if (teleportChaoticEnemyNearPlayer && ChaoticEnemy is not null)
                {
#if DEBUG
                    Plugin.Logger.LogError("Teleporting the chaotic monster near a player");
#endif
                    ChaoticEnemy.gameObject.transform.position = targetPositionPrevious;
                    return;
                }

                // Time to be silly
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(instance, inside: true, position: targetPositionPrevious, exclusion: ["girl"]);

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
                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(instance, inside: false, position: targetPositionPrevious, exclusion: ["mech", "double"]);

                if (enemySpawnedType is null)
                {
                    return;
                }

#if DEBUG
                Plugin.Logger.LogError($"Spawning {enemySpawnedType} outside the facility near a player");
#endif
            }

            numberOfTriesOfSpawningRandomEnemyNearPlayer = 6;
        };

        Timers.Add(spawnEnemyWaitTimer);
        spawnEnemyWaitTimer.Start();
    }

    static NetworkObjectReference? SwitchToRandomEnemyTypeInside(EnemyAI? enemyTarget, NetworkObject? networkObject = null)
    {
        Plugin.Logger.LogError("Trying to switch an enemy type to random enemy");

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

        NetworkObjectReference? networkObjectRef = null;

        if (Instance.currentLevel.Enemies.Any(enemy => enemy.enemyType == enemyTarget.enemyType))
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = (null, null);

            if (networkObject is null)
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomEnemy(Instance, inside: true, position: enemyTarget.thisNetworkObject.transform.position, exclusion: ["girl", "nut"]);
            }
            else
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomEnemy(Instance, inside: true, position: networkObject.transform.position, exclusion: ["girl", "nut"]);
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
            Plugin.Logger.LogError($"Changing {enemyTarget.enemyType} to {enemySpawnedType}");
#endif

            networkObjectRef = networkObjectReference;
        }
        else
        {
            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = (null, null);

            if (networkObject is null)
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomEnemy(Instance, inside: false, position: enemyTarget.thisNetworkObject.transform.position, exclusion: ["mech", "worm", "double", "redlocust"]);
            }
            else
            {
                (enemySpawnedType, networkObjectReference) = SpawnRandomEnemy(Instance, inside: false, position: networkObject.transform.position, exclusion: ["mech", "worm", "double", "redlocust"]);

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
            Plugin.Logger.LogError($"Changing {enemyTarget.enemyType} to {enemySpawnedType}");
#endif

            networkObjectRef = networkObjectReference;
        }

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


    static void StartSpawning()
    {
        Plugin.Logger.LogError("Start spawning enemies");

        if (Instance is null)
        {
            Plugin.Logger.LogError("Cannot spawn monster Instance is null");
            return;
        }

        chaoticEnemySwitchTypeTimer.OnTimeout += () =>
        {
            if (isChaoticEnemyAlreadyTryingToChange)
            {
                isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
                return;
            }

            if (ChaoticEnemy is null)
            {
                Plugin.Logger.LogError("ChaoticEnemy died");
                chaoticEnemySwitchTypeTimer.Stop();
                return;
            }

            if (ChaoticEnemy.gameObject is null)
            {
                Plugin.Logger.LogError("ChaoticEnemy's gameObject is null");
                return;
            }

            Plugin.Logger.LogError("Chaotic Enemy changing");

            var enemyAIComponent = ChaoticEnemy.gameObject.GetComponent<EnemyAI>();
            if (enemyAIComponent is null)
            {
                Plugin.Logger.LogError("ChaoticEnemy does not have an EnemyAI component");
                return;
            }

            NetworkObjectReference? networkObjectReference = null;


            networkObjectReference = SwitchToRandomEnemyTypeInside(enemyAIComponent, ChaoticEnemy);

            if (networkObjectReference is not null)
            {
                ChaoticEnemy = networkObjectReference.GetValueOrDefault();
            }
            else
            {
                Plugin.Logger.LogError("networkObjectReference is null after switching enemy type");
            }

            isChaoticEnemyAlreadyTryingToChange = !isChaoticEnemyAlreadyTryingToChange;
        };


        spawnEnemyTimer.OnTimeout += () =>
        {
            if (enemyNumber >= maxEnemyNumber)
            {
                spawnEnemyTimer.Stop();
                return;
            }

            // Time to be the sillies
            if (!thereIsChaoticEnemy)
            {
                int allEnemyVentsLength = Instance.allEnemyVents.Length;

                if (allEnemyVentsLength == 0)
                {
                    Plugin.Logger.LogError("Cannot spawn chaotic enemy on the vent because there is no vent available");
                    return;
                }

                EnemyVent ventToSpawnEnemy = Instance.allEnemyVents[Random.Range(0, allEnemyVentsLength)];

                Vector3 position = ventToSpawnEnemy.floorNode.position;
                float y = ventToSpawnEnemy.floorNode.eulerAngles.y;

                (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(Instance, inside: true, position, yRotation: y);

                if (enemySpawnedType is null || networkObjectReference is null)
                {
                    return;
                }

                ventToSpawnEnemy.OpenVentClientRpc();
#if DEBUG
                Plugin.Logger.LogError($"Spawning chaotic enemy");
#endif

                ChaoticEnemy = networkObjectReference.GetValueOrDefault();

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
                switch (spawnType)
                {
                    case EnemySpawnType.Inside:
                        if (Random.Range(0, 100) <= 20)
                        {
                            SpawnRandomEnemyNearPlayer(Instance, inside: true);
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
                            (EnemyType? enemySpawnedType, NetworkObjectReference? networkObjectReference) = SpawnRandomEnemy(Instance, inside: false, position, exclusion: ["mech", "worm", "double"]);

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
                            SpawnRandomEnemyNearPlayer(Instance, inside: false);
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
                        Plugin.Logger.LogError($"Spawning {outsideEnemyToSpawn.enemyType} Outside");
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
        Timers.Add(chaoticEnemySwitchTypeTimer);
    }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void StartPostfix(RoundManager __instance)
    {
        if (!__instance.IsServer)
        {
            return;
        }
        Instance = __instance;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void UpdatePrefix()
    {
        if (Instance is not null && !gameOver && Instance.IsServer && Instance.dungeonFinishedGeneratingForAllPlayers && Instance.allEnemyVents.Length != 0)
        {
            for (int i = Timers.Count - 1; i >= 0; i--)
            {
                Timers[i].Update();

                if (Timers[i].Finished)
                {
                    Timers.RemoveAt(i);
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
        gameOver = true;
        Plugin.Logger.LogError("Game Ended");
        Reset();
    }

}