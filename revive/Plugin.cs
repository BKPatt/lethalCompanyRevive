using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using lethalCompanyRevive.Managers;
using lethalCompanyRevive.Misc;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using UnityEngine;

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
            InitializeComponents();

            Logger.LogInfo("Revive functionality has been initialized.");
        }

        private void InitializeComponents()
        {
            GameObject busGameObject = new GameObject("UpgradeBus");
            UpgradeBus upgradeBus = busGameObject.AddComponent<UpgradeBus>();
            DontDestroyOnLoad(busGameObject);

            GameObject reviveStoreGameObject = new GameObject("ReviveStore");
            ReviveStore reviveStore = reviveStoreGameObject.AddComponent<ReviveStore>();
            DontDestroyOnLoad(reviveStoreGameObject);
        }

        private void PatchAllMethods()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private string GetJsonContent()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "lethalCompanyRevive.Misc.InfoStrings.json";

            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
