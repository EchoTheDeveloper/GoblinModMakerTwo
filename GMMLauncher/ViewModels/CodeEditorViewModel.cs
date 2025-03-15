using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
        public ICommand OpenSettingsCommand { get; }
        public ICommand QuitAppCommand { get; }
        public ICommand NewFileCommand { get; }
        public ICommand SaveModCommand { get; }
        private CodeEditor _editor;
        public CodeEditorViewModel(CodeEditor editor)
        {
            _editor = editor;
            OpenDocumentationCommand = new RelayCommand(OpenDocumentation);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            QuitAppCommand = new RelayCommand(QuitApp);
            NewFileCommand = new RelayCommand(NewFile);
            SaveModCommand = new RelayCommand(SaveMod);
        }
        

        #region MenuBarFunctions
            private void NewFile()
            {
                // var window = new CodeEditor();
                // window.Show();
            }

            private void SaveMod()
            {
                Mod mod = _editor.Mod;
                mod.SaveFiles(_editor._editor.Text);
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

            private void OpenSettings()
            {
                var window = new SettingsWindow(_editor);
                window.Show();
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
