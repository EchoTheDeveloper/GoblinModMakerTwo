using System;
using System.Diagnostics;
using ReactiveUI;
using Avalonia.Threading;
using System.Reactive;
using System.Threading.Tasks;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ReactiveCommand<Unit, Unit> OpenDocumentationCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitAppCommand { get; }

        public MainWindowViewModel()
        {// im acc so confused rn
            OpenDocumentationCommand = ReactiveCommand.CreateFromTask(async () => 
                Dispatcher.UIThread.Post(OpenDocumentation));

            QuitAppCommand = ReactiveCommand.CreateFromTask(async () => 
                Dispatcher.UIThread.Post(QuitApp));
        }

        // The error is somewhere here: I think its due to the async file running on the background thread
        
        private async void OpenDocumentation()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocumentationURL,
                    UseShellExecute = true
                });
            });
        }

        private async void QuitApp()
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                Environment.Exit(0);
            });
        }
    }
}
