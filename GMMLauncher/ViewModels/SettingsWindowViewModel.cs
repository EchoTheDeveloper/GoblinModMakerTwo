using System.IO;
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
        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);
        public ICommand AutoFindDirectoryCommand => new RelayCommand(AutoFindSteamDirectory);
        public ICommand InstallBepInExCommand => new RelayCommand(InstallBepInEx);
        
        SettingsWindow settingsWindow;
        CodeEditor editor;
        public SettingsWindowViewModel(SettingsWindow settingsWindow, CodeEditor editor = null)
        {
            this.editor = editor;
            this.settingsWindow = settingsWindow;
        }

        private void CloseWindow()
        {
            settingsWindow.Close();
        }

        private void SaveSettings()
        {
            string steamDir = settingsWindow.FindControl<TextBox>("SteamDirectory").Text;
            string selectedTheme = settingsWindow.FindControl<ComboBox>("SelectTheme").SelectedValue.ToString();
            bool showLineNumbers = (bool)settingsWindow.FindControl<CheckBox>("ShowLineNumbers").IsChecked;
            bool showExplorer = (bool)settingsWindow.FindControl<CheckBox>("ShowExplorer").IsChecked;
            bool overwriteCsproj = (bool)settingsWindow.FindControl<CheckBox>("OverwriteCsproj").IsChecked;
            bool zipMod = (bool)settingsWindow.FindControl<CheckBox>("ZipMod").IsChecked;
            bool openPluginFolder = (bool)settingsWindow.FindControl<CheckBox>("OpenPluginFolder").IsChecked;
            string opacityAmount = (string)settingsWindow.FindControl<NumericUpDown>("OpacityAmount").Text;
            
            App.Settings.SteamDirectory = steamDir;
            App.Settings.SelectedTheme = selectedTheme;
            App.Settings.ShowLineNumbers = showLineNumbers;
            App.Settings.ShowExplorer = showExplorer;
            App.Settings.OverwriteCsproj = overwriteCsproj;
            App.Settings.ZipMod =  zipMod;
            App.Settings.OpenPluginFolder = openPluginFolder;
            App.Settings.OpacityAmount = opacityAmount;
            App.Settings.SaveSettings();
            
            foreach (var window in WindowManager.Windows)
            {
                if (window is CodeEditor codeEditor)
                {
                    codeEditor.UpdateVisuals();
                    if (codeEditor.fileTree != null) codeEditor.fileTree.IsVisible = showExplorer;
                }
            }
            
            if (editor == null)
            {
                App.CleanThemeApply();
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
