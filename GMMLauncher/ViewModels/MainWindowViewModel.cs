using System;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using System.Threading.Tasks;
using Avalonia;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";
        
        public ReactiveCommand<Unit, Unit> OpenDocumentationCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitAppCmd { get; }

        public MainWindowViewModel()
        {
            // Initialize the command with an action
            OpenDocumentationCommand = ReactiveCommand.CreateFromTask(OpenDocumentationAsync);
            QuitAppCmd = ReactiveCommand.CreateFromTask(QuitAsync);
        }
        
        private async Task OpenDocumentationAsync()
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo(DocumentationURL) { UseShellExecute = true });
            });
        }

        private async Task QuitAsync()
        {
            await Task.Run(() =>
            {
                Environment.Exit(0);
            });
        }
    }
}