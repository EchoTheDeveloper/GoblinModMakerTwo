using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
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

    public string Name { get; set; }
    public string NameNoSpaces { get; set; }
    public string Description { get; set; }
    public string Authors { get; set; }
    public string GMMVersion { get; set; }
    public string Version { get; set; } = "1.0.0";


    public void SaveMod()
    {
        string filePath = Path.Combine(GetFolderPath(), NameNoSpaces + ".json");
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(filePath, json);
    }
    public void SaveFiles(CodeEditor editor)
    {
        foreach (var tab in editor._tabs)
        {
            string filePath = Path.Combine(GetFileFolderPath(), tab.Header.ToString()); // TODO: MIGHT HAVE TO ADD + ".cs
            TextEditor textEditor = (tab.Content as TextCodeEditor).Content as TextEditor;
            string code = textEditor.Text;
            File.WriteAllText(filePath, code);
        }
        SaveMod();
    }

    public void SaveFile(TabItem tab)
    {
        string filePath = Path.Combine(GetFileFolderPath(), tab.Header.ToString());
        TextEditor textEditor = (tab.Content as TextCodeEditor).Content as TextEditor;
        textEditor.IsModified = false;
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

    public class {NameNoSpaces} : BaseUnityPlugin
    {{
        public const string GUID = ""org.bepinex.plugins.{NameNoSpaces}"";
        public const string Name = ""{Name}"";
        public const string Version = ""{Version}"";
        
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

    public string GetFileFolderPath()
    {
        return Path.Combine(GetFolderPath(), "Files");
    }

    public void ConfigureMod(CodeEditor _editor)
    {
        var window = new PromptWindow("Configure Mod", 
            new List<(Type, string, object?, bool)>
            {
                (typeof(TextBox), "Name", Name, true),
                (typeof(TextBox), "Description", Description, true),
                (typeof(TextBox), "Developers (Separate By Comma)", Authors, true),
                (typeof(TextBox), "Version", Version, true),
            }, ConfigureModDone);
        window.Show();

        void ConfigureModDone(List<Control> answers, Window promptWindow)
        {
            string modName = (answers[0] as TextBox)?.Text ?? Name;
            string modDesc = (answers[1] as TextBox)?.Text ?? Description;
            string modDevelopers = (answers[2] as TextBox)?.Text ?? Authors;
            string modVersion = (answers[3] as TextBox)?.Text ?? Version;

            
            if (modName != Name || modVersion != Version || modDesc != Description || modDevelopers != Authors)
            {
                SaveFiles(_editor);
                string modNameNoSpaces = modName.Replace(" ", "");
                TabControl savedTabControl = _editor._tabControl;
                _editor.fileTree.Items.Clear();
                _editor._tabControl = null;
                _editor.Close();


                string modFolder = Path.Combine(Directory.GetCurrentDirectory(), "Mods");
                string modDirectory = Path.Combine(modFolder, NameNoSpaces);
                string newModDirectory = Path.Combine(modFolder, modNameNoSpaces);
                try
                {
                    if (Directory.Exists(modDirectory) && !Directory.Exists(newModDirectory))
                    {
                        Directory.Move(modDirectory, newModDirectory);
                    }
                }
                catch (Exception _)
                {
                    new InfoWindow("Mod couldn't be moved", InfoWindowType.Error,
                        "Mod files couldn't be moved. This is most likely because you have a mod file open in another application or in the File Explorer",
                        true).Show();
                    promptWindow.Close();
                    return;
                }
                
                string oldMainFileName = NameNoSpaces + ".cs";
                string oldFilePath = Path.Combine(newModDirectory, "Files", oldMainFileName);
                    
                string mainFileCode = File.ReadAllText(oldFilePath);

                mainFileCode = Regex.Replace(mainFileCode, $@"\b{Regex.Escape(NameNoSpaces)}\b", modNameNoSpaces);
                mainFileCode = mainFileCode.Replace(Name, modName);
                mainFileCode = Regex.Replace(mainFileCode, @"const string Version = ""[^""]+""", $"const string Version = \"{modVersion}\"");
                
                File.WriteAllText(Path.Combine(newModDirectory, "Files", modNameNoSpaces + ".cs"), mainFileCode);
                
                File.Delete(oldFilePath);
                File.Delete(Path.Combine(newModDirectory, NameNoSpaces + ".json"));
                    
                Name = modName;
                NameNoSpaces = modNameNoSpaces;
                Description = modDesc;
                Authors = modDevelopers;
                Version =  modVersion;
                
                SaveMod();
                
                    
                var editor = new CodeEditor(this)
                {
                    TabControl = savedTabControl
                };
                foreach (var tab in _editor._tabs)
                {
                    if (tab.Header.ToString() == oldMainFileName)
                    {
                        ((tab.Content as TextCodeEditor).Content as TextEditor).Text = mainFileCode;
                    }
                }
                editor.Show();
            }
            promptWindow.Close();
        }
    }
    
    
    #region Install/Building Mod
        public async Task InstallMod(InfoWindow infoWindow, CodeEditor editor)
        {
            SaveFiles(editor);
            
            string path = await CreateModFiles(infoWindow);
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
                infoWindow.ChangeWindowType("Build Successful", InfoWindowType.Ok,"Mod Successfully Installed. On clicking Ok the mod folder path will open.", true,
                    () =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = installPath,
                            UseShellExecute = true
                        });
                    });
            }
        }
    public async Task<string> CreateModFiles(InfoWindow infoWindow = null)
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

        await ShowChangelogPrompt(changelogPath);
        
        try
        {
            string librariesPath = Path.Combine(projectRoot, "Libraries");
            Directory.CreateDirectory(librariesPath);

            CopyDirectory(Path.Combine(App.Settings.SteamDirectory, "Isle Goblin_Data", "Managed"), librariesPath);
            CopyDirectory(Path.Combine(App.Settings.SteamDirectory, "BepInEx/core"), librariesPath);
            CopyDirectory(Path.Combine(Directory.GetCurrentDirectory(), "resources/Default Libraries"), librariesPath);
        }
        catch (DirectoryNotFoundException)
        {
            new InfoWindow("Error While Making Mod Files", InfoWindowType.YesNo, 
                "Could not create mod files, this is because BepInEx is not installed. Would you like to install it now?", true,
                () =>
                {
                    App.Settings.InstallBepInEx();
                }).Show();
            return null;
        }

        return projectRoot;
        
        Task ShowChangelogPrompt(string changelogPath)
        {
            var tcs = new TaskCompletionSource<bool>();

            var window = new PromptWindow("Changelog Entry", new List<(Type, string, object?, bool)>
            {
                (typeof(TextBox), "Enter Changelog Entry", "", true)
            }, (list, window) =>
            {
                string entry = (list[0] as TextBox).Text;
                string changelogEntry = $"## v{Version} - {DateTime.Now:yyyy-MM-dd}\n- {entry}.\n";
                File.AppendAllText(changelogPath, changelogEntry);
                if (infoWindow != null)
                {
                    infoWindow.UpdateInfoText("Running Dotnet Build...");
                }
                window.Close();
                tcs.SetResult(true);
            }, (window) =>
            {
                string changelogEntry = $"## v{Version} - {DateTime.Now:yyyy-MM-dd}\n- [ADD CHANGES].\n";
                File.AppendAllText(changelogPath, changelogEntry);
                if (infoWindow != null)
                {
                    infoWindow.UpdateInfoText("Running Dotnet Build...");
                }
                window.Close();
                tcs.SetResult(true);
            }, cancelText: "Skip");
            window.Topmost = true;
            window.Show();
            
            return tcs.Task;
        }
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
            CreateNoWindow = true
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