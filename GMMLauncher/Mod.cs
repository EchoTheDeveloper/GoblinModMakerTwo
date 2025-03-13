using System.IO;
using System.Text;

namespace GMMLauncher;

public class Mod(string modName, string modDescription, string modAuthors, string modVersion)
{
    public string Name { get; init; } = modName;
    public string NameNoSpaces { get; init; } = modName.Replace(" ", string.Empty);
    public string Description { get; init; } = modDescription;
    public string Authors { get; init; } = modAuthors;
    public string Version { get; init; } = modVersion;
    
    public void SaveFilesAsCS()
    {
        string currentDir = Directory.GetCurrentDirectory();
        string folderPath = Path.Combine(currentDir, "Mods", NameNoSpaces, "Files");
        // TODO: FINISH THIS
    }

    public void CreateFile(string fileName, string fileContent = "")
    {
        string currentDir = Directory.GetCurrentDirectory();
        string filePath = Path.Combine(currentDir, "Mods", NameNoSpaces, "Files", fileName + ".cs");
        File.WriteAllText(filePath, fileContent);
    }

    public string GetFilePath()
    {
        string currentDir = Directory.GetCurrentDirectory();
        return Path.Combine(currentDir, "Mods", NameNoSpaces, "Files", NameNoSpaces + ".cs");
    }

    public void CreateMainFile()
    {
        string currentDir = Directory.GetCurrentDirectory();
        string folderPath = Path.Combine(currentDir, "Mods", NameNoSpaces, "Files");
        Directory.CreateDirectory(folderPath);
        CreateFile(NameNoSpaces, $@"using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace {NameNoSpaces}
{{
    \t[BepInPlugin(GUID, Name, Version)]
    [BepInDependency(""Isle Goblin"")]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID, BepInDependency.DependencyFlags.HardDependency)]

    public class {Name} : BaseUnityPlugin
    {{
        public const string GUID = ""org.bepinex.plugins.{NameNoSpaces}"";
        public const string Name = ""{Name}"";
        public const string Version = ""1.0.0"";
        
        public static ConfigEntry<bool> mEnabled;
        public ConfigDefinition mEnabledDef = new ConfigDefinition(pluginVersion, ""Enable/Disable Mod"");

        public {NameNoSpaces}()
        {{
            mEnabled = Config.Bind(mEnabledDef, false, new ConfigDescription(
                ""Controls if the mod should be enabled or disabled"", null, 
                new ConfigurationManagerAttributes {{ Order = 0 }}
            ));
        }}
        
        void Awake()
        {{
            Harmony.CreateAndPatchAll(typeof({NameNoSpaces}));
        }}

        void Update()
        {{

        }}
    }}
}}");

    }

    public string GetFolderPath()
    {
        string currentDir = Directory.GetCurrentDirectory();
        return Path.Combine(currentDir, "Mods", NameNoSpaces, "Files");
    }
}