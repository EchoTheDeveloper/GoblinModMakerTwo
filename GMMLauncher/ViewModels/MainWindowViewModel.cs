using System;
using System.Collections;
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
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string DocumentationUrl = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ICommand OpenDocumentationCommand { get; }
        public ICommand QuitAppCommand { get; }
        public ICommand NewFileCommand { get; }
        public ICommand LoadModCommand { get; }
        public ICommand LoadModDialogCommand { get; }
        public ICommand SettingsCommand { get; }
        

        MainWindow mainWindow;
        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            OpenDocumentationCommand = new RelayCommand(OpenDocumentation);
            QuitAppCommand = new RelayCommand(QuitApp);
            NewFileCommand = new RelayCommand(NewFile);
            LoadModCommand = new RelayCommand(LoadExistingMod);
            LoadModDialogCommand = new RelayCommand(LoadMod);
            SettingsCommand = new RelayCommand(OpenSettings);
        }

        public void NewFile()
        {
            var window = new PromptWindow("New File",
                new List<(Type, string)>
                {
                    (typeof(TextBox), "Mod Name: "),
                    (typeof(TextBox), "Description: "),
                    (typeof(TextBox), "Developers (Separate by comma): ")
                }, 
                NewFileDone
            );
            window.Show();
        }

        private void NewFileDone(List<Control> answers, Window promptWindow)
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

        public void LoadExistingMod()
        {
            var availableMods = GetAvailableMods();

            if (availableMods != null && availableMods.Length > 0)
            {
                var window = new PromptWindow("Load Mod",
                    new List<(Type, string)>
                    {
                        (typeof(TextBox), "Mod Name:"),
                        (typeof(TextBlock), "OR"),
                        (typeof(StackPanel), "Select a Mod:"),
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
                    new List<(Type, string)>
                    {
                        (typeof(TextBlock), "No Mods Found!")
                    }
                );
                window.Show();
            }
        }

        private string[] GetAvailableMods()
        {
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Mods");
            return Directory.GetDirectories("Mods");
        }

        public void LoadExistingModDone(List<Control> answers, Window promptWindow)
        {
            string modName = (answers[0] as TextBox)?.Text ?? string.Empty;
            LoadModFromFile(modName.Replace(" ", ""));
            promptWindow.Close();
        }
        
        public async void LoadMod()
        {
            var selectedFolders = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select a Mod",
                AllowMultiple = false,
                SuggestedStartLocation = await mainWindow.StorageProvider.TryGetFolderFromPathAsync(
                    Path.Combine(Directory.GetCurrentDirectory(), "Mods"))
            });

            var selectedFolder = selectedFolders.FirstOrDefault();
            if (selectedFolder != null)
            {
                LoadModFromFile(filePath: selectedFolder.Path.ToString().Replace("file:///", ""));
            }
        }
        
        public void LoadModFromFile(string folderName = "", string filePath = "")
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


        public void OpenSettings()
        {
            var window = new SettingsWindow();
            window.Show();
        }
        
        private void OpenDocumentation()
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

        private void QuitApp()
        {
            Dispatcher.UIThread.Post(() => 
            {
                Environment.Exit(0);
            });
        }
    }
}
