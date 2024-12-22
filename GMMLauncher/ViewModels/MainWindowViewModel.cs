using System;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using Avalonia;
using Avalonia.Threading;

namespace GMMLauncher.ViewModels
{
    // TODO: This keeps crashing giving an async error but its being called from the correct thread

    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ReactiveCommand<Unit, Unit> OpenDocumentationCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitAppCmd { get; }

        public MainWindowViewModel()
        {
            OpenDocumentationCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(OpenDocumentation);
            }, outputScheduler: RxApp.MainThreadScheduler);

            QuitAppCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(QuitApp);
            }, outputScheduler: RxApp.MainThreadScheduler);
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
