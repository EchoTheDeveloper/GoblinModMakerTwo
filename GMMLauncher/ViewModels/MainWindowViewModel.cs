using System.Windows.Input;
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ICommand OpenDocumentationCommand => MenuCommands.OpenDocumentationCommand;
        public ICommand OpenSettingsCommand => new RelayCommand(OpenSettings);
        public ICommand QuitAppCommand => MenuCommands.QuitAppCommand;
        public ICommand NewModCommand => MenuCommands.NewModCommand;
        public ICommand LoadExistingModCommand => MenuCommands.LoadExistingModCommand;
        public ICommand LoadModDialogCommand => new RelayCommand(LoadModDialog);

        public string version => "Goblin Mod Maker v"+App.appVersion;

        MainWindow mainWindow;
        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void LoadModDialog()
        {
            MenuCommands.LoadModDialogCommand.Execute(mainWindow);
        }
        private void OpenSettings()
        {
            MenuCommands.OpenSettingsCommand.Execute(null);
        }
    }
}
