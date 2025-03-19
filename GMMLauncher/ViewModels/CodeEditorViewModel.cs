using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using DynamicData;
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class CodeEditorViewModel : ViewModelBase
    {
        #region Commands
        public ICommand OpenDocumentationCommand => MenuCommands.OpenDocumentationCommand;
        public ICommand OpenSettingsCommand => new RelayCommand(OpenSettings);
        public ICommand QuitAppCommand => MenuCommands.QuitAppCommand;
        public ICommand NewModCommand => MenuCommands.NewModCommand;
        public ICommand LoadExistingModCommand => MenuCommands.LoadExistingModCommand;
        public ICommand LoadModDialogCommand => new RelayCommand(LoadModDialog);
        public ICommand NewFileCommand => new RelayCommand(NewFile);

        public ICommand SaveModCommand => new RelayCommand(SaveMod);
        public ICommand SaveFileCommand => new RelayCommand(SaveFile);
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
        public ICommand CreateConfigItemCommand => new RelayCommand(CreateConfigItem);
        public ICommand CreateKeybindCommand => new RelayCommand(CreateKeybind);
        
        public ICommand BuildModCommand => new RelayCommand(BuildMod);
        
        public ICommand CloseTabCommand => new RelayCommand(CloseTab);
        public ICommand CloseAllTabsCommand => new RelayCommand(CloseAllTabs);
        public ICommand CloseOtherTabsCommand => new RelayCommand(CloseOtherTabs);
        #endregion
        
        private readonly CodeEditor _editor;
        
        public CodeEditorViewModel(CodeEditor editor)
        {
            _editor = editor;
        }

        #region MenuBarFunctions
            private void NewFile()
            {
                var window = new PromptWindow("New File",
                    new List<(Type, string, object?, bool)>
                    {
                        (typeof(TextBox), "File Name:", "", true)
                    },
                    NewFileDone
                );

                window.Show();
            }
            private void NewFileDone(List<Control> answers, Window promptWindow)
            {
                string fileName = (answers[0] as TextBox).Text;
                string nameNoSpace = string.Concat(fileName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                nameNoSpace = char.ToUpper(nameNoSpace[0]) + nameNoSpace[1..];

                // Ensure filename has .cs extension
                if (Path.GetExtension(nameNoSpace) == "")
                {
                    nameNoSpace += ".cs";
                }
                
                string fileFolder = Path.Combine(_editor.Mod.GetFolderPath(), "Files");
                string filePath = Path.Combine(fileFolder, nameNoSpace);

                // Generate class name (capitalize each word, remove spaces)
                string className = string.Concat(Path.GetFileNameWithoutExtension(fileName)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));
                className = char.ToUpper(className[0]) + className[1..];
                 // File.Create(fileName);
                string newFileContent = $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {_editor.Mod.NameNoSpaces}
{{
    public class {className}
    {{
        // Write code here
    }}
}}";
                File.WriteAllText(filePath, newFileContent);
                // _editor.UpdateVisuals();
                _editor.UpdateFileTree(fileFolder);
                promptWindow.Close();
            }

            #region CreateFunctions
                private void CreateHarmonyPatch()
                {
                    var window = new PromptWindow("Create Harmony Patch",
                        new List<(Type, string, object?, bool)>
                        {
                            (typeof(TextBox), "Function Name:", "", true),
                            (typeof(TextBox), "Function's Class:", "", true),
                            (typeof(TextBox), "Parameters (Separate by comma):", "", false),
                            (typeof(ComboBox), "Patch Type (Prefix, Postfix):", new List<string> { "Prefix", "Postfix"}, false),
                            (typeof(TextBox), "Return Type:", "None", true),
                            (typeof(CheckBox), "Have Instance:", false, false),
                        },
                        CreateHarmonyPatchDone
                    );
    
                    window.Show();
                }
    
                private void CreateHarmonyPatchDone(List<Control> answers, Window promptWindow)
                {
                    string functionName = (answers[0] as TextBox)?.Text ?? "";
                    string functionClassName = (answers[1] as TextBox)?.Text ?? "";
                    List<string> parameters = ((answers[2] as TextBox)?.Text.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).ToList();
                    string patchType = (answers[3] as ComboBox)?.SelectedItem as string ?? "Prefix";
                    string returnType = (answers[4] as TextBox)?.Text ?? "None";
                    bool? hasInstance = (answers[5] as CheckBox)?.IsChecked;

                    if (string.IsNullOrWhiteSpace(functionName) || string.IsNullOrWhiteSpace(functionClassName))
                    {
                        Console.WriteLine("Error: Function name or class name is missing.");
                        return;
                    }

                    string signature = "private ";

                    if (returnType != "None")
                    {
                        parameters.Insert(0, $"ref {returnType} __result");
                    }

                    if (hasInstance == true)
                    {
                        parameters.Insert(0, $"ref {functionClassName} __instance");
                        signature += "static ";
                    }

                    signature += (patchType == "Prefix") ? "bool " : "void ";
                    string joinedParameters = string.Join(", ", parameters);
                    signature += $"{functionName.Replace(" ", "")}{patchType}Patch({joinedParameters})";

                    string harmonyPatch = $"[HarmonyPatch(typeof({functionClassName}), \"{functionName}\")]";

                    string patchCode = $@"
        {harmonyPatch}
        [{patchType}Patch]
        {signature}
        {{
            if (mEnabled.Value)
            {{
                // Write code for patch here";

                    if (returnType != "None")
                    {
                        patchCode += "\n                // __result = null;";
                    }

                    if (patchType == "Prefix")
                    {
                        patchCode += "\n                return false; // Cancels Original Function of Method";
                    }

                    patchCode += "\n            }";

                    if (patchType == "Prefix")
                    {
                        patchCode += "\n            return true;";
                    }
                    patchCode += "\n        }";

                    string code = _editor.GetCurrentTextEditor().Text;
                    int lastNamespaceBracket = code.LastIndexOf('}');
                    int lastClassBracket = code.LastIndexOf('}', lastNamespaceBracket - 2);
                    if (lastClassBracket != -1 && lastNamespaceBracket != -1)
                    {
                        _editor.GetCurrentTextEditor().Text = code.Insert(lastClassBracket, patchCode + "\n    ");
                    }
                    else
                    {
                        new InfoWindow("Error", InfoWindowType.Error, "Couldn't find class end bracket.", true, fontSize:20).Show();

                    }
                    promptWindow.Close();
                }
                

                private void CreateConfigItem()
                {
                    var window = new PromptWindow("Create Config Item",
                        new List<(Type, string, object?, bool)>
                        {
                            (typeof(TextBox), "Variable Name:", "", true),
                            (typeof(ComboBox), "Data Type:", new List<string> { "int", "string" }, false),
                            (typeof(TextBox), "Default Value:", "", true),
                            (typeof(TextBox), "Definition (name in mod's configuration):", "", true),
                            (typeof(TextBox), "Description (info on hovered)", "", true)
                        },
                        CreateConfigItemDone
                    );
    
                    window.Show();
                }
    
                private void CreateConfigItemDone(List<Control> answers, Window promptWindow)
                {
                    string variableName = (answers[0] as TextBox)?.Text ?? "";
                    string dateType = (answers[1] as TextBox)?.Text ?? "";
                    string defaultValue = (answers[2] as TextBox)?.Text ?? "";
                    string definition = (answers[3] as TextBox)?.Text ?? "";
                    string description = (answers[4] as TextBox)?.Text ?? "";

                    string configEntry = $"        public static ConfigEntry<{dateType}> {variableName};";
                    string configDefinition = $"        public ConfigDefinition {variableName}Def = new ConfigDefinition(pluginVersion, \"{definition}\");";
                    string constructorContent = $"            {variableName} = Config.Bind({variableName}, {defaultValue},new ConfigDescription(\n" +
                                                $"                \"{description}\", null, \n" +
                                                $"                new ConfigurationManagerAttributes {{ Order = 0 }}\n" +
                                                $"            ));";
                    
                    string code = _editor.GetCurrentTextEditor().Text;
                    
                    int configEntryIndex = code.IndexOf("ConfigEntry<");
                    if (configEntryIndex != -1)
                    {
                        int insertIndex = code.IndexOf('\n', configEntryIndex) + 1;
                        code = code.Insert(insertIndex, configEntry + "\n");
                    }
                    else
                    {
                        int lastConst = code.IndexOf("const ");
                        if (lastConst != -1)
                            code = code.Insert(lastConst, configEntry + "\n");
                    }

                    int configDefinitionIndex = code.LastIndexOf("public ConfigDefinition");
                    if (configDefinitionIndex != -1)
                    {
                        int insertIndex = code.IndexOf('\n', configDefinitionIndex) + 1;
                        code = code.Insert(insertIndex, configDefinition + "\n");
                    }
                    else
                    {
                        int lastConfigEntry = code.LastIndexOf("ConfigEntry<");
                        if (lastConfigEntry != -1)
                        {
                            int insertIndex = code.IndexOf('\n', lastConfigEntry) + 2;
                            code = code.Insert(insertIndex, configDefinition + "\n");
                        }
                    }

                    int constructorIndex = code.IndexOf($"public {_editor.Mod.NameNoSpaces}");
                    if (constructorIndex != -1)
                    {
                        int bracketIndex = code.IndexOf('{', constructorIndex);
                        if (bracketIndex != -1)
                        {
                            code = code.Insert(bracketIndex + 1, "\n" + constructorContent);
                        }
                    }
                    
                    _editor.GetCurrentTextEditor().Text = code;
                    
                    promptWindow.Close();
                }
                
                private void CreateKeybind()
                {
                    var window = new PromptWindow("Create Keybind",
                        new List<(Type, string, object?, bool)>
                        {
                            (typeof(TextBox), "Variable Name:", "", true),
                            (typeof(Button), "Keycode:", () =>
                            {
                                var url = "https://docs.unity3d.com/ScriptReference/KeyCode.html";
                                Process.Start(new ProcessStartInfo 
                                { 
                                    FileName = url, 
                                    UseShellExecute = true 
                                });
                            }, false),
                            (typeof(TextBox), "", "", true),
                            (typeof(TextBox), "Definition (name in mod's configuration):", "", true),
                            (typeof(TextBox), "Description (info on hovered)", "", true)
                        },
                        CreateKeybindDone
                    );
    
                    window.Show();
                }
    
                private void CreateKeybindDone(List<Control> answers, Window promptWindow)
                {
                    string variableName = (answers[0] as TextBox)?.Text ?? "";
                    string keycode = (answers[2] as TextBox)?.Text ?? "";
                    
                    CreateConfigItemDone([
                        answers[0],
                        new TextBox { Text = "BepInEx.Configuration.KeyboardShortcut" },
                        new TextBox
                        {
                            Text = $"new BepInEx.Configuration.KeyboardShortcut(UnityEngine.KeyCode.{keycode})"
                        },
                        answers[3],
                        answers[4]
                    ], promptWindow);
                    
                    string code = _editor.GetCurrentTextEditor().Text;
                    
                    string logic = $@"
            // Keybind logic for {variableName}
            {variableName}JustPressed = {variableName}.Value.IsDown();
            if ({variableName}.Value.IsDown())
            {{
                {variableName}Down = true;
                if (mEnabled.Value)
                {{
                    // Code For When Key Is Pressed
                }}
            }}
            if ({variableName}.Value.IsUp())
            {{
                {variableName}Down = false;
                if (mEnabled.Value)
                {{
                    // Code For When Key Is Released
                }}
            }}";
                    
                    int updateIndex = code.IndexOf("void Update()");
                    if (updateIndex != -1)
                    {
                        int bracketIndex = code.IndexOf('{', updateIndex);
                        if (bracketIndex != -1)
                        {
                            code = code.Insert(bracketIndex + 1, "\n" + logic);
                        }
                    }
                    void DeclareVariable(string nameModifier = "Down")
                    {
                        string declaration = $"        public static bool {variableName}{nameModifier} = false;";
                        int lastBoolIndex = code.IndexOf("public static bool ");
                        if (lastBoolIndex != -1)
                        {
                            int insertIndex = code.IndexOf('\n', lastBoolIndex) + 1;
                            code = code.Insert(insertIndex, declaration + "\n");
                        }
                        else
                        {
                            int lastConfigEntryIndex = code.LastIndexOf("ConfigDefinition");
                            if (lastConfigEntryIndex != -1)
                            {
                                int insertIndex = code.IndexOf('\n', lastConfigEntryIndex) + 2;
                                code = code.Insert(insertIndex, declaration + "\n");
                            }
                        }
                    }
                    DeclareVariable(); 
                    DeclareVariable("JustPressed");
                    _editor.GetCurrentTextEditor().Text = code;
                    
                    promptWindow.Close();
                }
            #endregion

            private void BuildMod()
            {
                var infoWindow = new InfoWindow("Building Mod", InfoWindowType.Info, "Running Dotnet Build...");
                infoWindow.Show();
                _editor.Mod.InstallMod(infoWindow, _editor);
            }
            
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
                mod.SaveFiles(_editor);
            }

            private void SaveFile()
            {
                var tab = _editor._tabControl.SelectedItem as TabItem;
                string filePath = Path.Combine(_editor.Mod.GetFolderPath(), "Files", tab.Header.ToString());
                TextEditor textEditor = (tab.Content as TextCodeEditor).Content as TextEditor;
                string code = textEditor.Text;
                File.WriteAllText(filePath, code);
            }
        
        #endregion
        #region MenuCommands

            private void Undo()
            {
                ApplicationCommands.Undo.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }

            private void Redo()
            {
                ApplicationCommands.Redo.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }
            private void CopyMouse()
            {
                ApplicationCommands.Copy.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }

            private void CutMouse()
            {
                ApplicationCommands.Cut.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }
        
            private void PasteMouse()
            {
                ApplicationCommands.Paste.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }

            private void SelectAllMouse()
            {
                ApplicationCommands.SelectAll.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }

            private void Find()
            {
                ApplicationCommands.Find.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }

            private void Replace()
            {
                ApplicationCommands.Replace.Execute(null, _editor.GetCurrentTextEditor().TextArea);
            }

            private void GoToLine()
            {
                var window = new PromptWindow("Go to Line:Column",
                    new List<(Type, string, object?, bool)>
                    {
                        (typeof(TextBox), "Line:Column", "", true)
                    },
                    GoToLineDone, 
                    baseHeight:220);
            
                var promptsPanel = window.FindControl<StackPanel>("PromptsPanel")!;
                
                var lineControl = new TextBox()
                {
                    Text = _editor.GetCurrentTextEditor().TextArea.Caret.Line + ":" + _editor.GetCurrentTextEditor().TextArea.Caret.Column,
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
                    _editor.GetCurrentTextEditor().TextArea.Caret.Line = 1;
                    _editor.GetCurrentTextEditor().TextArea.Caret.Column = 1;
                }
                else
                {
                    var parts = input.Split(':');
            
                    if (parts.Length == 1)
                    {
                        if (int.TryParse(parts[0], out int line))
                        {
                            _editor.GetCurrentTextEditor().TextArea.Caret.Line = line;
                            _editor.GetCurrentTextEditor().TextArea.Caret.Column = 1;
                        }
                        else
                        {
                            _editor.GetCurrentTextEditor().TextArea.Caret.Line = 1;
                            _editor.GetCurrentTextEditor().TextArea.Caret.Column = 1;
                        }
                    }
                    else if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int line) && int.TryParse(parts[1], out int column))
                        {
                            _editor.GetCurrentTextEditor().TextArea.Caret.Line = line;
                            _editor.GetCurrentTextEditor().TextArea.Caret.Column = column;
                        }
                        else
                        {
                            _editor.GetCurrentTextEditor().TextArea.Caret.Line = 1;
                            _editor.GetCurrentTextEditor().TextArea.Caret.Column = 1;
                        }
                    }
                    else
                    {
                        _editor.GetCurrentTextEditor().TextArea.Caret.Line = 1;
                        _editor.GetCurrentTextEditor().TextArea.Caret.Column = 1;
                    }
                }
            
                promptWindow.Close();
            }

            #region Tab Commands

            private void CloseTab()
            {
                if (_editor.rightClickedTab != null)
                {
                    _editor.CloseTab(_editor.rightClickedTab);
                    _editor.rightClickedTab = null;
                }
                else
                {
                    _editor.CloseTab(_editor._tabControl.SelectedItem as TabItem);
                }
            }

            private void CloseAllTabs()
            {
                _editor._tabs.Clear();
                _editor.rightClickedTab = null;
            }

            private void CloseOtherTabs()
            {
                _editor._tabs = new ObservableCollection<TabItem> { _editor.rightClickedTab };
                _editor.ResetTabControl();
            }
            

            #endregion

        #endregion
    }
}