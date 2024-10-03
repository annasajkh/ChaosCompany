using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace ChaosCompany.Scripts;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

    public static ManualLogSource Logger { get; private set; } = BepInEx.Logging.Logger.CreateLogSource("ChaosCompany");

    public static int MaxChaoticEnemySpawn { get; private set; }
    public static int MaxMoveEnemySpawn { get; private set; }

    public static int SpawnEnemyInterval { get; private set; }

    public static int MinEnemySpawnNumber { get; private set; }
    public static int MaxEnemySpawnNumber { get; private set; }

    public static int MinChaoticItemSpawnNumber { get; private set; }
    public static int MaxChaoticItemSpawnNumber { get; private set; }

    public static int ChanceOfSpawnEnemyOnItem { get; private set; }
    
    public static string? SpawnEnemyOnItemExclusionListInside1 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListInside2 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListInside3 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListInside4 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListInside5 { get; private set; }

    public static string? SpawnEnemyOnItemExclusionListOutside1 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListOutside2 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListOutside3 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListOutside4 { get; private set; }
    public static string? SpawnEnemyOnItemExclusionListOutside5 { get; private set; }

    public static float MinTimeMultiplier { get; private set; }
    public static float MaxTimeMultiplier { get; private set; }

    public static int MinDayBeforeDeadline { get; private set; }
    public static int MaxDayBeforeDeadline { get; private set; }

    public static int CompanyMonsterTimesHeardNoiseBeforeWarning { get; private set; }
    public static int CompanyMonsterTimesHeardNoiseBeforeAttack { get; private set; }
    public static float CompanyMonsterConsecutiveNoiseDelay { get; private set; }

    public static int MinPowerOutageDuration { get; set; }
    public static int MaxPowerOutageDuration { get; set; }

    void Awake()
    {
        MaxChaoticEnemySpawn = Config.Bind("Spawning", "Max chaotic enemy spawn", 2, "The maximum number of chaotic enemy that can spawn").Value;
        MaxMoveEnemySpawn = Config.Bind("Spawning", "Max move enemy spawn", 2, "The maximum number of move enemy that can spawn").Value;

        SpawnEnemyInterval = Config.Bind("Spawning", "Spawn enemy interval", 60 * 3, "The interval time for spawn behaviour from chaos company to spawn stuff in seconds").Value;

        MinEnemySpawnNumber = Config.Bind("Spawning", "Min enemy spawn number", 2, "Minimum number of enemy the mod can spawn, the final result will be random between the minimum and the maximum").Value;
        MaxEnemySpawnNumber = Config.Bind("Spawning", "Max enemy spawn number", 4, "Maximum number of enemy the mod can spawn, the final result will be random between the minimum and the maximum").Value;

        MinChaoticItemSpawnNumber = Config.Bind("Spawning", "Min chaotic item spawn number", 2, "Minimum number of chaotic item the mod can spawn, the final result will be random between the minimum and the maximum").Value;
        MaxChaoticItemSpawnNumber = Config.Bind("Spawning", "Max chaotic item spawn number", 5, "Maximum number of chaotic item the mod can spawn, the final result will be random between the minimum and the maximum").Value;

        ChanceOfSpawnEnemyOnItem = Config.Bind("Spawn Enemy When Grabbing", "Chance", 1, new ConfigDescription("The chance of spawning enemy when grabbing an item", new AcceptableValueRange<int>(0, 100))).Value;
        
        SpawnEnemyOnItemExclusionListInside1 = Config.Bind("Spawn Enemy When Grabbing", "Inside Exclusion 1", "None" , "The spawn exclusion of a chance of spawning enemy when grabbing an item inside, The name of the enemy is the name in the code and not aliases example instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is Masked, Spring, Flowerman, ClaySurgeon, DressedGirl, and NutCracker").Value;
        SpawnEnemyOnItemExclusionListInside2 = Config.Bind("Spawn Enemy When Grabbing", "Inside Exclusion 2", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item inside, The name of the enemy is the name in the code and not aliases example instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is Masked, Spring, Flowerman, ClaySurgeon, DressedGirl, and NutCracker").Value;
        SpawnEnemyOnItemExclusionListInside3 = Config.Bind("Spawn Enemy When Grabbing", "Inside Exclusion 3", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item inside, The name of the enemy is the name in the code and not aliases example instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is Masked, Spring, Flowerman, ClaySurgeon, DressedGirl, and NutCracker").Value;
        SpawnEnemyOnItemExclusionListInside4 = Config.Bind("Spawn Enemy When Grabbing", "Inside Exclusion 4", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item inside, The name of the enemy is the name in the code and not aliases example instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is Masked, Spring, Flowerman, ClaySurgeon, DressedGirl, and NutCracker").Value;
        SpawnEnemyOnItemExclusionListInside5 = Config.Bind("Spawn Enemy When Grabbing", "Inside Exclusion 5", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item inside, The name of the enemy is the name in the code and not aliases example instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is Masked, Spring, Flowerman, ClaySurgeon, DressedGirl, and NutCracker").Value;

        SpawnEnemyOnItemExclusionListOutside1 = Config.Bind("Spawn Enemy When Grabbing", "Outside Exclusion 1", "None" , "The spawn exclusion of a chance of spawning enemy when grabbing an item outside, The name of the enemy is the name in the code and not aliases example, instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is RedLocust, Mech, Worm, Dog, and Double").Value;
        SpawnEnemyOnItemExclusionListOutside2 = Config.Bind("Spawn Enemy When Grabbing", "Outside Exclusion 2", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item outside, The name of the enemy is the name in the code and not aliases example, instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is RedLocust, Mech, Worm, Dog, and Double").Value;
        SpawnEnemyOnItemExclusionListOutside3 = Config.Bind("Spawn Enemy When Grabbing", "Outside Exclusion 3", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item outside, The name of the enemy is the name in the code and not aliases example, instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is RedLocust, Mech, Worm, Dog, and Double").Value;
        SpawnEnemyOnItemExclusionListOutside4 = Config.Bind("Spawn Enemy When Grabbing", "Outside Exclusion 4", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item outside, The name of the enemy is the name in the code and not aliases example, instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is RedLocust, Mech, Worm, Dog, and Double").Value;
        SpawnEnemyOnItemExclusionListOutside5 = Config.Bind("Spawn Enemy When Grabbing", "Outside Exclusion 5", "None", "The spawn exclusion of a chance of spawning enemy when grabbing an item outside, The name of the enemy is the name in the code and not aliases example, instead of Bracken it's FlowerMan and capitalization doesn't matter, full name also not required, default exclusion is RedLocust, Mech, Worm, Dog, and Double").Value;

        MinDayBeforeDeadline = Config.Bind("Time", "Min day before deadline", 4, "Minimum value for how many day before the deadline, the final result will be random between the minimum and the maximum").Value;
        MaxDayBeforeDeadline = Config.Bind("Time", "Max day before deadline", 6, "Maximum value for how many day before the deadline, the final result will be random between the minimum and the maximum").Value;
        MinTimeMultiplier = Config.Bind("Time", "Min time multiplier", 0.5f, "Minimum value for the time multiplier, the final result will be random between the minimum and the maximum").Value;
        MaxTimeMultiplier = Config.Bind("Time", "Max time multiplier", 1.25f, "Maximum value for the time multiplier, the final result will be random between the minimum and the maximum").Value;

        CompanyMonsterTimesHeardNoiseBeforeWarning = Config.Bind("Company Monster", "Times heard noise before warning", 3, "The company monster noise hearing times before warning, signified by growling and camera shaking").Value;
        CompanyMonsterTimesHeardNoiseBeforeAttack = Config.Bind("Company Monster", "Times heard noise before attack", 6, "The company monster noise hearing times before attacking").Value;
        CompanyMonsterConsecutiveNoiseDelay = Config.Bind("Company Monster", "Consecutive noise delay", 3, "The company monster delay in seconds if it doesn't hear noises with this amount of time it will reset the TimesHeardNoise to 0").Value;

        MinPowerOutageDuration = Config.Bind("Power outage", "Min duration", 30, "The minimum duration for power outage, the final result will be random between the minimum and the maximum").Value;
        MaxPowerOutageDuration = Config.Bind("Power outage", "Max duration", 60 * 3, "The maximum duration for power outage, the final result will be random between the minimum and the maximum").Value;

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! yeah baby!!!");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}