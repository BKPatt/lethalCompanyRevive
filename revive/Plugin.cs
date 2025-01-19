using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using lethalCompanyRevive.Misc;
using lethalCompanyRevive.UI.Application;
using System.Reflection;
using UnityEngine;
using InteractiveTerminalAPI.UI; // We rely on InteractiveTerminalManager

namespace lethalCompanyRevive
{
    [BepInPlugin(Metadata.GUID, Metadata.NAME, Metadata.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(Metadata.GUID);
        public static Plugin instance;
        public static ManualLogSource mls;
        public static PluginConfig cfg { get; private set; }

        void Awake()
        {
            cfg = new(base.Config);
            cfg.InitBindings();
            mls = Logger;
            instance = this;
            PatchAllMethods();
            NetcodePatch();

            // We register 'revive' as an application command in the InteractiveTerminalAPI,
            // so typing "revive" alone is recognized and launches ReviveApplication.
            // Meanwhile, "revive <player>" is still handled by TerminalPatcher for direct revives.
            InteractiveTerminalManager.RegisterApplication<ReviveApplication>("revive", caseSensitive: false);

            Logger.LogInfo("Revive functionality has been initialized.");
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
                var methods = type.GetMethods(
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0) method.Invoke(null, null);
                }
            }
        }
    }
}
