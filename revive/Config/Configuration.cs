using BepInEx.Configuration;
using Unity.Netcode;
using Unity.Collections;
using System;
using HarmonyLib;
using GameNetcodeStuff;

namespace lethalCompanyRevive.Config
{

    [Serializable]
    public class Configuration
    {
        public const int DefaultTicksPerRegeneration = 2;
        public const int DefaultRegenerationPower = 5;
        public const bool DefaultregenerationOutsideShip = false;
        public const bool DefaultHealingUpgradeEnabled = false;
        public const int DefaultHealingUpgradePrice = 800;
        public bool HealingUpgradeUnlocked { get; set; }
        public int TicksPerRegeneration { get; private set; }
        public int RegenerationPower { get; private set; }
        public bool RegenerationOutsideShip { get; private set; }
        public int TicksPerRegenerationOutsideShip { get; private set; }
        public int RegenerationPowerOutsideShip { get; private set; }
        public bool HealingUpgradeEnabled { get; private set; }
        public int HealingUpgradePrice { get; private set; }
        public int RegenerationLimitPerPlayer { get; private set; }


        [NonSerialized]
        readonly ConfigFile configFile;

        public Configuration(ConfigFile cfg)
        {
            Instance = this;

            configFile = cfg;
            InitConfigEntries();
            LethalRegenerationBase.Logger.LogInfo("Configuration Set");
        }

        private void InitConfigEntries()
        {
            RegenerationPower = NewEntry("Values", "Regeneration Power", DefaultRegenerationPower, "Amount of health regenerated INSIDE the ship each time triggered (Between 1 an 100)");
            TicksPerRegeneration = NewEntry("Values", "Ticks Per Regeneration", DefaultTicksPerRegeneration, "Number of ticks until the regeneration is triggered INSIDE the ship (1 tick equals each time the minutes of the clock are changed)");
            RegenerationOutsideShip = NewEntry("Values", "Enable Regeneration Outside Ship", DefaultregenerationOutsideShip, "Whether health is regenerated also outside the ship or only inside.");
            RegenerationPowerOutsideShip = NewEntry("Values", "Regeneration Power Outside Ship", DefaultRegenerationPower, "Amount of health regenerated OUTSIDE the ship each time triggered, requires Regeneration Outside The Ship enabled (Between 1 an 100)");
            TicksPerRegenerationOutsideShip = NewEntry("Values", "Ticks Per Regeneration Outside Ship", DefaultTicksPerRegeneration, "Number of ticks until the regeneration is triggered OUTSIDE the ship, requires Regeneration Outside The Ship enabled (1 tick equals each time the minutes of the clock are changed)");

            HealingUpgradeEnabled = NewEntry("Values", "Regeneration As Upgrade", DefaultHealingUpgradeEnabled, "Makes natural health regeneration an upgrade for the ship and has to be purchased to make it work.");
            HealingUpgradePrice = NewEntry("Values", "Upgrade Price", DefaultHealingUpgradePrice, "Changes the price of ship upgrade for health regeneration. Only works if ship upgrade is enabled");

            RegenerationLimitPerPlayer = NewEntry("Values", "Regeneration Limit Per Player", -1, "Regeneration limit in a round (If the value is '-1' this option will be unlimited)");

            RegenerationPower = 100 >= RegenerationPower && RegenerationPower > 0 ? RegenerationPower : DefaultRegenerationPower;
            TicksPerRegeneration = TicksPerRegeneration > 0 ? TicksPerRegeneration : DefaultTicksPerRegeneration;
            RegenerationPowerOutsideShip = 100 >= RegenerationPowerOutsideShip && RegenerationPowerOutsideShip > 0 ? RegenerationPowerOutsideShip : DefaultRegenerationPower;
            TicksPerRegenerationOutsideShip = TicksPerRegenerationOutsideShip > 0 ? TicksPerRegenerationOutsideShip : DefaultTicksPerRegeneration;
            HealingUpgradePrice = HealingUpgradePrice > 0 ? HealingUpgradePrice : DefaultHealingUpgradePrice;
        }
        private T NewEntry<T>(string category, string key, T defaultVal, string desc)
        {
            return configFile.Bind(category, key, defaultVal, desc).Value;
        }

