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
        public ICommand OpenDocumentationCommand => MenuCommands.OpenDocumentationCommand;
        public ICommand OpenSettingsCommand => new RelayCommand(OpenSettings);
        public ICommand QuitAppCommand => MenuCommands.QuitAppCommand;
        public ICommand NewModCommand => MenuCommands.NewModCommand;
        public ICommand LoadExistingModCommand => MenuCommands.LoadExistingModCommand;
        public ICommand LoadModDialogCommand => new RelayCommand(LoadModDialog);
        

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
