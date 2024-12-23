using System;
using System.Diagnostics;
using ReactiveUI;
using Avalonia.Threading;
using System.Reactive;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ReactiveCommand<Unit, Unit> OpenDocumentationCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitAppCommand { get; }

        public MainWindowViewModel()
        {
            OpenDocumentationCommand = ReactiveCommand.CreateFromTask(async () => await Dispatcher.UIThread.InvokeAsync(OpenDocumentation));
            QuitAppCommand = ReactiveCommand.CreateFromTask(async () => await Dispatcher.UIThread.InvokeAsync(QuitApp));
        }

        private void OpenDocumentation()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DocumentationURL,
                UseShellExecute = true
            });
        }

        private void QuitApp()
        {
            Environment.Exit(0);
        }
    }
}