        public static void RequestSync()
        {
            if (!IsClient) return;
            using FastBufferWriter stream = new(4, Allocator.Temp);
            MessageManager.SendNamedMessage("LethalRegeneration_OnRequestConfigSync", 0uL, stream);
        }
        public static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!IsHost) return;

            LethalRegenerationBase.Logger.LogInfo($"Config sync request received from client: {clientId}");

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            using FastBufferWriter stream = new(array.Length + 4, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                MessageManager.SendNamedMessage("LethalRegeneration_OnReceiveConfigSync", clientId, stream);
            }
            catch (Exception e)
            {
                LethalRegenerationBase.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(4))
            {
                LethalRegenerationBase.Logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                LethalRegenerationBase.Logger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            UpdateInstance(data);

            LethalRegenerationBase.Logger.LogInfo("Successfully synced config with host.");
        }
        public void SendHealingUpgradeStatusToHost()
        {
            if (IsClient)
            {
                using FastBufferWriter stream = new(4, Allocator.Temp);
                stream.WriteValueSafe(HealingUpgradeUnlocked ? 1 : 0, default);
                MessageManager.SendNamedMessage("LethalRegeneration_SendHealingUpgradeStatus", 0uL, stream);
            }
        }

        public static void OnReceiveHealingUpgradeStatus(ulong _, FastBufferReader reader)
        {
            if (IsHost)
            {
                if (reader.TryBeginRead(4))
                {
                    reader.ReadValueSafe(out int status, default);
                    Instance.HealingUpgradeUnlocked = status == 1;
                    LethalRegenerationBase.Logger.LogInfo($"Received sent HealingUpgrade status: {Instance.HealingUpgradeUnlocked}");
                    Instance.BroadcastHealingUpgradeStatusToClients();
                }
                else
                {
                    LethalRegenerationBase.Logger.LogError("Error reading healingUpgradeUnlocked status.");
                }
            }
        }

        public void BroadcastHealingUpgradeStatusToClients()
        {
            if (IsHost)
            {
                using (FastBufferWriter stream = new(4, Allocator.Temp))
                {
                    stream.WriteValueSafe(HealingUpgradeUnlocked ? 1 : 0, default);
                    foreach (var clientId in NetworkManager.Singleton.ConnectedClients.Keys)
                    {
                        MessageManager.SendNamedMessage("LethalRegeneration_BroadcastHealingUpgradeStatus", clientId, stream);
                    }
                }
            }
        }

        public static void OnReceiveBroadcastedHealingUpgradeStatus(ulong _, FastBufferReader reader)
        {
            if (IsClient && !IsHost)
            {
                if (reader.TryBeginRead(4))
                {
                    reader.ReadValueSafe(out int status, default);
                    Instance.HealingUpgradeUnlocked = status == 1;
                    LethalRegenerationBase.Logger.LogInfo($"Received broadcasted healingUpgradeUnlocked status: {Instance.HealingUpgradeUnlocked}");
                    Synced = true;
                }
                else
                {
                    LethalRegenerationBase.Logger.LogError("Error reading broadcasted healingUpgradeUnlocked status.");
                }
            }
        }




        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            Instance.HealingUpgradeUnlocked = false;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("LethalRegeneration_BroadcastHealingUpgradeStatus", OnReceiveBroadcastedHealingUpgradeStatus);

            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnRequestConfigSync", OnRequestSync);
                MessageManager.RegisterNamedMessageHandler("LethalRegeneration_SendHealingUpgradeStatus", OnReceiveHealingUpgradeStatus);

                Synced = true;
                Instance.HealingUpgradeUnlocked = ES3.Load("LethalRegeneration_healingUpgradeUnlocked", GameNetworkManager.Instance.currentSaveFileName, false);
                return;
            }
            MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnReceiveConfigSync", OnReceiveSync);

            Synced = false;
            RequestSync();
        }
    }
}
