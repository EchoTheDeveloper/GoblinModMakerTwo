using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GMMLauncher.ViewModels;
using GMMLauncher.Views;

namespace GMMLauncher;

public static class MenuCommands
{
    private const string DocumentationUrl = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

    public static ICommand OpenDocumentationCommand { get; } = new RelayCommand(OpenDocumentation);
    public static ICommand QuitAppCommand { get; } = new RelayCommand(QuitApp);
    public static ICommand NewModCommand { get; } = new RelayCommand(NewMod);
    public static ICommand OpenSettingsCommand { get; } = new RelayCommand(OpenSettings);
    public static ICommand OpenHarmonyPatchCommand { get; } = new RelayCommand(OpenHarmonyPatch);
    public static ICommand LoadExistingModCommand { get; } = new RelayCommand(LoadExistingMod);
    public static ICommand LoadModDialogCommand { get; } = new RelayCommand<Window>(LoadMod);
    
    public static void NewMod()
    {
        var window = new PromptWindow("New Mod",
            new List<(Type, string, string)>
            {
                (typeof(TextBox), "Mod Name:", ""),
                (typeof(TextBox), "Description:", ""),
                (typeof(TextBox), "Developers (Separate by comma):", "")
            }, 
            NewFileDone
        );
        window.Show();
    }
    private static void NewFileDone(List<Control> answers, Window promptWindow)
    {
        string modName = (answers[0] as TextBox)?.Text ?? string.Empty;
        string description = (answers[1] as TextBox)?.Text ?? string.Empty;
        string developers = (answers[2] as TextBox)?.Text ?? string.Empty;

        Mod mod = new(modName, description, developers,
            Assembly.GetExecutingAssembly().GetName().Version.ToString());
        mod.CreateMainFile();
    
        var editor = new CodeEditor(mod);
        editor.Show();
    
        promptWindow.Close();
    }
    
    public static void LoadExistingMod()
    {
        var availableMods = GetAvailableMods();

        if (availableMods != null && availableMods.Length > 0)
        {
            var window = new PromptWindow("Load Mod",
                new List<(Type, string, string)>
                {
                    (typeof(TextBox), "Mod Name:", ""),
                    (typeof(TextBlock), "OR", ""),
                    (typeof(StackPanel), "Select a Mod:", ""),
                }, 
                LoadExistingModDone
            );
            
            var promptsPanel = window.FindControl<StackPanel>("PromptsPanel")!;
            
            foreach (var modName in availableMods)
            {
                string modNameFixed = modName.Replace(@"Mods\", "");
                var button = new Button
                {
                    Content = modNameFixed,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                button.Click += (_, _) =>
                {
                    LoadModFromFile(modNameFixed.Replace(" ", ""));
                    window.Close();
                };
    
                promptsPanel.Children.Add(button);
            }
            window.Show();
        }
        else
        {
            var window = new PromptWindow("Load Mod",
                new List<(Type, string, string)>
                {
                    (typeof(TextBlock), "No Mods Found!", ""),
                }
            );
            window.Show();
        }
    }

    private static string[] GetAvailableMods()
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Mods");
        return Directory.GetDirectories("Mods");
    }

    public static void LoadExistingModDone(List<Control> answers, Window promptWindow)
    {
        string modName = (answers[0] as TextBox)?.Text ?? string.Empty;
        LoadModFromFile(modName.Replace(" ", ""));
        promptWindow.Close();
    }
    
    public static async void LoadMod(Window window)
    {
        var selectedFolders = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select a Mod",
            AllowMultiple = false,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                Path.Combine(Directory.GetCurrentDirectory(), "Mods"))
        });

        var selectedFolder = selectedFolders.FirstOrDefault();
        if (selectedFolder != null)
        {
            LoadModFromFile(filePath: selectedFolder.Path.ToString().Replace("file:///", ""));
        }
    }
    
    public static void LoadModFromFile(string folderName = "", string filePath = "")
    {
        if (filePath == "" && folderName != "") filePath = Path.Combine(Directory.GetCurrentDirectory(),"Mods", folderName, folderName + ".json");
            
        Mod mod = null;
            
        using (Stream fileStream = new FileStream(filePath, FileMode.Open))
        {
            var modData = JsonSerializer.Deserialize<Mod>(fileStream, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });

            if (modData != null)
            {
                string modName = modData.Name;
                string description = modData.Description;
                string developers = modData.Authors;
                string gmmVersion = modData.GMMVersion;

                mod = new Mod(modName, description, developers, gmmVersion);
            
                mod.CreateMainFile();
            }
        }

        var editor = new CodeEditor(mod);
        editor.Show();
    }
    
    public static void OpenSettings()
    {
        var window = new SettingsWindow();
        window.Show();
    }

    public static void OpenHarmonyPatch()
    {
        var window = new PromptWindow("Create Harmony Patch",
        new List<(Type, string, string)>
        {
            (typeof(TextBox), "Function Name:", ""),
            (typeof(TextBox), "Function's Class", ""),
            (typeof(TextBox), "Parameters (Separate by comma):", ""),
            (typeof(TextBox), "Patch Type (Prefix, Postfix):", "Prefix"),
            (typeof(TextBox), "Return Type:", "None"),
            (typeof(TextBox), "Have Instance:", "False"),
        }
);

        window.Show();
    }

    private static void OpenDocumentation()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocumentationUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening documentation: {ex.Message}");
            }
        });
    }

    private static void QuitApp()
    {
        Dispatcher.UIThread.Post(() => 
        {
            Environment.Exit(0);
        });
    }
}