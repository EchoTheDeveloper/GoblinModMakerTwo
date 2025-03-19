using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaEdit;
using GMMLauncher.Views;


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
    public void SaveFiles(CodeEditor editor)
    {
        string currentDir = Directory.GetCurrentDirectory();
        string folderPath = Path.Combine(currentDir, "Mods", NameNoSpaces, "Files");
        foreach (var tab in editor._tabs)
        {
            string filePath = Path.Combine(GetFolderPath(), "Files", tab.Header.ToString()); // TODO: MIGHT HAVE TO ADD + ".cs
            TextEditor textEditor = (tab.Content as TextCodeEditor).Content as TextEditor;
            string code = textEditor.Text;
            File.WriteAllText(filePath, code);
        }
        SaveMod();
    }

    public void SaveFile(TabItem tab)
    {
        string filePath = Path.Combine(GetFolderPath(), "Files", tab.Header.ToString());
        TextEditor textEditor = (tab.Content as TextCodeEditor).Content as TextEditor;
        string code = textEditor.Text;
        File.WriteAllText(filePath, code);
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
    
    
    #region Install/Building Mod
        public void InstallMod(InfoWindow infoWindow, CodeEditor editor)
        {
            SaveFiles(editor);
            
            string path = CreateModFiles(editor);
            if (!BuildMod(path, out string errorMessage))
            {
                infoWindow.ChangeWindowType("Build Failed",InfoWindowType.Error, errorMessage, true, height:400, width:600);
                return;
            }
    
            string modFolderName = $"{Name}_{Version}";
            string installPath = Path.Combine(App.Settings.SteamDirectory, "BepInEx", "plugins", modFolderName);
            Directory.CreateDirectory(installPath);
    
            string dllPath = Path.Combine(path, "bin", "Debug", "netstandard2.1", $"{NameNoSpaces}.dll");
            File.Copy(dllPath, Path.Combine(installPath, $"{NameNoSpaces}.dll"), true);
    
            File.Copy(Path.Combine(path, "manifest.json"), Path.Combine(installPath, "manifest.json"), true);
            File.Copy(Path.Combine(path, "README.md"), Path.Combine(installPath, "README.md"), true);
            File.Copy(Path.Combine(path, "CHANGELOG.md"), Path.Combine(installPath, "CHANGELOG.md"), true);
    
            if (infoWindow.windowType != InfoWindowType.Error)
            {
                infoWindow.ChangeWindowType("Build Successful", InfoWindowType.Ok,"Mod Successfully Installed.", true);
            }
        }
    public string CreateModFiles(Window window)
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string projectRoot = Path.Combine(currentDirectory, "Mods", NameNoSpaces);

        Directory.CreateDirectory(projectRoot);

        File.Copy("resources/gitignoretemplate", Path.Combine(projectRoot, ".gitignore"), true);
        File.Copy("resources/configmanagertemplate", Path.Combine(projectRoot, "ConfigurationManagerAttributes.cs"), true);

        string csprojTemplate = File.ReadAllText("resources/csprojtemplate").Replace("{{mod_name}}", NameNoSpaces);
        File.WriteAllText(Path.Combine(projectRoot, $"{NameNoSpaces}.csproj"), csprojTemplate);

        var manifest = new
        {
            mod_name = Name,
            version = Version,
            description = Description,
            mod_maker_version = GMMVersion,
            authors = Authors.Split(',')
        };
        File.WriteAllText(Path.Combine(projectRoot, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        string readme = $"# {Name}\n\n## Description\n{Description}\n\n## Version\n{Version}\n\n## Developers\n{Authors}\n\n## Installation\nRequires BepInEx.";
        File.WriteAllText(Path.Combine(projectRoot, "README.md"), readme);

        string changelogPath = Path.Combine(projectRoot, "CHANGELOG.md");
        string changelogEntry = $"## v{Version} - {DateTime.Now:yyyy-MM-dd}\n- [ADD CHANGES].\n";
        File.AppendAllText(changelogPath, changelogEntry);
        
        try
        {
            string librariesPath = Path.Combine(projectRoot, "Libraries");
            Directory.CreateDirectory(librariesPath);

            CopyDirectory(Path.Combine(App.Settings.SteamDirectory, "Isle Goblin_Data", "Managed"), librariesPath);
            CopyDirectory(Path.Combine(App.Settings.SteamDirectory, "BepInEx/core"), librariesPath);
            CopyDirectory(Path.Combine(Directory.GetCurrentDirectory(), "resources/Default Libraries"), librariesPath);
        }
        catch (DirectoryNotFoundException e)
        {
            new InfoWindow("Error While Making Mod Files", InfoWindowType.YesNo, "Could not create mod files, this is because BepInEx is not installed. Would you Like to install it now?.", true,
                () =>
                {
                    App.Settings.InstallBepInEx();
                }).Show();
            return null;
        }


        return projectRoot;
    }
    
    public bool BuildMod(string path, out string errorMessage)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build",
            WorkingDirectory = path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        using (Process process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n').Concat(errorOutput.Split('\n'));

            var errors = lines
                .Where(line => line.Contains(": error CS"))
                .Select(line => ShortenPath(line, path))
                .Distinct()
                .ToList();

            int warningCount = lines.Count(line => line.Contains(": warning CS"));
            int errorCount = errors.Count;

            errorMessage = $"=== Build Log ===\n\n";

            if (errors.Any())
            {
                errorMessage += "Error: " + string.Join("\nError: ", errors) + "\n";
            }

            errorMessage += $"\n=== Build FAILED ===\n";
            errorMessage += $"\n    {warningCount} Warning(s)\n";
            errorMessage += $"    {errorCount} Error(s)\n";
            errorMessage += $"\nTime Elapsed {process.ExitTime - process.StartTime:hh\\:mm\\:ss\\:ff}\n";
            errorMessage += "=====================";

            return errorCount == 0;
        }
    }
    
    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);

        foreach (var directory in Directory.GetDirectories(sourceDir))
            CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
    }

    private string ShortenPath(string message, string basePath)
    {
        string fullPath = Path.GetFullPath(basePath);
        var cleanedMessage = System.Text.RegularExpressions.Regex.Replace(message, @"\s*\[\s*.*\]", string.Empty);
        return cleanedMessage.Replace(fullPath, "...");
    }
    #endregion
}