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

    public static ICommand NewModCommand { get; } = new RelayCommand(NewMod);
    public static ICommand LoadExistingModCommand { get; } = new RelayCommand(LoadExistingMod);
    public static ICommand LoadModDialogCommand { get; } = new RelayCommand<Window>(LoadMod);
    public static ICommand QuitAppCommand { get; } = new RelayCommand(QuitApp);
    public static ICommand OpenSettingsInEditorCommand { get; } = new RelayCommand<CodeEditor>(OpenSettingsInEditor);
    public static ICommand OpenSettingsCommand { get; } = new RelayCommand(OpenSettings);
    public static ICommand OpenDocumentationCommand { get; } = new RelayCommand(OpenDocumentation);

    
    #region Mods
    public static void NewMod()
    {
        var window = new PromptWindow("New Mod",
            new List<(Type, string, object?, bool)>
            {
                (typeof(TextBox), "Mod Name:", "", true),
                (typeof(TextBox), "Description:", "", true),
                (typeof(TextBox), "Developers (Separate by comma):", "", true)
            }, 
            NewModDone
        );
        window.Show();
    }
    private static void NewModDone(List<Control> answers, Window promptWindow)
    {
        string modName = (answers[0] as TextBox)?.Text ?? string.Empty;
        string description = (answers[1] as TextBox)?.Text ?? string.Empty;
        string developers = (answers[2] as TextBox)?.Text ?? string.Empty;

        Mod mod = new(modName, description, developers,
            App.appVersion);
        mod.CreateMainFile();
    
        var editor = new CodeEditor(mod);
        editor.Show();
    
        promptWindow.Close();
    }
    
    public static void LoadExistingMod()
    {
        var availableMods = GetAvailableMods();
        StackPanel promptsPanel;

        if (availableMods != null && availableMods.Length > 0)
        {
            var window = new PromptWindow("Load Mod",
                new List<(Type, string, object?, bool)>
                {
                    (typeof(TextBox), "Mod Name:", "", false),
                    (typeof(TextBlock), "OR", "", false),
                    (typeof(StackPanel), "Select a Mod:", "", false),
                }, 
                LoadExistingModDone
            );
            
            promptsPanel = window.FindControl<StackPanel>("PromptsPanel")!;
            window.Height += availableMods.Length * 24;
            foreach (var modName in availableMods)
            {
                string modNameFixed = modName.Replace(@"Mods\", "");
                var button = new Button
                {
                    Content = modNameFixed,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                button.ContextMenu = new ContextMenu
                {
                    Items =
                    {
                        new MenuItem
                        {
                            Header = "Delete Mod",
                            Command = new RelayCommand((() => { DeleteMod(button); }))
                        }
                    }
                };
                
                button.Click += (_, _) =>
                {
                    LoadModFromFile(modNameFixed.Replace(" ", ""));
                    window.Close();
                };
                button.PointerPressed += (_, e) =>
                {
                    var pointerPoint = e.GetCurrentPoint(button);
                    if (pointerPoint.Properties.IsRightButtonPressed)
                    {
                        button.ContextMenu.Open(button);
                    }
                };
    
                promptsPanel.Children.Add(button);
            }
            window.Show();
        }
        else
        {
            new InfoWindow("Error Loading Mods", InfoWindowType.Error, "No Mods Found!", true, fontSize:20).Show();
        }

        void DeleteMod(Button button)
        {
            string modName = button.Content.ToString();
            new InfoWindow("Are You Sure?", InfoWindowType.YesNo, $"Are you sure you want to delete {modName}. This action is irreversible.", true,
                () =>
                {
                    Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Mods" , modName), true);
                    promptsPanel.Children.Remove(button);
                }).Show();
        }
    }

    private static string[] GetAvailableMods()
    {
        return Directory.GetDirectories("Mods");
    }

    public static void LoadExistingModDone(List<Control> answers, Window promptWindow)
    {
        string modName = (answers[0] as TextBox)?.Text ?? string.Empty;
        if (string.IsNullOrEmpty(modName))
        {
            new InfoWindow("Field Empty", InfoWindowType.Error, "Mod Name field is empty, enter a mod name or select a mod", true, fontSize: 20).Show();
            return;
        }
        LoadModFromFile(modName.Replace(" ", ""));
        promptWindow.Close();
    }
    
    public static async void LoadMod(Window window)
    {
        var selectedFolders = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select a Mod",
            AllowMultiple = false,
            FileTypeFilter = 
            [
                new FilePickerFileType("JSON Files")
                {
                    Patterns = ["*.json"]
                }
            ],
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
    #endregion
    
    public static void OpenSettingsInEditor(CodeEditor editor = null)
    {
        var window = new SettingsWindow(editor);
        window.Show();
    }
    public static void OpenSettings()
    {
        var window = new SettingsWindow();
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
                new InfoWindow("Couldn't Open Documentation", InfoWindowType.Error, ex.Message, true, fontSize:16).Show();
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