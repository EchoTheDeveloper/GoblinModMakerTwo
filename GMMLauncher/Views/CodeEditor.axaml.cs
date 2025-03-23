using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation.CSharp;
using GMMLauncher.ViewModels;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvaloniaEdit.Folding;

namespace GMMLauncher.Views;

public partial class CodeEditor : Window
{
    private TextMate.Installation _textMateInstallation;
    private CompletionWindow _completionWindow;
    private OverloadInsightWindow _insightWindow;
    private RegistryOptions _registryOptions;
    private int _currentTheme = (int)App.Settings.SelectedTheme;
    private TextBlock _statusTextBlock;
    private CustomMargin _margin;
    
    public TabControl _tabControl { get; set; }
    public ObservableCollection<TabItem> _tabs = new();
    public TabItem rightClickedTab { get; set; }
    
    public TreeViewItem rightClickedFile { get; set; }
    public TreeView fileTree  { get; set; }
    
    public Mod Mod;
    
    public CodeEditor(Mod mod)
    {
        DataContext = new CodeEditorViewModel(this);
        this.Mod = mod;
        InitializeComponent();
        
        _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
        _statusTextBlock = this.Find<TextBlock>("StatusText");
        _tabControl = this.FindControl<TabControl>("TabControl");
        
        fileTree = this.FindControl<TreeView>("FileTree");
        
        _tabControl.ItemsSource = _tabs;
        _tabControl.PointerPressed += TabControl_PointerPressed;
        
        string filePath = mod.GetFilePath();
        if (!File.Exists(filePath))
        {
            mod.CreateMainFile();
        }

        SetupFileTree(mod.GetFileFolderPath());
        
        AddNewTab(Path.Combine(mod.NameNoSpaces + ".cs"));
    }
    
    
    #region Tabs
    private void TabControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(_tabControl);

        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            var clickedTab = e.Source as Control;
            while (clickedTab != null && clickedTab is not TabItem)
            {
                clickedTab = (Control)clickedTab.Parent;
            }

