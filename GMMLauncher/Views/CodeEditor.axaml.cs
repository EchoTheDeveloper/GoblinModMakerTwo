using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
using GMMBackend;
using GMMLauncher.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Tabalonia.Controls;
using Tabalonia.Events;
using TextDocument = AvaloniaEdit.Document.TextDocument;

namespace GMMLauncher.Views;

public partial class CodeEditor : Window
{
    private TextMate.Installation _textMateInstallation;
    private CompletionWindow _completionWindow;
    private OverloadInsightWindow _insightWindow;
    public RegistryOptions _registryOptions { get; private set; }
    private int _currentTheme = (int)App.Settings.SelectedTheme;
    private TextBlock? _statusTextBlock;
    private TextBlock? _titleTextBlock;
    public CustomMargin _margin { get; }
    
    public TabsControl? _tabControl { get; set; }
    public TabItemViewModel lastClickedTab { get; set; }
    
    public TreeViewItem rightClickedFile { get; set; }
    public TreeView? fileTree  { get; set; }
    

    public CodeEditorViewModel viewModel;
    
    public Mod Mod;
    private AdhocWorkspace workspace = new();
    private ProjectInfo projectInfo;
    private Project project;
    
    public static Dictionary<TextEditor, Document> documentMap = new();
    public CodeEditor(Mod mod)
    {
        viewModel = new CodeEditorViewModel(this);
        DataContext = viewModel;
        Mod = mod;
        InitializeComponent();
        
        WindowManager.Add(this);
        
        projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), mod.NameNoSpaces, mod.NameNoSpaces, LanguageNames.CSharp);
        project = workspace.AddProject(projectInfo);
        
        
        _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
        _statusTextBlock = this.Find<TextBlock>("StatusText");
        _tabControl = this.FindControl<TabsControl>("TabControl");
        fileTree = this.FindControl<TreeView>("FileTree");
        _titleTextBlock = this.FindControl<TextBlock>("TitleText");

        Title = $"GMM - {mod.Name}";
        _titleTextBlock.Text = $"Goblin Mod Maker - {mod.Name}";
        
        _tabControl.PointerPressed += TabControl_PointerPressed;
        _tabControl.TabClosing += TabControl_CloseTab;
        _tabControl.LastTabClosedAction = null;

        
        string filePath = mod.GetFilePath();
        if (!File.Exists(filePath))
        {
            mod.CreateMainFile();
        }

        SetupFileTree(mod.GetFileFolderPath());
        AddNewTab(Path.Combine(mod.NameNoSpaces + ".cs"));
    }
    public void AddNewFileToProject(string name, SourceText sourceText, string filePath)
    {
        project = project.AddDocument(name, sourceText, filePath:filePath).Project;
        workspace.TryApplyChanges(project.Solution);
    }
    #region Tabs
    private void TabControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(_tabControl);
        var clicked = e.Source as Control;

        while (clicked != null && clicked.DataContext is not TabItemViewModel)
        {
            clicked = clicked.Parent as Control;
        }

        if (clicked?.DataContext is TabItemViewModel tabVM)
        {
            lastClickedTab = tabVM;
            if (pointerPoint.Properties.IsMiddleButtonPressed)
            {
                CloseTab(tabVM);
                lastClickedTab = null;
            }
            e.Handled = true;
        }
    }

    private void TabControl_CloseTab(object? sender, TabClosingEventArgs e)
    {
        var tab = e.Item as TabItemViewModel;

        if ((tab?.Content as TextCodeEditor)?.Content is not TextEditor editor || !editor.IsModified)
            return;

        e.Cancel = true;

        if (tab != null) CloseTab(tab);
    }
    public void CloseTab(TabItemViewModel tab)
    {
        TextEditor? textEditor = (tab.Content as TextCodeEditor)?.Content as TextEditor;
        if (textEditor.IsModified)
        {
            new InfoWindow("File Not Saved", InfoWindowType.YesNo, $"{tab.Header.ToString()} is not saved, would you like to save now?", true,
                (window) =>
                {
                    Mod.SaveFile(tab);
                    viewModel.TabItems.Remove(tab);
                    window.Close();
                },
                (window) =>
                {
                    viewModel.TabItems.Remove(tab);
                    window.Close();
                }).Show();
        }
        else
        {
            viewModel.TabItems.Remove(tab);
        }
    }

    public void UpdateTabControl()
    {
        string fileFolder = Mod.GetFileFolderPath();
        var tabsToRemove = viewModel.TabItems.Where(tab => !File.Exists(Path.Combine(fileFolder, tab.Header.ToString()))).ToList();

        foreach (var tab in tabsToRemove)
        {
            viewModel.TabItems.Remove(tab);
        }
    }

    public TabItemViewModel CreateTab(string fileName)
    {
        string filePath = Path.Combine(Mod.GetFileFolderPath(), fileName);
        var tab = new TabItemViewModel
        {
            Header = fileName,
            Content = new TextCodeEditor(filePath)
        };

        var editor = ((TextCodeEditor)tab.Content).Content as TextEditor;
        var document = GetDocumentByName(fileName);
        documentMap.Add(editor, document);
        _textMateInstallation = editor.InstallTextMate(_registryOptions);
        _textMateInstallation.AppliedTheme += (o, installation) => TextMateInstallationOnAppliedTheme(o, installation, editor);
        // editor.TextArea.TextEntered += (o, args) => textEditor_TextArea_TextEntered(o, args, editor);
        // editor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        editor.TextChanged += textEditor_TextChanged;
        editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(editor.Options);
        editor.TextArea.Caret.PositionChanged += (o, args) => Caret_PositionChanged(o, args, editor);
        editor.TextArea.LeftMargins.Insert(0, _margin);

        var csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");
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
        return tab;
    }
    
    public void AddNewTab(string fileName)
    {
        var tab = CreateTab(fileName);
        viewModel.TabItems.Add(tab);
        _tabControl.SelectedItem = tab;
    }
    
    public Document? GetDocumentByName(string fileName)
    {
        return project.Documents.FirstOrDefault(doc =>
            string.Equals(Path.GetFileName(doc.FilePath), Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));
    }

    private void OnTabMenuClosed(object? sender, RoutedEventArgs e)
    {
        lastClickedTab = null;
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
        fileTree.SelectionChanged += (_,_) =>
        {
            if (fileTree.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is string filePath)
            {
                if (File.Exists(filePath))
                {
                    foreach (var tab in viewModel.TabItems)
                    {
                        if (tab.Header == selectedItem.Header.ToString())
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
    
        var existingFilePaths = new HashSet<string>(
            project.Documents
                .Where(d => d.FilePath != null)
                .Select(d => Path.GetFullPath(d.FilePath!)),
            StringComparer.OrdinalIgnoreCase);
    
        foreach (var file in files)
        {
            string fullPath = Path.GetFullPath(file.FullName);
    
            if (file.Extension == ".cs" && !existingFilePaths.Contains(fullPath))
            {
                var sourceText = SourceText.From(File.ReadAllText(fullPath));
                AddNewFileToProject(file.Name, sourceText, fullPath);
            }
    
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
    public TextEditor? GetCurrentTextEditor()
    {
        return ((_tabControl.SelectedContent as TabItemViewModel)?.Content as TextCodeEditor)?.Content as TextEditor;
    }
    private Document? GetDocumentForEditor(TextEditor editor)
    {
        return documentMap.TryGetValue(editor, out var doc) ? doc : null;
    }

    
    #region Visuals
    public void UpdateVisuals()
        {
            Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");
            _currentTheme = (int)App.Settings.SelectedTheme;
            _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
            foreach (var tab in viewModel.TabItems)
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
    public void TextMateInstallationOnAppliedTheme(object sender, TextMate.Installation e, TextEditor textEditor)
    {
        ApplyThemeColorsToEditor(e);
        ApplyThemeColorsToWindow(e);
    }

    void ApplyThemeColorsToEditor(TextMate.Installation e)
    {
        foreach (var tab in viewModel.TabItems)
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
    
    private async void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e, TextEditor textEditor)
    {
        var document = GetDocumentForEditor(textEditor);
        if (document == null)
            return;
        var updatedText = SourceText.From(textEditor.Text);
        document = document.WithText(updatedText);


        var completionService = CompletionService.GetService(document);
        if (completionService == null)
            return;
        
        _completionWindow = new CompletionWindow(textEditor.TextArea);
        _completionWindow.Closed += (o, args) => _completionWindow = null;
        
        var data = _completionWindow.CompletionList.CompletionData;
        var position = textEditor.CaretOffset;
        
        var completions = await completionService.GetCompletionsAsync(document, position);

        if (completions != null)
        {
            foreach (var item in completions.ItemsList)
            {
                data.Add(new MyCompletionData(item.DisplayText));
            }
        }
        _completionWindow.Show();
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

    private void textEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_tabControl?.SelectedContent is not TabItemViewModel selectedTab) return;
        if (!selectedTab.Header.EndsWith("*"))
        {
            selectedTab.Header += "*";
        }
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