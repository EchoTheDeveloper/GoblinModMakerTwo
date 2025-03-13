using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Controls;
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
            var window = new PromptWindow("New File", ["Mod Name: ", "Description", "Developers (Separate by comma)"], NewFileDone, NewFileCancel);
            window.Show();
        }

        private void NewFileDone(List<TextBox> answers, Window promptWindow)
        {
            Mod mod = new(answers[0].Text, answers[1].Text, answers[2].Text,
                Assembly.GetExecutingAssembly().GetName().Version.ToString());
            mod.CreateMainFile();
            var editor = new CodeEditor(mod);
            editor.Show();
            promptWindow.Close();
        }

        private void NewFileCancel(Window window)
        {
            window.Close();
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