            if (clickedTab is TabItem tabItem)
            {
                CloseTab(tabItem);
                e.Handled = true;
            }
        }
        else if (pointerPoint.Properties.IsRightButtonPressed) 
        {
            var clickedTab = e.Source as Control;
            while (clickedTab != null && clickedTab is not TabItem)
            {
                clickedTab = (Control)clickedTab.Parent;
            }
        
            if (clickedTab is TabItem tabItem && clickedTab is not TextEditor)
            {
                rightClickedTab = tabItem;
                e.Handled = true;
            }
        }
    }
    public void CloseTab(TabItem tab)
    {
        TextEditor textEditor = (tab.Content as TextCodeEditor).Content as TextEditor;
        if (textEditor.IsModified)
        {
            new InfoWindow("File Not Saved", InfoWindowType.YesNo, $"{tab.Header.ToString()} is not saved, would you like to save now?", true,
                () =>
                {
                    Mod.SaveFile(tab);
                    _tabs.Remove(tab);
                },
                () =>
                {
                    _tabs.Remove(tab);
                }).Show();
        }
        else
        {
            _tabs.Remove(tab);
        }
    }

    public void UpdateTabControl()
    {
        string fileFolder = Mod.GetFileFolderPath();
        var tabsToRemove = _tabs.Where(tab => !File.Exists(Path.Combine(fileFolder, tab.Header.ToString()))).ToList();

        foreach (var tab in tabsToRemove)
        {
            _tabs.Remove(tab);
        }
    }

    
    private void AddNewTab(string fileName)
    {
        string filePath = Path.Combine(Mod.GetFileFolderPath(), fileName);
        var tab = new TabItem
        {
            Header = fileName,
            Content = new TextCodeEditor(filePath)
            
        };
        TextEditor editor = (tab.Content as TextCodeEditor).Content as TextEditor;
        _textMateInstallation =  editor.InstallTextMate(_registryOptions);
        _textMateInstallation.AppliedTheme += (o, installation) => TextMateInstallationOnAppliedTheme(o, installation, editor);
        editor.TextArea.TextEntered += (o, args) => textEditor_TextArea_TextEntered(o, args, editor);
        editor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(editor.Options);
        editor.TextArea.Caret.PositionChanged += (o, args) => Caret_PositionChanged(o, args, editor);
        editor.TextArea.LeftMargins.Insert(0, _margin);
        Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
        
        var contextMenu = new ContextMenu
        {
            ItemsSource = new List<MenuItem>
            {
                new MenuItem { Header = "Copy", Command = ((CodeEditorViewModel)DataContext).CopyCommand, CommandParameter = editor.TextArea },
                new MenuItem { Header = "Cut", Command = ((CodeEditorViewModel)DataContext).CutCommand, CommandParameter = editor.TextArea },
                new MenuItem { Header = "Paste", Command = ((CodeEditorViewModel)DataContext).PasteCommand, CommandParameter = editor.TextArea },
                new MenuItem { Header = "-" },
                new MenuItem { Header = "Select All", Command = ((CodeEditorViewModel)DataContext).SelectAllCommand, CommandParameter = editor.TextArea }
            }
        };
    
        editor.ContextMenu = contextMenu;
    
        // Add key bindings
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift), Command = ((CodeEditorViewModel)DataContext).SaveModCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.S, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).SaveFileCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.N, KeyModifiers.Control | KeyModifiers.Shift), Command = ((CodeEditorViewModel)DataContext).NewModCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.O, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).LoadExistingModCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.O, KeyModifiers.Control | KeyModifiers.Shift), Command = ((CodeEditorViewModel)DataContext).LoadModDialogCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.N, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).NewFileCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.F4, KeyModifiers.Alt), Command = ((CodeEditorViewModel)DataContext).QuitAppCommand });
    
        // Editing 
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.C, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).CopyCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.X, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).CutCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.V, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).PasteCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.A, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).SelectAllCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Z, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).UndoCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Y, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).RedoCommand });
    
        // Search
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.F, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).FindCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.H, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).ReplaceCommand });
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.G, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).GoToLineCommand });
        
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.W, KeyModifiers.Control), Command = ((CodeEditorViewModel)DataContext).CloseTabCommand});
        
        editor.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.N, KeyModifiers.Control | KeyModifiers.Alt), Command = ((CodeEditorViewModel)DataContext).ConfigureModCommand });

        
        
        _tabs.Add(tab);
        _tabControl.SelectedItem = tab;

    }
    private void OnTabMenuClosed(object? sender, RoutedEventArgs e)
    {
        rightClickedTab = null;
    }
    #endregion
    
    #region File Tree
    private void SetupFileTree(string folderPath)
    {
        var rootDirectory = new DirectoryInfo(folderPath);
        var rootItem = new TreeViewItem
        {
            Header = rootDirectory.Name,
            IsExpanded = true,
            Tag = rootDirectory
        };

        fileTree.Items.Clear();
        fileTree.Items.Add(rootItem);
        fileTree.SelectionChanged += (sender, e) =>
        {
            if (fileTree.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is string filePath)
            {
                if (File.Exists(filePath))
                {
                    foreach (var tab in _tabs)
                    {
                        if (tab.Header.ToString() == selectedItem.Header.ToString())
                        {
                            _tabControl.SelectedItem = tab;
                            return;
                        }
                    }
                    AddNewTab(selectedItem.Header.ToString());
                }
            }
        };

        PopulateTreeView(rootDirectory, rootItem);
        fileTree.IsVisible = App.Settings.ShowExplorer;
    }
    
    private void PopulateTreeView(DirectoryInfo directoryInfo, TreeViewItem parentItem)
    {
        var directories = directoryInfo.GetDirectories();
        var files = directoryInfo.GetFiles();

        foreach (var file in files)
        {
            var fileItem = new TreeViewItem
            {
                Header = file.Name,
                Tag = file.FullName
            };
            fileItem.PointerPressed += (sender, e) =>
            {
                var pointerPoint = e.GetCurrentPoint(fileItem);
                if (pointerPoint.Properties.IsRightButtonPressed) 
                {
                    var clickedFile = e.Source as Control;
                    while (clickedFile != null && clickedFile is not TreeViewItem)
                    {
                        clickedFile = (Control)clickedFile.Parent;
                    }
        
                    if (clickedFile is TreeViewItem item)
                    {
                        rightClickedFile = item;
                        e.Handled = true;
                    }
                }
            };

            parentItem.Items.Add(fileItem);
        }

        foreach (var directory in directories)
        {
            var dirItem = new TreeViewItem
            {
                Header = directory.Name,
                IsExpanded = true,
                Tag = directory.FullName
            };

            parentItem.Items.Add(dirItem);
            PopulateTreeView(directory, dirItem);
        }
    }
    
    public void UpdateFileTree()
    {
        fileTree.Items.Clear();

        var rootDirectory = new DirectoryInfo(Mod.GetFileFolderPath());
        var rootItem = new TreeViewItem
        {
            Header = rootDirectory.Name,
            IsExpanded = true,
            Tag = rootDirectory
        };

        PopulateTreeView(rootDirectory, rootItem);

        fileTree.Items.Add(rootItem);
    }
    #endregion

    #region Text Editor
    public TextEditor GetCurrentTextEditor()
    {
        return (_tabControl.SelectedContent as TextCodeEditor).Content as TextEditor;
    }
    
    #region Visuals
    public void UpdateVisuals()
        {
            Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");
            _currentTheme = (int)App.Settings.SelectedTheme;
            _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
            foreach (var tab in _tabs)
            {
                TextEditor editor = (tab.Content as TextCodeEditor).Content as TextEditor;
                _textMateInstallation = editor.InstallTextMate(_registryOptions);
                _textMateInstallation.AppliedTheme +=
                    (o, installation) => TextMateInstallationOnAppliedTheme(o, installation, editor);
                editor.ShowLineNumbers = App.Settings.ShowLineNumbers;
                _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
            }
        }
    
    private void Caret_PositionChanged(object sender, EventArgs e, TextEditor textEditor)
    {
        _statusTextBlock.Text = string.Format("Line {0} Column {1}",
            textEditor.TextArea.Caret.Line,
            textEditor.TextArea.Caret.Column);
    }
    private void TextMateInstallationOnAppliedTheme(object sender, TextMate.Installation e, TextEditor textEditor)
    {
        ApplyThemeColorsToEditor(e);
        ApplyThemeColorsToWindow(e);
    }

        void ApplyThemeColorsToEditor(TextMate.Installation e)
        {
            foreach (var tab in _tabs)
            {
                TextEditor _editor = (tab.Content as TextCodeEditor).Content as TextEditor;
                ApplyBrushAction(e, "editor.background",brush => _editor.Background = brush);
                ApplyBrushAction(e, "editor.foreground",brush => _editor.Foreground = brush);
    
                if (!ApplyBrushAction(e, "editor.selectionBackground",
                        brush => _editor.TextArea.SelectionBrush = brush))
                {
                    if (Application.Current!.TryGetResource("TextAreaSelectionBrush", out var resourceObject))
                    {
                        if (resourceObject is IBrush brush)
                        {
                            _editor.TextArea.SelectionBrush = brush;
                        }
                    }
                }
    
                if (!ApplyBrushAction(e, "editor.lineHighlightBackground",
                        brush =>
                        {
                            _editor.TextArea.TextView.CurrentLineBackground = brush;
                            _editor.TextArea.TextView.CurrentLineBorder = new Pen(brush); 
                        }))
                {
                    _editor.TextArea.TextView.SetDefaultHighlightLineColors();
                }
    
                if (!ApplyBrushAction(e, "editorLineNumber.foreground",
                        brush => _editor.LineNumbersForeground = brush))
                {
                    _editor.LineNumbersForeground = _editor.Foreground;
                }
            }
        }

        private void ApplyThemeColorsToWindow(TextMate.Installation e)
        {
            var panel = this.Find<StackPanel>("StatusBar");
            if (panel == null)
            {
                return;
            }

            if (!ApplyBrushAction(e, "statusBar.background", brush => panel.Background = brush))
            {
                panel.Background = Brushes.Purple;
            }

            if (!ApplyBrushAction(e, "statusBar.foreground", brush => _statusTextBlock.Foreground = brush))
            {
                _statusTextBlock.Foreground = Brushes.White;
            }

            if (!ApplyBrushAction(e, "sideBar.background", brush => _margin.BackGroundBrush = brush))
            {
                _margin.SetDefaultBackgroundBrush();
            }

            //Applying the Editor background to the whole window for demo sake.
            ApplyBrushAction(e, "editor.background",brush => Background = brush);
            ApplyBrushAction(e, "editor.foreground",brush => Foreground = brush);
        }

        bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
        {
            if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
                return false;

            if (!Color.TryParse(colorString, out Color color))
                return false;

            var colorBrush = new SolidColorBrush(color);
            applyColorAction(colorBrush);
            return true;
        }
    private void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e, TextEditor textEditor)
    {
        if (e.Text == ".")
        {

            _completionWindow = new CompletionWindow(textEditor.TextArea);
            _completionWindow.Closed += (o, args) => _completionWindow = null;

            var data = _completionWindow.CompletionList.CompletionData;

            for (int i = 0; i < 500; i++)
            {
                data.Add(new MyCompletionData("Item" + i.ToString()));
            }

            data.Insert(20, new MyCompletionData("long item to demosntrate dynamic poup resizing"));

            _completionWindow.Show();
        }
        else if (e.Text == "(")
        {
            _insightWindow = new OverloadInsightWindow(textEditor.TextArea);
            _insightWindow.Closed += (o, args) => _insightWindow = null;

            _insightWindow.Provider = new MyOverloadProvider(new[]
            {
                ("Method1(int, string)", "Method1 description"),
                ("Method2(int)", "Method2 description"),
                ("Method3(string)", "Method3 description"),
            });

            _insightWindow.Show();
        }
    }
    
    private void textEditor_TextArea_TextEntering(object sender, TextInputEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]))
            {
                // Whenever a non-letter is typed while the completion window is open,
                // insert the currently selected element.
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }

        _insightWindow?.Hide();

        // Do not set e.Handled=true.
        // We still want to insert the character that was typed.
    }
    #endregion
    #endregion

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private class MyOverloadProvider : IOverloadProvider
        {
            private readonly IList<(string header, string content)> _items;
            private int _selectedIndex;

            public MyOverloadProvider(IList<(string header, string content)> items)
            {
                _items = items;
                SelectedIndex = 0;
            }

            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    _selectedIndex = value;
                    OnPropertyChanged();
                    // ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(CurrentHeader));
                    OnPropertyChanged(nameof(CurrentContent));
                    // ReSharper restore ExplicitCallerInfoArgument
                }
            }

            public int Count => _items.Count;
            public string CurrentIndexText => $"{SelectedIndex + 1} of {Count}";
            public object CurrentHeader => _items[SelectedIndex].header;
            public object CurrentContent => _items[SelectedIndex].content;

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class MyCompletionData : ICompletionData
        {
            public MyCompletionData(string text)
            {
                Text = text;
            }

            public IImage Image => null;

            public string Text { get; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content => _contentControl ??= BuildContentControl();

            public object Description => "Description for " + Text;

            public double Priority { get; } = 0;

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }

            Control BuildContentControl()
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = Text;
                textBlock.Margin = new Thickness(5);

                return textBlock;
            }

            Control _contentControl;
        }

        
}
public class TextCodeEditor : UserControl
{
    public TextCodeEditor(string filePath)
    {
        IHighlightingDefinition syntax = HighlightingManager.Instance.GetDefinition("C#");
        string code = File.ReadAllText(filePath);
        TextEditorOptions options = new TextEditorOptions
        {
            HighlightCurrentLine = true,
            EnableHyperlinks = true,
            CutCopyWholeLine = true,
            AllowToggleOverstrikeMode = true,
            ShowBoxForControlCharacters = true,
            ConvertTabsToSpaces = true,
        };
        Content = new TextEditor
        {
            ShowLineNumbers = App.Settings.ShowLineNumbers,
            FontSize = 14,
            FontFamily = new FontFamily("Cascadia Code"),
            SyntaxHighlighting = syntax,
            Background = Brushes.Black,
            Foreground = Brushes.White,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Options = options,
            Document = new TextDocument(code)
        };
    }
}