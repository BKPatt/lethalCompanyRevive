using BepInEx.Configuration;
using UnityEngine;
using System.Drawing;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;


namespace lethalCompanyRevive.Misc
{
    public class PluginConfig
    {
        readonly ConfigFile configFile;

        public bool REVIVE { get; set; }

        public PluginConfig(ConfigFile cfg)
        {
            configFile = cfg;
        }

        private T ConfigEntry<T>(string section, string key, T defaultVal, string description)
        {
            return configFile.Bind(section, key, defaultVal, description).Value;
        }

        public void InitBindings()
        {
            REVIVE = ConfigEntry("Revive Players","Revive all players in the ship.", true, "");
        }

    }
}
