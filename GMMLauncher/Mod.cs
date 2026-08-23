using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaEdit;
using GMMLauncher.ViewModels;
using GMMLauncher.Views;
using Tabalonia.Controls;

namespace GMMLauncher;

public class Mod
{
    public Mod() { } // DO NOT REMOVE
    
    public Mod(string modName, string modDescription, string modAuthors, string? gmmVersion, string version)
    {
        Name = modName;
        NameNoSpaces = modName.Replace(" ", string.Empty);
        Description = modDescription;
        Authors = modAuthors;
        GMMVersion = gmmVersion;
        Version = version;
    }

    public Mod(string filePath, bool usingName = false)
    {
        if (usingName)
        {
            filePath = filePath.Replace(" ", string.Empty);
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "Mods", filePath, filePath + ".json");
        }
        
        using (Stream fileStream = new FileStream(filePath, FileMode.Open))
        {
            if (!Path.Exists(filePath))
            {
                new InfoWindow("Error Loading Mods", InfoWindowType.Error, $"Mod file not found. Using path {filePath}", true, fontSize:20).Show();
            }

            Mod? modData;
            try
            {
                modData = JsonSerializer.Deserialize<Mod>(fileStream, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });

            }
            catch (Exception e)
            {
                new InfoWindow("Error Loading Mods", InfoWindowType.Error, $"Mod file could not be loaded. Using path {filePath}", true, fontSize:20).Show();
                throw;
            }

            if (modData != null)
            {
                Name = modData.Name;
                NameNoSpaces = modData.Name.Replace(" ", string.Empty);
                Description = modData.Description;
                Authors = modData.Authors;
                GMMVersion = modData.GMMVersion;
                Version = modData.Version;
            }
        }
    }

    public string Name { get; set; }
    public string NameNoSpaces { get; set; }
    public string Description { get; set; }
    public string Authors { get; set; }
    public string? GMMVersion { get; set; }
    public string Version { get; set; } = "1.0.0";
    private string CsprojTemplate => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <RootNamespace>{NameNoSpaces}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""BepInEx"">
      <HintPath>Libraries/BepInEx.dll</HintPath>
    </Reference>
    <Reference Include=""Assembly-CSharp"">
      <HintPath>Libraries/Assembly-CSharp.dll</HintPath>
    </Reference>
    <Reference Include=""HarmonyX"">
      <HintPath>Libraries/0Harmony.dll</HintPath>
    </Reference>
    <Reference Include=""BepInEx.Harmony"">
      <HintPath>Libraries/BepInEx.Harmony.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine"">
      <HintPath>Libraries/UnityEngine.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine.CoreModule"">
      <HintPath>Libraries/UnityEngine.CoreModule.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine.UI"">
      <HintPath>Libraries/UnityEngine.UI.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine.IMGUIModule"">
      <HintPath>Libraries/UnityEngine.IMGUIModule.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine.AddressableAssets"">
      <HintPath>Libraries\Unity.Addressables.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine.AssetBundleModule"">
      <HintPath>Libraries\UnityEngine.AssetBundleModule.dll</HintPath>
    </Reference>
    <Reference Include=""UnityEngine.InputLegacyModule"">
      <HintPath>Libraries\UnityEngine.InputLegacyModule.dll</HintPath>
    </Reference>
    <Reference Include=""GoblinManager"">
      <HintPath>Z:\SteamLibrary\steamapps\common\Isle Goblin Playtest\BepInEx\plugins\GoblinManager_1.0.0\GoblinManager.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
