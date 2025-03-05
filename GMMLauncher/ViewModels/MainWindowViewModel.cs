using System;
using System.Diagnostics;
using System.Windows.Input;
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

        public MainWindowViewModel()
        {
            OpenDocumentationCommand = new RelayCommand(OpenDocumentation);
            QuitAppCommand = new RelayCommand(QuitApp);
            NewFileCommand = new RelayCommand(NewFile);
        }

        public void NewFile()
        {
            var window = new CodeEditor();
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
