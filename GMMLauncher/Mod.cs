using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GMMLauncher;

public class Mod
{
    public Mod() { }
    
    public Mod(string modName, string modDescription, string modAuthors, string gmmVersion)
    {
        Name = modName;
        NameNoSpaces = modName.Replace(" ", string.Empty);
        Description = modDescription;
        Authors = modAuthors;
        GMMVersion = gmmVersion;
        Version = "1.0.0";
    }

    public string Name { get; init; }
    public string NameNoSpaces { get; init; }
    public string Description { get; init; }
    public string Authors { get; init; }
    public string GMMVersion { get; init; }
    public string Version { get; init; } = "1.0.0";


    public void SaveMod()
    {
        string filePath = Path.Combine(GetFolderPath(), NameNoSpaces + ".json");
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(filePath, json);
    }
    public void SaveFiles(string fileContent)
    {
        string currentDir = Directory.GetCurrentDirectory();
        string folderPath = Path.Combine(currentDir, "Mods", NameNoSpaces, "Files");
        // TODO: Add support for when tabs and an explorer is added
        
        File.WriteAllText(GetFilePath(), fileContent);
        SaveMod();
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
        if (File.Exists(GetFilePath())) return;
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
    [BepInPlugin(GUID, Name, Version)]
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
        SaveMod();
    }

    public string GetFolderPath()
    {
        string currentDir = Directory.GetCurrentDirectory();
        return Path.Combine(currentDir, "Mods", NameNoSpaces);
    }
}