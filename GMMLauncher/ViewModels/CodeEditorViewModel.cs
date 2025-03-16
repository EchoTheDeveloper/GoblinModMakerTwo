using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class CodeEditorViewModel : ViewModelBase
    {
        public ICommand OpenDocumentationCommand => MenuCommands.OpenDocumentationCommand;
        public ICommand OpenSettingsCommand => new RelayCommand(OpenSettings);
        public ICommand QuitAppCommand => MenuCommands.QuitAppCommand;
        public ICommand NewModCommand => MenuCommands.NewModCommand;
        public ICommand LoadExistingModCommand => MenuCommands.LoadExistingModCommand;
        public ICommand LoadModDialogCommand => new RelayCommand(LoadModDialog);

        public ICommand SaveModCommand => new RelayCommand(SaveMod);
        public ICommand UndoCommand => new RelayCommand(Undo);
        public ICommand RedoCommand => new RelayCommand(Redo);

        public ICommand CopyCommand => new RelayCommand(CopyMouse);
        public ICommand CutCommand => new RelayCommand(CutMouse);
        public ICommand PasteCommand => new RelayCommand(PasteMouse);
        public ICommand SelectAllCommand => new RelayCommand(SelectAllMouse);
            
        public ICommand FindCommand => new RelayCommand(Find);
        public ICommand ReplaceCommand => new RelayCommand(Replace);
        public ICommand GoToLineCommand => new RelayCommand(GoToLine);

        public ICommand CreateHarmonyPatchCommand => new RelayCommand(CreateHarmonyPatch);
        
        
        private readonly CodeEditor _editor;
        
        public CodeEditorViewModel(CodeEditor editor)
        {
            _editor = editor;
        }

        #region MenuBarFunctions
            private void NewFile()
            {
                var window = new PromptWindow("New File",
                    new List<(Type, string, object?)>
                    {
                        (typeof(TextBox), "File Name:", "")
                    },
                    NewFileDone
                );

                window.Show();
            }
            private void NewFileDone(List<Control> answers, Window promptWindow)
            {
                string fileName = (answers[0] as TextBlock).Text;
                promptWindow.Close();
            }

            #region CreateFunctions
                private void CreateHarmonyPatch()
                {
                    var window = new PromptWindow("Create Harmony Patch",
                        new List<(Type, string, object?)>
                        {
                            (typeof(TextBox), "Function Name:", ""),
                            (typeof(TextBox), "Function's Class:", ""),
                            (typeof(TextBox), "Parameters (Separate by comma):", ""),
                            (typeof(ComboBox), "Patch Type (Prefix, Postfix):", new List<string> { "Prefix", "Postfix"}),
                            (typeof(TextBox), "Return Type:", "None"),
                            (typeof(CheckBox), "Have Instance:", false),
                        },
                        CreateHarmonyPatchDone
                    );
    
                    window.Show();
                }
    
                private void CreateHarmonyPatchDone(List<Control> answers, Window promptWindow)
                {
                    
                    promptWindow.Close();
                }

                private void CreateConfigItem()
                {
                    var window = new PromptWindow("Create Config Item",
                        new List<(Type, string, object?)>
                        {
                            (typeof(TextBox), "Variable Name:", ""),
                            (typeof(TextBox), "Data Type (e.g. int):", ""),
                            (typeof(TextBox), "Default Value:", ""),
                            (typeof(TextBox), "Definition (name in mod's configuration):", ""),
                            (typeof(TextBox), "Description (info on hovered)", "")
                        },
                        CreateConfigItemDone
                    );
    
                    window.Show();
                }
    
                private void CreateConfigItemDone(List<Control> answers, Window promptWindow)
                {
                    
                    promptWindow.Close();
                }
                
                private void CreateKeybind()
                {
                    var window = new PromptWindow("Create Keybind",
                        new List<(Type, string, object?)>
                        {
                            (typeof(TextBox), "Variable Name:", ""),
                            (typeof(TextBox), "Keycode:", ""),
                            (typeof(Button), "Open Keycode List", () =>
                            {
                                var url = "https://docs.unity3d.com/ScriptReference/KeyCode.html";
                                Process.Start(new ProcessStartInfo 
                                { 
                                    FileName = url, 
                                    UseShellExecute = true 
                                });
                            }),
                            (typeof(TextBox), "Definition (name in mod's configuration):", ""),
                            (typeof(TextBox), "Description (info on hovered)", "")
                        },
                        CreateKeybindDone
                    );
    
                    window.Show();
                }
    
                private void CreateKeybindDone(List<Control> answers, Window promptWindow)
                {
                    
                    promptWindow.Close();
                }
            #endregion
            
            private void LoadModDialog()
            {
                MenuCommands.LoadModDialogCommand.Execute(_editor);
            }

            private void OpenSettings()
            {
                MenuCommands.OpenSettingsInEditorCommand.Execute(_editor);
            }
            
            private void SaveMod()
            {
                Mod mod = _editor.Mod;
                mod.SaveFiles(_editor._editor.Text);
            }
        
        #endregion
        #region MenuCommands

            private void Undo()
            {
                ApplicationCommands.Undo.Execute(null, _editor._editor.TextArea);
            }

            private void Redo()
            {
                ApplicationCommands.Redo.Execute(null, _editor._editor.TextArea);
            }
            private void CopyMouse()
            {
                ApplicationCommands.Copy.Execute(null, _editor._editor.TextArea);
            }

            private void CutMouse()
            {
                ApplicationCommands.Cut.Execute(null, _editor._editor.TextArea);
            }
        
            private void PasteMouse()
            {
                ApplicationCommands.Paste.Execute(null, _editor._editor.TextArea);
            }

            private void SelectAllMouse()
            {
                ApplicationCommands.SelectAll.Execute(null, _editor._editor.TextArea);
            }

            private void Find()
            {
                ApplicationCommands.Find.Execute(null, _editor._editor.TextArea);
            }

            private void Replace()
            {
                ApplicationCommands.Replace.Execute(null, _editor._editor.TextArea);
            }

            private void GoToLine()
            {
                var window = new PromptWindow("Go to Line:Column",
                    new List<(Type, string, object?)>
                    {
                        (typeof(TextBlock), "Line:Column", "")
                    },
                    GoToLineDone, 
                    baseHeight:220);
            
                var promptsPanel = window.FindControl<StackPanel>("PromptsPanel")!;
                
                var lineControl = new TextBox()
                {
                    Text = _editor._editor.TextArea.Caret.Line + ":" + _editor._editor.TextArea.Caret.Column,
                };
            
                promptsPanel.Children.Add(lineControl);
                window.answers.Add(lineControl);
                window.Show();
            }
            
            private void GoToLineDone(List<Control> answers, Window promptWindow)
            {
                var input = (answers[0] as TextBox)?.Text;
            
                if (string.IsNullOrWhiteSpace(input))
                {
                    _editor._editor.TextArea.Caret.Line = 1;
                    _editor._editor.TextArea.Caret.Column = 1;
                }
                else
                {
                    var parts = input.Split(':');
            
                    if (parts.Length == 1)
                    {
                        if (int.TryParse(parts[0], out int line))
                        {
                            _editor._editor.TextArea.Caret.Line = line;
                            _editor._editor.TextArea.Caret.Column = 1;
                        }
                        else
                        {
                            _editor._editor.TextArea.Caret.Line = 1;
                            _editor._editor.TextArea.Caret.Column = 1;
                        }
                    }
                    else if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int line) && int.TryParse(parts[1], out int column))
                        {
                            _editor._editor.TextArea.Caret.Line = line;
                            _editor._editor.TextArea.Caret.Column = column;
                        }
                        else
                        {
                            _editor._editor.TextArea.Caret.Line = 1;
                            _editor._editor.TextArea.Caret.Column = 1;
                        }
                    }
                    else
                    {
                        _editor._editor.TextArea.Caret.Line = 1;
                        _editor._editor.TextArea.Caret.Column = 1;
                    }
                }
            
                promptWindow.Close();
            }


        #endregion
    }
}
