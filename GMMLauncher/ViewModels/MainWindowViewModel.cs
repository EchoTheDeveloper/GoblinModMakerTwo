using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<ModInfo> RecentProjects => App.RecentProjects.RecentProjectList;

        public string Version => "v" + App.appVersion;

        MainWindow mainWindow;
        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            mainWindow.GettingFocus += (_, _) => ReloadVisuals();
            
            // App.RecentProjects.VerifyProjects();
        }

        public void NewMod() => MenuCommands.NewMod();
        public void LoadModDialog() => MenuCommands.LoadMod(mainWindow);
        public void LoadExistingMod() => MenuCommands.LoadExistingMod();
        public void OpenSettings() => MenuCommands.OpenSettings();
        public void OpenUpdateWindow() => MenuCommands.OpenUpdateWindow();
        public void ReloadVisuals() => App.CleanThemeApply();
        public void QuitApp() => MenuCommands.QuitCompletely();
        public void OpenDocumentation() => MenuCommands.OpenDocumentation();
        public void OpenIssues() => MenuCommands.OpenIssues();
        
        
        public void ConfigureMod()
        {
            mainWindow.ConfigureMod();
            
            mainWindow._rightClickedMod = null;
        }
        
        public void OpenModInExplorer()
        {
            mainWindow.OpenModInExplorer();
            
            mainWindow._rightClickedMod = null;
        }
        
        public void DeleteMod()
        {
            mainWindow.DeleteMod();
            
            mainWindow._rightClickedMod = null;
        }
    }
}

