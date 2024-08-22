using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace ChaosCompany.Scripts;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
    public static ManualLogSource Logger { get; private set; } = BepInEx.Logging.Logger.CreateLogSource("ChaosCompany");

    void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! yeah baby!!!");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

}