using System;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class CodeEditorViewModel : ViewModelBase
    {
        private const string DocumentationUrl = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ICommand OpenDocumentationCommand { get; }
        public ICommand QuitAppCommand { get; }
        public ICommand NewFileCommand { get; }
        
        public CodeEditorViewModel()
        {
            OpenDocumentationCommand = new RelayCommand(OpenDocumentation);
            QuitAppCommand = new RelayCommand(QuitApp);
            NewFileCommand = new RelayCommand(NewFile);
        }

        #region MenuBarFunctions
            private void NewFile()
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
        #endregion

        #region ContextMenuFunctions
            public void CopyMouseCommand(TextArea textArea)
            {
                ApplicationCommands.Copy.Execute(null, textArea);
            }

            public void CutMouseCommand(TextArea textArea)
            {
                ApplicationCommands.Cut.Execute(null, textArea);
            }
        
            public void PasteMouseCommand(TextArea textArea)
            {
                ApplicationCommands.Paste.Execute(null, textArea);
            }

            public void SelectAllMouseCommand(TextArea textArea)
            {
                ApplicationCommands.SelectAll.Execute(null, textArea);
            }
        #endregion
        
    }
}
