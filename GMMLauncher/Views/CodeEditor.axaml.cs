using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation.CSharp;
using GMMLauncher.ViewModels;

namespace GMMLauncher.Views;

public partial class CodeEditor : Window
{
    public readonly TextEditor _editor;
    public TextMate.Installation _textMateInstallation;
    private CompletionWindow _completionWindow;
    private OverloadInsightWindow _insightWindow;
    public RegistryOptions _registryOptions;
    private int _currentTheme = (int)App.Settings.SelectedTheme;
    private TextBlock _statusTextBlock;
    private CustomMargin _margin;

    public Mod Mod;
    
    public CodeEditor(Mod mod)
    {
        this.Mod = mod;
        InitializeComponent();

        _editor = this.FindControl<TextEditor>("Editor");
        _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
        _textMateInstallation = _editor.InstallTextMate(_registryOptions);
        _textMateInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;

        Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");

        string filePath = mod.GetFilePath();
        if (!File.Exists(filePath))
        {
            mod.CreateMainFile();
        }

        string content = File.ReadAllText(filePath);
        _editor.Document = new TextDocument(content);
        _editor.FontFamily = new FontFamily("Cascadia Code");
        _editor.FontSize = 14;

        SetupFileTree(Path.Combine(mod.GetFolderPath(), "Files"));

        _editor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
        _editor.ShowLineNumbers = App.Settings.ShowLineNumbers;
        _editor.Options.ShowTabs = true;
        _editor.TextArea.RightClickMovesCaret = true;
        _editor.Options.HighlightCurrentLine = true;
        _editor.Options.EnableHyperlinks = true;
        _editor.Options.CutCopyWholeLine = true;
        _editor.Options.AllowToggleOverstrikeMode = true;
        _editor.Options.EnableTextDragDrop = true;
        _editor.Options.ShowBoxForControlCharacters = true;
        _editor.Options.ConvertTabsToSpaces = true;
        _editor.Options.IndentationSize = 4;
        _editor.TextArea.Background = this.Background;
        
        _statusTextBlock = this.Find<TextBlock>("StatusText");
        
        _editor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
        _editor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        _editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(_editor.Options);
        _editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        _editor.TextArea.LeftMargins.Insert(0, _margin);
        
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
        DataContext = new CodeEditorViewModel(this);
    }

    public void UpdateVisuals()
    {
        Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");
        _currentTheme = (int)App.Settings.SelectedTheme;
        _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
        _textMateInstallation = _editor.InstallTextMate(_registryOptions);
        _textMateInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;
        _editor.ShowLineNumbers = App.Settings.ShowLineNumbers;
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
    }

    private void SetupFileTree(string folderPath)
    {
        var fileTree = this.FindControl<TreeView>("FileTree");

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
                    string content = File.ReadAllText(filePath);
                    _editor.Document = new TextDocument(content);
                }
            }
        };

        PopulateTreeView(rootDirectory, rootItem);
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
    
    private void UpdateFileTree(string folderPath)
    {
        var fileTree = this.FindControl<TreeView>("FileTree");
        if (fileTree == null) return;

        fileTree.Items.Clear();

        var rootDirectory = new DirectoryInfo(folderPath);
        var rootItem = new TreeViewItem
        {
            Header = rootDirectory.Name,
            IsExpanded = true,
            Tag = rootDirectory
        };

        PopulateTreeView(rootDirectory, rootItem);

        fileTree.Items.Add(rootItem);
    }


    
    private void Caret_PositionChanged(object sender, EventArgs e)
    {
        _statusTextBlock.Text = string.Format("Line {0} Column {1}",
            _editor.TextArea.Caret.Line,
            _editor.TextArea.Caret.Column);
    }
    private void TextMateInstallationOnAppliedTheme(object sender, TextMate.Installation e)
    {
        ApplyThemeColorsToEditor(e);
        ApplyThemeColorsToWindow(e);
    }

        void ApplyThemeColorsToEditor(TextMate.Installation e)
        {
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
    private void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e)
    {
        if (e.Text == ".")
        {

            _completionWindow = new CompletionWindow(_editor.TextArea);
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
            _insightWindow = new OverloadInsightWindow(_editor.TextArea);
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