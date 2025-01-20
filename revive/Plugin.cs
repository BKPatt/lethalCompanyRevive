using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using lethalCompanyRevive.Misc;
using lethalCompanyRevive.UI.Application;
using System.Reflection;
using UnityEngine;
using InteractiveTerminalAPI.UI;

namespace lethalCompanyRevive
{
    [BepInPlugin(Metadata.GUID, Metadata.NAME, Metadata.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        readonly Harmony harmony = new Harmony(Metadata.GUID);
        public static Plugin instance;
        public static ManualLogSource mls;
        public static PluginConfig cfg { get; private set; }

        void Awake()
        {
            cfg = new PluginConfig(Config);
            cfg.InitBindings();
            mls = Logger;
            instance = this;

            if (!cfg.EnableRevive.Value)
            {
                Logger.LogInfo("Revive functionality is disabled via config.");
                return;
            }

            PatchAllMethods();
            NetcodePatch();
            InteractiveTerminalManager.RegisterApplication<ReviveApplication>("revive", false);
            Logger.LogInfo("Revive functionality initialized.");
        }

        void PatchAllMethods()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        static void NetcodePatch()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0) method.Invoke(null, null);
                }
            }
        }
    }
}