";
    
    private string csprojPath => Path.Combine(GetFolderPath(), $"{NameNoSpaces}.csproj");
    
    public void SaveMod()
    {
        string filePath = GetModFilePath();
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(filePath, json);
    }
    public void SaveFiles(CodeEditor editor)
    {
        foreach (var tab in editor.viewModel.TabItems)
        {
            SaveFile(tab);
        }
        SaveMod();
    }

    public void SaveFile(TabItemViewModel tab)
    {
        if (tab.FileName.EndsWith("*"))
            tab.FileName = tab.FileName.Substring(0, tab.FileName.Length - 1);
        string filePath = tab.FilePath;
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
    

    public void CreateMainFile()
    {
        if (File.Exists(GetFilePath())) return;
        string currentDir = Directory.GetCurrentDirectory();
        string folderPath = Path.Combine(currentDir, "Mods", NameNoSpaces, "Files");
        Directory.CreateDirectory(folderPath);
        CreateFile(NameNoSpaces, $@"using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using GoblinManager;

namespace {NameNoSpaces}
{{
    [BepInDependency(""GoblinManager"", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(""gmm.{NameNoSpaces}"", ""{Name}"", ""{Version}"")]
    [ModDescription(""{Description}"")]
    public class {NameNoSpaces} : Mod
    {{
        public override void OnModLoaded(AssetBundle bundle)
        {{
            Debug.Log(""{Name} Loaded"");   
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
    
    public string GetFilePath()
    {
        return Path.Combine(GetFolderPath(), "Files", NameNoSpaces + ".cs");
    }
    
    public string GetModFilePath()
    {
        return Path.Combine(GetFolderPath(), NameNoSpaces + ".json");;
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
    
    
    public void OverwriteCsproj()
    {
        File.WriteAllText(csprojPath, CsprojTemplate);
    }
    
    public void ConfigureMod(CodeEditor? _editor = null, Action<string> invokeOnComplete = null)
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
            if (_editor == null)
            {
                _editor = WindowManager.SearchForModsInCodeEditor(GetModFilePath());
            }
            
            bool editorFunctions = _editor != null;
            string modName = (answers[0] as TextBox)?.Text ?? Name;
            string modDesc = (answers[1] as TextBox)?.Text ?? Description;
            string modDevelopers = (answers[2] as TextBox)?.Text ?? Authors;
            string modVersion = (answers[3] as TextBox)?.Text ?? Version;

            
            if (modName != Name || modVersion != Version || modDesc != Description || modDevelopers != Authors)
            {
                if (editorFunctions) SaveFiles(_editor);
                string modNameNoSpaces = modName.Replace(" ", "");

                TabsControl savedTabControl = null;
                if (editorFunctions)
                {
                    savedTabControl = _editor._tabControl;
                    _editor.fileTree.Items.Clear();
                    _editor._tabControl = null;
                    _editor.Close();
                }


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
                catch (Exception ex)
                {
                    new InfoWindow("Mod couldn't be moved", InfoWindowType.Error,
                        "Mod files couldn't be moved. This is most likely because you have a mod file open in another application or in the File Explorer\n" + ex.Message,
                        true).Show();
                    promptWindow.Close();
                    return;
                }
                
                string filesDir = Path.Combine(newModDirectory, "Files");
                string oldMainFileName = NameNoSpaces + ".cs";
                string newMainFileName = modNameNoSpaces + ".cs";


                foreach (var file in Directory.GetFiles(filesDir, "*.cs"))
                {
                    string code = File.ReadAllText(file);

                    code = Regex.Replace(code, $@"\b{Regex.Escape(NameNoSpaces)}\b", modNameNoSpaces);
                    code = code.Replace(Name, modName);

                    code = Regex.Replace(code,
                        @"\[BepInPlugin\(\s*""[^""]+"",\s*""[^""]+"",\s*""[^""]+""\s*\)\]",
                        $"[BepInPlugin(\"gmm.{modNameNoSpaces.ToLower()}\", \"{modName}\", \"{modVersion}\")]");

                    if (Regex.IsMatch(code, @"\[ModDescription\("".*""\)\]"))
                    {
                        code = Regex.Replace(code,
                            @"\[ModDescription\("".*""\)\]",
                            $"[ModDescription(\"{modDesc}\")]");
                    }

                    code = Regex.Replace(code,
                        @"const string Version\s*=\s*""[^""]+""",
                        $"const string Version = \"{modVersion}\"");

                    if (Path.GetFileName(file) == oldMainFileName)
                    {
                        File.Delete(file);
                        string newPath = Path.Combine(filesDir, newMainFileName);
                        File.WriteAllText(newPath, code);
                    }
                    else
                    {
                        File.WriteAllText(file, code);
                    }
                }


                File.Delete(Path.Combine(newModDirectory, NameNoSpaces + ".json"));
                    
                Name = modName;
                NameNoSpaces = modNameNoSpaces;
                Description = modDesc;
                Authors = modDevelopers;
                Version =  modVersion;
                
                SaveMod();
                invokeOnComplete.Invoke(modName);

                if (editorFunctions)
                {
                    var editor = new CodeEditor(this)
                    {
                        TabControl = savedTabControl
                    };
                    foreach (var tab in _editor.viewModel.TabItems)
                    {
                        if (tab.FileName == oldMainFileName)
                        {
                            string mainFileCode = File.ReadAllText(Path.Combine(filesDir, newMainFileName));
                            ((tab.Content as TextCodeEditor).Content as TextEditor).Text = mainFileCode;
                        }
                    }
                    editor.Show();
                }
            }
            promptWindow.Close();
        }
    }

    public void DeleteMod(Action invokeOnComplete = null)
    {
        new InfoWindow("Are you sure?", InfoWindowType.YesNo, $"WARNING: This action is irreversible. Do you want to delete the mod: {Name}", true,
            window =>
            {
                if (WindowManager.SearchForModsInCodeEditor(GetModFilePath()) is CodeEditor codeEditor)
                {
                    codeEditor.Close();
                }
                Directory.Delete(Path.Combine(GetFolderPath()), true);
                App.RecentProjects.VerifyProjects();
                invokeOnComplete.Invoke();
                window.Close();
            }).Show();
    }

    public void OpenInExplorer()
    {
        string filePath = GetModFilePath();
        if (File.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
        else if (Directory.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
        else
        {
            new InfoWindow("Error: Cannot find mod", InfoWindowType.Error, 
                $"Unable to find mod with path {filePath}", true, (window) =>
                {
                    window.Close();
                }).Show();
        }
    }
    
    #region Install/Building Mod
    public async Task InstallMod(InfoWindow infoWindow, CodeEditor editor, bool quickBuild = false)
    {
        SaveFiles(editor);
        
        string path = await CreateModFiles(infoWindow, quickBuild);
        if (path == null)
        {
            infoWindow.Close();
            return;
        }
        infoWindow.UpdateInfoText("Running Dotnet Build...");
        if (!BuildMod(path, out string errorMessage))
        {
            infoWindow.ChangeWindowType("Build Failed",InfoWindowType.Error, errorMessage, true, height:400, width:600);
            return;
        }
        
        string modFolderName = $"{NameNoSpaces}_{Version}";
        string pluginFolderName = Path.Combine(App.Settings.SteamDirectory, "BepInEx", "plugins");
        string installPath = Path.Combine(pluginFolderName, modFolderName);
        Directory.CreateDirectory(installPath);


        File.Copy(Path.Combine(path, "manifest.json"), Path.Combine(installPath, "manifest.json"), true);
        File.Copy(Path.Combine(path, "README.md"), Path.Combine(installPath, "README.md"), true);
        if (File.Exists(Path.Combine(path, "CHANGELOG.md"))) File.Copy(Path.Combine(path, "CHANGELOG.md"), Path.Combine(installPath, "CHANGELOG.md"), true);
        
        string dllPath = Path.Combine(path, "bin", "Debug", "netstandard2.1", $"{NameNoSpaces}.dll");
        File.Copy(dllPath, Path.Combine(installPath, $"{NameNoSpaces}.dll"), true);
        
        
        if (infoWindow.windowType != InfoWindowType.Error)
        {
            if (!quickBuild)
            {
                infoWindow.ChangeWindowType("Build Successful", InfoWindowType.YesNo,"Mod Successfully Installed. Would you like the mod compiled in a .zip (easier to share the mod)?", true,
                async void (_) =>
                {
                    string zipPath = Path.Combine(pluginFolderName, $"{NameNoSpaces}.zip");
                    await Task.Run(() =>
                    {
                        if (File.Exists(zipPath))
                            File.Delete(zipPath);
                        ZipFile.CreateFromDirectory(installPath, zipPath);
                    });
    
                    AskToOpenPluginFolder();
                },
                (_) =>
                {
                    AskToOpenPluginFolder();
                });
            }
            else
            {
                infoWindow.ChangeWindowType("Build Successful", InfoWindowType.Ok,"Mod Successfully Installed.", true,
                    async void (window) =>
                    {
                        window.Close();
                    });
                
                if (App.Settings.ZipMod)
                {
                    string zipPath = Path.Combine(pluginFolderName, $"{NameNoSpaces}.zip");
                    await Task.Run(() =>
                    {
                        if (File.Exists(zipPath))
                            File.Delete(zipPath);
                        ZipFile.CreateFromDirectory(installPath, zipPath);
                    });
                }
                if (App.Settings.OpenPluginFolder)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installPath,
                        UseShellExecute = true
                    });
                }
            }

            void AskToOpenPluginFolder()
            {
                infoWindow.ChangeWindowType("Build Successful", InfoWindowType.YesNo,"Would you like to open the plugin folder", true,
                async void (window) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installPath,
                        UseShellExecute = true
                    });
                    window.Close();
                },
                (window) =>
                {
                    window.Close();
                });
            }
        }
    }
    public async Task<string> CreateModFiles(InfoWindow? infoWindow = null, bool quickBuild = false)
    {
        string projectRoot = GetFolderPath();
        Directory.CreateDirectory(projectRoot);
        
        await ShowOverwritePrompt();

        infoWindow?.UpdateInfoText("Creating Mod Files...");
        
        var manifest = new
        {
            mod_name = Name,
            version = Version,
            description = Description,
            mod_maker_version = GMMVersion,
            authors = Authors.Split(',')
        };
        
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        string readme = $"# {Name}\n\n## Description\n{Description}\n\n## Version\n{Version}\n\n## Developers\n{Authors}\n\n## Installation\nRequires BepInEx.";
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "README.md"), readme);

        string changelogPath = Path.Combine(projectRoot, "CHANGELOG.md");
        if (!quickBuild) await ShowChangelogPrompt(changelogPath);
        try
        {
            string librariesPath = Path.Combine(projectRoot, "Libraries");
            Directory.CreateDirectory(librariesPath);

            CopyDirectory(Path.Combine(App.Settings.SteamDirectory, "Isle Goblin_Data", "Managed"), librariesPath);
            CopyDirectory(Path.Combine(App.Settings.SteamDirectory, "BepInEx/core"), librariesPath);
        }
        catch (DirectoryNotFoundException)
        {
            new InfoWindow("Error While Making Mod Files", InfoWindowType.YesNo, 
                "Could not create mod files, this is because BepInEx is not installed. Would you like to install it now? After please retry building.", true, (window) =>
                {
                    _ = App.Settings.InstallBepInEx();
                    window.Close();
                }).Show();
            return null;
        }

        return projectRoot;
        
        Task ShowChangelogPrompt(string changelogPath)
        {
            infoWindow?.UpdateInfoText("Waiting For Changelog Entry...");
            var tcs = new TaskCompletionSource<bool>();
            var window = new PromptWindow("Changelog Entry", new List<(Type, string, object?, bool)>
            {
                (typeof(TextBox), "Enter Changelog Entry", "", true)
            }, (list, window) =>
            {
                infoWindow?.UpdateInfoText("Running Dotnet Build...");
                string entry = (list[0] as TextBox).Text;
                string changelogEntry = $"## v{Version} - {DateTime.Now:yyyy-MM-dd}\n- {entry}.\n";
                File.AppendAllText(changelogPath, changelogEntry);
                window.Close();
                tcs.SetResult(true);
            }, (window) =>
            {
                // string changelogEntry = $"## v{Version} - {DateTime.Now:yyyy-MM-dd}\n- [ADD CHANGES].\n";
                // File.AppendAllText(changelogPath, changelogEntry);
                infoWindow?.UpdateInfoText("Running Dotnet Build...");
                window.Close();
                tcs.SetResult(true);
            }, cancelText: "Skip");
            window.Topmost = true;
            window.Show();
            
            return tcs.Task;
        }
        
        Task ShowOverwritePrompt()
        {
            // infoWindow?.UpdateInfoText("Waiting For Overwrite Choice...");
            var tcs = new TaskCompletionSource<bool>();
            
            // if (Path.Exists(csprojPath))
            // {
            //     if (File.ReadAllText(csprojPath).Equals(CsprojTemplate))
            //     {
            //         if (!quickBuild)
            //         {
            //             var window = new PromptWindow("Overwrite Csproj? ", new List<(Type, string, object?, bool)>
            //             {
            //                 (typeof(CheckBox), "The .csproj file has been modified \ndo you want to overwrite it?", false, true)
            //             }, (list, window) =>
            //             {
            //                 if ((list[0] as CheckBox)?.IsChecked == true)
            //                 {
            //                     OverwriteCsproj();
            //                 }
            //                 tcs.SetResult(true);
            //                 window.Close();
            //             }, window =>
            //             {
            //                 window.Close();
            //                 tcs.SetResult(true);
            //             });
            //             window.Topmost = true;
            //             window.Show();
            //         }
            //         else
            //         {
            //             if (App.Settings.OverwriteCsproj) OverwriteCsproj();
            //             tcs.SetResult(true);
            //         }
            //     }
            //     else
            //     {
            //         tcs.SetResult(true);
            //     }
            // }
            // else
            if (!Path.Exists(csprojPath))
            {
                OverwriteCsproj();
            }
            tcs.SetResult(true);
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
            Console.WriteLine(output);
            Console.WriteLine(errorOutput);
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
        var cleanedMessage = Regex.Replace(message, @"\s*\[\s*.*\]", string.Empty);
        return cleanedMessage.Replace(fullPath, "...");
    }
    #endregion
}