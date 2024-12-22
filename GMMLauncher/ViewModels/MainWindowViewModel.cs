using System;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using Avalonia;
using Avalonia.Threading;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";
        
        public ReactiveCommand<Unit, Unit> OpenDocumentationCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitAppCmd { get; }

        public MainWindowViewModel()
        {
            OpenDocumentationCommand = ReactiveCommand.Create(OpenDocumentation);
            QuitAppCmd = ReactiveCommand.Create(QuitApp);
        }

        private void OpenDocumentation()
        {
   
            // Making sure it runs on the ui thread but at this point idk what the fuck the ui thread is and my mental brain is draining
            Dispatcher.UIThread.Invoke(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocumentationURL,
                    UseShellExecute = true
                });
            });
        }

        private void QuitApp()
        {
            // TODO: There should be an avalonia shutdown but I could not find it I think they changed it so for now i exit the whole env but I think we should use the Avalonia shutdown aswell
            Environment.Exit(0);
        }
    }
}