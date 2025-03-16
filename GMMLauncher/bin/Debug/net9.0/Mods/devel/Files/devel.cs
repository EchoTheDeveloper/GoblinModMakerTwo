using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace devel
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("Isle Goblin")]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID, BepInDependency.DependencyFlags.HardDependency)]

    public class devel : BaseUnityPlugin
    {
        public const string GUID = "org.bepinex.plugins.devel";
        public const string Name = "devel";
        public const string Version = "1.0.0";
        
        public static ConfigEntry<bool> mEnabled;
        public ConfigDefinition mEnabledDef = new ConfigDefinition(pluginVersion, "Enable/Disable Mod");

        public devel()
        {
            mEnabled = Config.Bind(mEnabledDef, false, new ConfigDescription(
                "Controls if the mod should be enabled or disabled", null, 
                new ConfigurationManagerAttributes { Order = 0 }
            ));
        }
        
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(devel));
        }

        void Update()
        {

        }
    }
}