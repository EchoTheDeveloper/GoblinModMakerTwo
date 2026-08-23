using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GMMBackend;
using GMMLauncher.ViewModels;
using GMMLauncher.Views;

namespace GMMLauncher;

public static class MenuCommands
{
    private const string DocumentationUrl = "https://github.com/EchoTheDeveloper/GoblinModMakerTwo/wiki";
    private const string AboutUrl = "https://github.com/EchoTheDeveloper/GoblinModMakerTwo";
    private const string IssuesUrl = "https://github.com/EchoTheDeveloper/GoblinModMakerTwo/issues";
    
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

        Mod mod = new(modName, description, developers, App.appVersion, "1.0.0");
        
        if (Path.Exists(mod.GetFolderPath()))
        {
            new InfoWindow("Duplicate Mod Name", InfoWindowType.Error, $"There is already a mod by name {modName}", true).Show();
            return;
        }
        
        mod.CreateMainFile();
    
        var editor = new CodeEditor(mod);
        editor.Show();
        
        App.RecentProjects.AddRecentProject(new ModInfo()
        {
            Name = modName,
            Path = mod.GetModFilePath(),
            LastOpened = DateTime.Now,
        });
    
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
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Mod mod = new Mod(modNameFixed, true);

                button.ContextMenu = new ContextMenu
                {
                    Items =
                    {
                        new MenuItem
                        {
                            Header = "Configure Mod",
                            Command = new RelayCommand(() => { mod.ConfigureMod(invokeOnComplete: s => { button.Content = s; }); })
                        },
                        new MenuItem
                        {
                            Header = "Open Mod In Explorer",
                            Command = new RelayCommand(() => { mod.OpenInExplorer(); })
                        },
                        new Separator(),
                        new MenuItem
                        {
                            Header = "Delete Mod",
                            Command = new RelayCommand(() => { mod.DeleteMod(() => { promptsPanel.Children.Remove(button); }); })
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
    }

    private static string[] GetAvailableMods()
    {
        return Directory.GetDirectories("Mods");
    }

    private static void LoadExistingModDone(List<Control> answers, Window promptWindow)
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
        if (WindowManager.SearchForModsInCodeEditor(filePath) != null)
        {
            new InfoWindow("Error Opening Mod", InfoWindowType.Error, $"Only one instance of a project can be open at a time\n{filePath}").Show();
            return;
        }
        
        Mod mod = null;
            
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
                string modName = modData.Name;
                string description = modData.Description;
                string developers = modData.Authors;
                string? gmmVersion = modData.GMMVersion;
                string version = modData.Version;

                mod = new Mod(modName, description, developers, gmmVersion, version);
                App.RecentProjects.AddRecentProject(new ModInfo()
                {
                    Name = modName,
                    Path = filePath,
                    LastOpened = DateTime.Now,
                });
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
    public static void OpenUpdateWindow()
    {
        var window = new UpdateWindow();
        window.Show();
    }

    public static void OpenDocumentation()
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
    public static void OpenAbout()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AboutUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                new InfoWindow("Couldn't Open About", InfoWindowType.Error, ex.Message, true, fontSize:16).Show();
            }
        });
    }
    public static void OpenIssues()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = IssuesUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                new InfoWindow("Couldn't Open Issues Link", InfoWindowType.Error, ex.Message, true, fontSize:16).Show();
            }
        });
    }

    public static void QuitCodeEditor()
    {
        new InfoWindow("Are you sure?", InfoWindowType.YesNo, "Are you sure you would like to close the application?", true,
            win =>
            {
                Window mainWindow = WindowManager.Windows.FirstOrDefault(w => w is MainWindow);
        
                if (mainWindow == null)
                {
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                }
        
                foreach (var window in WindowManager.Windows)
                {
                    if (window is not MainWindow)
                    {
                        if (window is CodeEditor codeEditor)
                        {
                            new InfoWindow("Save Project", InfoWindowType.YesNo,
                                $"Would you like to save your project ({codeEditor.Mod.Name})?", true,
                                w =>
                                {
                                    codeEditor.Mod.SaveFiles(codeEditor);
                                    w.Close();
                                    window.Close();
                                }, w =>
                                {
                                    w.Close();
                                    window.Close();
                                }).Show();
                        }
                        else
                        {
                            window.Close();
                        }
                    }
                }
        
                if (mainWindow != null)
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                        mainWindow.WindowState = WindowState.Normal;
                    
                    mainWindow.Activate();
                    mainWindow.Topmost = true;
                    mainWindow.Topmost = false;
                }
                win.Close();
                
            }).Show();
    }
    
    public static void QuitCompletely()
    {
        foreach (var window in WindowManager.Windows)
        {
            if (window is CodeEditor codeEditor)
            {
                new InfoWindow("Save Project", InfoWindowType.YesNo,
                    $"Would you like to save your project ({codeEditor.Mod.Name})?", true,
                    w =>
                    {
                        codeEditor.Mod.SaveFiles(codeEditor);
                        w.Close();
                        window.Close();
                    }, w =>
                    {
                        w.Close();
                        window.Close();
                    }).Show();
            }
            else
            {
                window.Close();
            }
        }
    }
}
