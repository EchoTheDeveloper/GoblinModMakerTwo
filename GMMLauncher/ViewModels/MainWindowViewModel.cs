using System;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using Avalonia.Threading;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ReactiveCommand<Unit, Unit> OpenDocumentationCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitAppCommand { get; }

        public MainWindowViewModel()
        {
            OpenDocumentationCommand = ReactiveCommand.Create(() =>
            {
                Dispatcher.UIThread.InvokeAsync(OpenDocumentation, DispatcherPriority.Normal);
                return Unit.Default;
            });

            QuitAppCommand = ReactiveCommand.Create(() =>
            {
                Dispatcher.UIThread.InvokeAsync(QuitApp, DispatcherPriority.Normal);
                return Unit.Default;
            });
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
