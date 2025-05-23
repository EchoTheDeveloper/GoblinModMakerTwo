﻿using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GMMLauncher.Views;
using TextMateSharp.Grammars;

namespace GMMLauncher.ViewModels
{
    public partial class SettingsWindowViewModel : ViewModelBase
    {
        public ICommand CloseWindowCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand AutoFindDirectoryCommand { get; }
        public ICommand InstallBepInExCommand { get; }
        
        SettingsWindow settingsWindow;
        CodeEditor editor;
        public SettingsWindowViewModel(SettingsWindow settingsWindow, CodeEditor editor = null)
        {
            this.editor = editor;
            this.settingsWindow = settingsWindow;
            CloseWindowCommand = new RelayCommand(CloseWindow);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            AutoFindDirectoryCommand = new RelayCommand(AutoFindSteamDirectory);
            InstallBepInExCommand = new RelayCommand(InstallBepInEx);
        }

        private void CloseWindow()
        {
            settingsWindow.Close();
        }

        private void SaveSettings()
        {
            string steamDir = settingsWindow.FindControl<TextBox>("SteamDirectory").Text;
            ThemeName selectedTheme = (ThemeName)settingsWindow.FindControl<ComboBox>("SelectTheme").SelectedIndex;
            bool showLineNumbers = (bool)settingsWindow.FindControl<CheckBox>("ShowLineNumbers").IsChecked;
            bool showExplorer = (bool)settingsWindow.FindControl<CheckBox>("ShowExplorer").IsChecked;
            
            App.Settings.SteamDirectory = steamDir;
            App.Settings.SelectedTheme = selectedTheme;
            App.Settings.ShowLineNumbers = showLineNumbers;
            App.Settings.ShowExplorer = showExplorer;
            App.Settings.SaveSettings();
            
            if (editor != null)
            {
                editor.UpdateVisuals();
                editor.fileTree.IsVisible = showExplorer;
            }
            
            settingsWindow.Close();
        }

        private void AutoFindSteamDirectory()
        {
            string directory = App.Settings.FindSteamDirectory();
            if (directory == null)
            {
                string customPath = PromptForCustomSteamDirectory().Result;
                if (!string.IsNullOrEmpty(customPath) && Directory.Exists(Path.Combine(customPath, "steamapps", "common", "Isle Goblin Playtest")))
                {
                    directory = Path.Combine(customPath, "steamapps", "common", "Isle Goblin Playtest");
                }
            }
            settingsWindow.FindControl<TextBox>("SteamDirectory").Text = directory;
        }

        

        private async Task<string> PromptForCustomSteamDirectory()
        {
            var selectedFiles = await settingsWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select a Mod",
                AllowMultiple = false,
                SuggestedStartLocation = await settingsWindow.StorageProvider.TryGetFolderFromPathAsync(
                    Path.Combine(Directory.GetCurrentDirectory(), "Mods"))
            });

            var selectedFile = selectedFiles.FirstOrDefault();
            if (selectedFile != null)
            {
                return selectedFile.Path.ToString().Replace("file:///", "");
            }

            return null;
        }

        private void InstallBepInEx()
        {
            App.Settings.InstallBepInEx();
        }
    }
}
