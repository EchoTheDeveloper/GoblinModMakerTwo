using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation.CSharp;
using GMMLauncher.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Tabalonia.Controls;
using Tabalonia.Events;
using TextDocument = AvaloniaEdit.Document.TextDocument;
using AvaloniaEdit.Folding;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using Document = Microsoft.CodeAnalysis.Document;
using Grammar = TextMateSharp.Internal.Grammars.Grammar;
using ISymbol = Microsoft.CodeAnalysis.ISymbol;
using Location = Microsoft.CodeAnalysis.Location;
using SymbolKind = Microsoft.CodeAnalysis.SymbolKind;

namespace GMMLauncher.Views;

public partial class CodeEditor : Window
{
    public CodeEditorViewModel viewModel;
    public Mod Mod;
    private AdhocWorkspace workspace = new();
    private ProjectInfo projectInfo;
    private Project project;
    
    private string _currentTheme = App.Settings.SelectedTheme;
    private TextMate.Installation installation;
    
    private TextBlock? _caretTextBlock;
    private TextBlock? _titleTextBlock;
    
    private static Dictionary<TextEditor, Document?> documentMap = new();
    private static Dictionary<string, DecompiledTabData> decompiledMap = new();
    
    public TabsControl? _tabControl { get; set; }
    public TabItemViewModel lastClickedTab { get; set; }
    private List<TabItemViewModel> _lastOpenTabs = new();
    
    public TreeViewItem rightClickedFile { get; set; }
    public TreeView? fileTree  { get; set; }
    
    private readonly CSharpSymbolService _symbolService = new();
    private ToolTip? _symbolToolTip;
    private ISymbol? _lastHoveredSymbol;
    
    private CompletionWindow _completionWindow;
    private CancellationTokenSource _completionCancellation;
    private OverloadInsightWindow _insightWindow;
    private int _completionRequestId;

    
    private CancellationTokenSource? _diagnosticCancellation;
    private CancellationTokenSource? _hoverCancellation;
    private CancellationTokenSource? _filterCancellation;
    private bool _navigatingToDefinition;
    private bool hovered;
    
    private List<ChordedKeyBind> _chordBindings = new();
    private KeyGesture? _activePrefix;
    private DispatcherTimer? _chordTimer;
    private readonly Action<string>? _onStatusChanged;
    
    private string scriptAssembliesPath => Path.Combine(App.Settings.FindSteamDirectory(), "Isle Goblin_Data", "Managed");
    private string modAssembliesPath => Path.Combine(App.Settings.FindSteamDirectory(), "BepInEx");
    public CodeEditor(Mod mod)
    {
        viewModel = new CodeEditorViewModel(this);
        DataContext = viewModel;
        Mod = mod;
        InitializeComponent();
        
        var references = new List<MetadataReference>();
        
        foreach (var dll in Directory.GetFiles(scriptAssembliesPath, "*.dll"))
        {
            references.Add(MetadataReference.CreateFromFile(dll));
        }
        foreach (var dll in Directory.GetFiles(modAssembliesPath, "*.dll", SearchOption.AllDirectories))
        {
            references.Add(MetadataReference.CreateFromFile(dll));
        }

        
        projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), mod.NameNoSpaces, mod.NameNoSpaces, LanguageNames.CSharp, metadataReferences: references);
        project = workspace.AddProject(projectInfo);
        
        _currentTheme = App.Settings.SelectedTheme;
        _caretTextBlock = this.Find<TextBlock>("CaretText");
        _tabControl = this.FindControl<TabsControl>("TabControl");
        fileTree = this.FindControl<TreeView>("FileTree");
        _titleTextBlock = this.FindControl<TextBlock>("TitleText");

        Title = $"GMM - {mod.Name}";
        _titleTextBlock.Text = $"Goblin Mod Maker - {mod.Name}";
        
        _tabControl.TabClosing += TabControl_CloseTab;
        _tabControl.LastTabClosedAction = null;
        
        _chordTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _chordTimer.Tick += (_, _) => ResetChordState();

        _onStatusChanged = s => this.FindControl<TextBlock>("StatusText").Text = s; 
        _chordBindings.Add(new ChordedKeyBind("Ctrl+K", "Ctrl+D0", () => ToggleFolding(true)));
        _chordBindings.Add(new ChordedKeyBind("Ctrl+K", "Ctrl+J", () => ToggleFolding(false)));
        _chordBindings.Add(new ChordedKeyBind("Ctrl+K", "Ctrl+C", () => Comment()));
        _chordBindings.Add(new ChordedKeyBind("Ctrl+K", "Ctrl+U", () => Uncomment()));
        _chordBindings.Add(new ChordedKeyBind("Ctrl+C", "", () => viewModel.CopyMouse()));
        AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel);
        
        string filePath = mod.GetFilePath();
        if (!File.Exists(filePath))
        {
            mod.CreateMainFile();
        }

        SetupFileTree(mod.GetFileFolderPath());
        AddNewTab(filePath);
        App.Instance.ApplyThemeColorsToResources(installation);
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        WindowManager.Add(this);
    }
    
    #region Tabs
    // Tab Interaction Events
    private void TabControl_CloseTab(object? sender, TabClosingEventArgs e)
    {
        var tab = e.Item as TabItemViewModel;

        if ((tab?.Content as TextCodeEditor)?.Content is not TextEditor editor || !editor.IsModified)
            return;

        e.Cancel = true;

        if (tab != null) CloseTab(tab);
    }
    private async Task ShowTabTooltip(object? sender, TabItemViewModel tab)
    {
        
        if (tab == null || sender is not DragTabItem dragTab)
            return;
        
        var cancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(30, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellation.IsCancellationRequested)
            return;
        
        var tip = new ToolTip()
        {
            Content = new TextBlock()
            {
                Text = tab.FilePath,
            },
            MaxWidth = 600
        };
        
        ToolTip.SetTip(dragTab, tip);
    }
    private void HideTabTooltip(object? sender, PointerEventArgs e)
    {
        if (sender is not DragTabItem tab)
            return; 
        ToolTip.SetIsOpen(tab, false);
    }
    private async Task ShowDecompiledTooltip(object? sender, DecompiledTabData data)
    {
        if (sender is not DragTabItem tab)
            return; 
        
        var cancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(30, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellation.IsCancellationRequested)
            return;
        
        var tip = new ToolTip()
        {
            Content = new TextBlock()
            {
                Text = $"Decompiled File\n\nSource: {data.FullPath}\nAssembly: {data.Symbol.ContainingAssembly}\nMVID: {data.Mvid}",
            },
            MaxWidth = 600
        };
        
        ToolTip.SetTip(tab, tip);
    }
    private void Tab_PointerPressed(object? sender, PointerPressedEventArgs e, TabItemViewModel tab)
    {
        if (sender is not DragTabItem || tab == null)
            return; 
        
        var pointerPoint = e.GetCurrentPoint(_tabControl);
        
        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            CloseTab(tab);
        }
        else
        {
            lastClickedTab = tab;
        }
        e.Handled = true;
    }
    private void OnTabMenuClosed(object? sender, RoutedEventArgs e)
    {
        lastClickedTab = null;
    }
    
    // Tab Actions
    public void CloseTab(TabItemViewModel tab)
    {
        TextEditor? textEditor = (tab.Content as TextCodeEditor)?.Content as TextEditor;
        if (textEditor.IsModified)
        {
            new InfoWindow("File Not Saved", InfoWindowType.YesNo, $"{tab.FileName.ToString()} is not saved, would you like to save now?", true,
                (window) =>
                {
                    Mod.SaveFile(tab);
                    RemoveTab(tab);
                    window.Close();
                },
                (window) =>
                {
                    RemoveTab(tab);
                    window.Close();
                }).Show();
        }
        else
        {
            RemoveTab(tab);
        }
    }
    private void RemoveTab(TabItemViewModel tab)
    {
        viewModel.TabItems.Remove(tab);
        
        if (_lastOpenTabs.Contains(tab)) _lastOpenTabs.Remove(tab);
        
        _lastOpenTabs.Insert(0, tab);
    }
    public void UpdateTabControl()
    {
        var tabsToRemove = viewModel.TabItems.Where(tab => !File.Exists(tab.FilePath.ToString()) && tab.DecompiledData == null).ToList();

        foreach (var tab in tabsToRemove)
        {
            viewModel.TabItems.Remove(tab);
        }
    }
    public void OpenLastTab()
    {
        if (_lastOpenTabs.Count < 1 || viewModel.TabItems.Contains(_lastOpenTabs[0])) return;
            
        if (_lastOpenTabs[0].DecompiledData == null)
        {
            AddNewTab(_lastOpenTabs[0]);
        }
        else
        {
            AddNewDecompiledTab(_lastOpenTabs[0]);
        }
    }
    
    // New Tabs
    public TabItemViewModel CreateTab(string filePath)
    {
        var tab = new TabItemViewModel
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            Content = new TextCodeEditor(filePath),
        };
        var codeEditor = (TextCodeEditor)tab.Content;
        var editor = codeEditor.Content as TextEditor;
        var document = GetDocumentByName(tab.FileName);
        documentMap.TryAdd(editor, document);
        if (_currentTheme != App.Settings.SelectedTheme)
        {
            _currentTheme = App.Settings.SelectedTheme;
        }
        
        if (_lastOpenTabs.Contains(tab)) _lastOpenTabs.Remove(tab);
        codeEditor._registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        codeEditor._textMateInstallation = editor.InstallTextMate(codeEditor._registryOptions);

        App.ApplyTheme(codeEditor._textMateInstallation, _currentTheme);
        codeEditor._textMateInstallation.AppliedTheme += (_, installation) => TextMateInstallationOnAppliedTheme(installation);
        editor.TextArea.TextEntered += (_, args) => textEditor_TextArea_TextEntered(args, editor);
        editor.TextArea.TextEntering += (_, args) => textEditor_TextArea_TextEntering(args, editor);
        editor.TextArea.PointerMoved += (_, e) => HandlePointerHover(codeEditor, e);
        editor.TextArea.PointerPressed += (_, e) => textEditor_PointerPressed(editor, e);
        editor.TextArea.PointerExited += (_, e) => textEditor_TextArea_PointerExited(e, editor);
        editor.TextArea.AddHandler(
            PointerPressedEvent,
            (_, e) => textEditor_PointerPressed(editor, e),
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        editor.TextArea.Caret.PositionChanged += (_, _) => Caret_PositionChanged(editor);
        editor.TextChanged += (_, _) => textEditor_TextChanged(codeEditor);
        editor.KeyDown += (_, args) => textEditor_KeyDown(args, editor);
        editor.LostFocus += textEditor_LostFocus;
        editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(editor.Options);
        codeEditor._foldingManager = FoldingManager.Install(editor.TextArea);
        // editor.TextArea.LeftMargins.Add(new CustomFoldingMargin(_foldingManager));
        editor.TextArea.LeftMargins.Add(new CustomMargin());
        codeEditor._diagnosticRenderer = new CSharpDiagnosticRenderer(editor);
        editor.TextArea.TextView.BackgroundRenderers.Add(codeEditor._diagnosticRenderer);
        _ = UpdateEditor(codeEditor); 
        ApplyThemeToTabEditor(tab, codeEditor._textMateInstallation);
        installation = codeEditor._textMateInstallation;

        var csharpLanguage = codeEditor._registryOptions.GetLanguageByExtension(".cs");
        codeEditor._textMateInstallation.SetGrammar(codeEditor._registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
        
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
    public void AddNewTab(string filePath, bool focus = true)
    {
        var tab = CreateTab(filePath);
        
        viewModel.TabItems.Add(tab);
        
        if (focus) _tabControl.SelectedItem = tab;
        
        Dispatcher.UIThread.Post(() =>
        {
            if (_tabControl.ContainerFromItem(tab) is DragTabItem dragTab)
            {
                dragTab.PointerPressed += (o, args) =>
                    Tab_PointerPressed(o, args, tab);
                dragTab.PointerEntered += (o, args) => _ = ShowTabTooltip(o, tab);
                dragTab.PointerExited += HideTabTooltip;
            }
        });
    }
    public void AddNewTab(TabItemViewModel tab, bool focus = true)
    {
        viewModel.TabItems.Add(tab);
        
        if (focus) _tabControl.SelectedItem = tab;
        
        Dispatcher.UIThread.Post(() =>
        {
            if (_tabControl.ContainerFromItem(tab) is DragTabItem dragTab)
            {
                dragTab.PointerPressed += (o, args) =>
                    Tab_PointerPressed(o, args, tab);
                dragTab.PointerEntered += (o, args) => _ = ShowTabTooltip(o, tab);
                dragTab.PointerExited += HideTabTooltip;
            }
        });
    }

    public TabItemViewModel CreateDecompiledTab(DecompiledTabData data)
    {
        var tab = new TabItemViewModel
        {
            FileName = "@" + data.Name,
            FilePath = data.FullPath,
            Content = new TextCodeEditor(data),
            DecompiledData = data
        };
        var codeEditor = (TextCodeEditor)tab.Content;
        var editor = codeEditor.Content as TextEditor;
        var document = GetDocumentByName(data.FullPath);
        decompiledMap.TryAdd(data.FullPath, data);
        documentMap.TryAdd(editor, document);
        editor.IsReadOnly = true;
        if (_currentTheme != App.Settings.SelectedTheme)
        {
            _currentTheme = App.Settings.SelectedTheme;
        }
        
        if (_lastOpenTabs.Contains(tab)) _lastOpenTabs.Remove(tab);
        codeEditor._registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        codeEditor._textMateInstallation = editor.InstallTextMate(codeEditor._registryOptions);

        App.ApplyTheme(codeEditor._textMateInstallation, _currentTheme);
        editor.TextArea.PointerMoved += (_, e) => HandlePointerHover(codeEditor, e);
        editor.TextArea.PointerPressed += (_, e) => textEditor_PointerPressed(editor, e);
        editor.TextArea.AddHandler(
            PointerPressedEvent,
            (_, e) => textEditor_PointerPressed(editor, e),
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        editor.TextArea.Caret.PositionChanged += (_, _) => Caret_PositionChanged(editor);
        editor.LostFocus += textEditor_LostFocus;
        editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(editor.Options);
        codeEditor._foldingManager = FoldingManager.Install(editor.TextArea);
        // editor.TextArea.LeftMargins.Add(new CustomFoldingMargin(_foldingManager));
        editor.TextArea.LeftMargins.Add(new CustomMargin());
        codeEditor._diagnosticRenderer = new CSharpDiagnosticRenderer(editor);
        editor.TextArea.TextView.BackgroundRenderers.Add(codeEditor._diagnosticRenderer);
        _ = UpdateEditor(codeEditor); 
        ApplyThemeToTabEditor(tab, codeEditor._textMateInstallation);
        installation = codeEditor._textMateInstallation;

        var csharpLanguage = codeEditor._registryOptions.GetLanguageByExtension(".cs");
        codeEditor._textMateInstallation.SetGrammar(codeEditor._registryOptions.GetScopeByLanguageId(csharpLanguage.Id));

        var contextMenu = new ContextMenu
         {
            ItemsSource = new List<MenuItem>
            {
                new MenuItem { Header = "Select All", Command = ((CodeEditorViewModel)DataContext).SelectAllCommand, CommandParameter = editor.TextArea }
            }
        };

        editor.ContextMenu = contextMenu;
        return tab;
    }
    public TabItemViewModel AddNewDecompiledTab(DecompiledTabData data)
    {
        var tab = CreateDecompiledTab(data);
        viewModel.TabItems.Add(tab);
        _tabControl.SelectedItem = tab;
        Dispatcher.UIThread.Post(() =>
        {
            if (_tabControl.ContainerFromItem(tab) is DragTabItem dragTab)
            {
                dragTab.PointerPressed += (o, args) => Tab_PointerPressed(o, args, tab);
                dragTab.PointerEntered += (o, args) => _ = ShowDecompiledTooltip(o, data);
                dragTab.PointerExited += HideTabTooltip;
            }
        });
        return tab;
    }
    public TabItemViewModel AddNewDecompiledTab(TabItemViewModel tab)
    {
        viewModel.TabItems.Add(tab);
        _tabControl.SelectedItem = tab;
        Dispatcher.UIThread.Post(() =>
        {
            if (_tabControl.ContainerFromItem(tab) is DragTabItem dragTab)
            {
                dragTab.PointerPressed += (o, args) => Tab_PointerPressed(o, args, tab);
                dragTab.PointerEntered += (o, args) => _ = ShowDecompiledTooltip(o, tab.DecompiledData);
                dragTab.PointerExited += HideTabTooltip;
            }
        });
        return tab;
    }

    // Helpers
    public Document? GetDocumentByName(string fileName)
    {
        return project.Documents.FirstOrDefault(doc => string.Equals(Path.GetFileName(doc.FilePath), Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));
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
            Tag = rootDirectory,
            CornerRadius = new CornerRadius(8),
            Foreground = (IBrush?)Resources["EditorForegroundBrush"],
            Padding = new Thickness(0,0,8,0)
        };
        
        rootItem.PointerPressed += (sender, e) =>
        {
            var pointerPoint = e.GetCurrentPoint(rootItem);
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
                        if (tab.FilePath == selectedItem.Tag.ToString())
                        {
                            _tabControl.SelectedItem = tab;
                            return;
                        }
                    }
                    AddNewTab(selectedItem.Tag.ToString());
                }
            }
        };

        PopulateTreeView(rootDirectory, rootItem);
        fileTree.IsVisible = App.Settings.ShowExplorer;
        fileTree.Width = Width + 100;
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
                Tag = file.FullName,
                CornerRadius = new CornerRadius(8),
                Foreground = (IBrush?)Resources["EditorForegroundBrush"],
                Padding = new Thickness(0,0,4,0)
            };
    
            fileItem.PointerPressed += (sender, e) =>
            {
                var pointerPoint = e.GetCurrentPoint(fileItem);
                
                if (pointerPoint.Properties.IsRightButtonPressed)
                {
                    rightClickedFile = fileItem;
                    e.Handled = true;
                }
                else if (pointerPoint.Properties.IsMiddleButtonPressed)
                {
                    if (File.Exists(file.FullName))
                    {
                        foreach (var tab in viewModel.TabItems)
                        {
                            if (tab.FilePath == file.FullName)
                            {
                                return;
                            }
                        }
                        AddNewTab(file.FullName, false);
                    }
                    e.Handled = true;
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
                Tag = directory.FullName,
                CornerRadius = new CornerRadius(8),
                Foreground = (IBrush?)Resources["EditorForegroundBrush"],
                Padding = new Thickness(0,0,4,0)
            };
            
            dirItem.PointerPressed += (sender, e) =>
            {
                var pointerPoint = e.GetCurrentPoint(dirItem);
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
            Tag = rootDirectory,
            CornerRadius = new CornerRadius(8),
            Foreground = (IBrush?)Resources["EditorForegroundBrush"],
            Padding = new Thickness(0,0,4,0)
        };

        PopulateTreeView(rootDirectory, rootItem);

        fileTree.Items.Add(rootItem);
    }
    
    // Helpers
    public void AddNewFileToProject(string name, SourceText sourceText, string filePath)
    {
        project = project.AddDocument(name, sourceText, filePath:filePath).Project;
        workspace.TryApplyChanges(project.Solution); // This is a problem for decompiled tabs
    }
    #endregion

    #region Text Editor
    // Events
    private void textEditor_PointerPressed(TextEditor editor, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(editor.TextArea.TextView);

        if (!properties.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }
        
        _ = GoToDefinition(editor);
        e.Handled = true;
    }
    private void textEditor_TextChanged(TextCodeEditor codeEditor)
    {
        _ = UpdateEditor(codeEditor);
        if (_tabControl?.SelectedContent is not TabItemViewModel selectedTab) return;
        if (!selectedTab.FileName.EndsWith("*"))
        {
            selectedTab.FileName += "*";
        }
    }
    private void textEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        _completionWindow?.Close();
        var editor = GetCurrentTextEditor();
        if (editor != null)
            HideSymbolToolTip(editor);
    }
    private void Caret_PositionChanged(TextEditor textEditor)
    {
        _caretTextBlock.Text = string.Format("{0}:{1}",
            textEditor.TextArea.Caret.Line,
            textEditor.TextArea.Caret.Column);
    }
    private async void HandlePointerHover(TextCodeEditor codeEditor, PointerEventArgs e)
    {
        _hoverCancellation?.Cancel();
        hovered = true;
        TextEditor editor =  codeEditor.Content as TextEditor;
        var textView = editor.TextArea.TextView;
        var point = e.GetPosition(textView);

        var visualY = point.Y + textView.VerticalOffset;

        var visualLine = textView.GetVisualLineFromVisualTop(visualY);

        if (visualLine == null)
        {
            HideSymbolToolTip(editor);
            return;
        }

        var visualColumn = visualLine.GetVisualColumn(point, true);
        var relativeOffset = visualLine.GetRelativeOffset(visualColumn);
        var offset = visualLine.FirstDocumentLine.Offset + relativeOffset;

        var document =
            GetDocumentForEditor(editor);

        if (document == null)
            return;

        _hoverCancellation =
            new CancellationTokenSource();

        var token =
            _hoverCancellation.Token;

        try
        {
            await Task.Delay(250, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
            return;
    
        var symbol = await _symbolService.GetSymbolAtPosition(document, offset);

        var diagnostics =
            codeEditor._diagnosticRenderer?.CheckForDiagnosticAtOffset(offset);

        if (!hovered)
        {
            HideSymbolToolTip(editor);
            return;
        }

        if (symbol == null && (diagnostics == null || diagnostics.Count == 0))
        {
            HideSymbolToolTip(editor);
            return;
        }

        if (symbol != null)
        {
            if (_lastHoveredSymbol != null && !SymbolEqualityComparer.Default.Equals(symbol, _lastHoveredSymbol))
            {
                HideSymbolToolTip(editor);
                return;
            }

            _lastHoveredSymbol = symbol;
        }

        CreateSymbolTooltip(symbol, diagnostics, editor);
        e.Handled = true;
    }    
    protected override void OnClosed(EventArgs e)
    {
        _completionCancellation?.Cancel();
        _completionCancellation?.Dispose();

        _completionWindow?.Close();

        base.OnClosed(e);
    }
    
    // Helpers
    public TextEditor? GetCurrentTextEditor()
    {
        return ((_tabControl.SelectedContent as TabItemViewModel)?.Content as TextCodeEditor)?.Content as TextEditor;
    }
    public TextCodeEditor? GetCurrentTextCodeEditor()
    {
        return (_tabControl.SelectedContent as TabItemViewModel)?.Content as TextCodeEditor;
    }
    private Document? GetDocumentForEditor(TextEditor editor)
    {
        return documentMap.TryGetValue(editor, out var doc) ? doc : null;
    }

    #region Visuals
    // Updating
    public void UpdateVisuals(bool forceThemeReload = false)
    {
        bool themeChanged = false;
        if (_currentTheme != App.Settings.SelectedTheme ||  forceThemeReload)
        {
            themeChanged = true;
            _currentTheme = App.Settings.SelectedTheme;
        }
        foreach (var tab in viewModel.TabItems)
        {
            TextCodeEditor codeEditor = (tab.Content as TextCodeEditor);
            TextEditor editor = codeEditor.Content as TextEditor;
            Language csharpLanguage = codeEditor._registryOptions.GetLanguageByExtension(".cs");
            if (themeChanged)
            {
                App.ApplyTheme(codeEditor._textMateInstallation, _currentTheme);
                // codeEditor._textMateInstallation = editor.InstallTextMate(_registryOptions);
                ApplyThemeColorsToEditor(codeEditor._textMateInstallation);
            }
            editor.ShowLineNumbers = App.Settings.ShowLineNumbers;
            codeEditor._textMateInstallation.SetGrammar(codeEditor._registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
            _ = UpdateEditor(codeEditor);
            if (codeEditor._textMateInstallation != null) installation = codeEditor._textMateInstallation;
        }
        if (installation != null) App.Instance.ApplyThemeColorsToResources(installation);
    }
    private async Task UpdateEditor(TextCodeEditor codeEditor)
    {
        await UpdateFoldings(codeEditor);
        var editor = codeEditor.Content as TextEditor;
        if (!documentMap.TryGetValue(editor, out var document))
            return;

        document = document.WithText(SourceText.From(editor.Text));
        documentMap[editor] = document;
        
        await ScheduleDiagnostics(codeEditor, document);
    }
    
    #region Themes
    // Theme Events
    public void TextMateInstallationOnAppliedTheme(TextMate.Installation e)
    {
        ApplyThemeColorsToEditor(e);
        App.Instance.ApplyThemeColorsToResources(e);
    }
    
    // Theme Applying
    void ApplyThemeToTabEditor(TabItemViewModel tab, TextMate.Installation e)
    {
        TextEditor _editor = (tab.Content as TextCodeEditor).Content as TextEditor;
        var foldingMargin = _editor.TextArea.LeftMargins.OfType<FoldingMargin>().FirstOrDefault();

        App.ApplyBrushAction(e, "editor.background",brush =>
        {
            _editor.Background = brush;
            if (foldingMargin != null)
            {
                foldingMargin.FoldingMarkerBackgroundBrush = brush;
                foldingMargin.SelectedFoldingMarkerBackgroundBrush = brush;
            }
        });
        App.ApplyBrushAction(e, "editor.foreground",brush =>
        {
            _editor.Foreground = brush;
            if (foldingMargin != null) foldingMargin.SelectedFoldingMarkerBrush = brush;
        });

        App.ApplyBrushAction(e, "editor.selectionBackground", brush =>
        {
            _editor.TextArea.SelectionBrush = brush;
            if (foldingMargin != null) foldingMargin.FoldingMarkerBrush = brush;
        });

        if (!App.ApplyBrushAction(e, "editor.lineHighlightBackground",
                brush =>
                {
                    _editor.TextArea.TextView.CurrentLineBackground = brush;
                    _editor.TextArea.TextView.CurrentLineBorder = new Pen(brush); 
                }))
        {
            _editor.TextArea.TextView.SetDefaultHighlightLineColors();
        }

        if (!App.ApplyBrushAction(e, "editorLineNumber.foreground",
                brush => _editor.LineNumbersForeground = brush))
        {
            _editor.LineNumbersForeground = _editor.Foreground;
        }
    }
    void ApplyThemeColorsToEditor(TextMate.Installation e)
    {
        foreach (var tab in viewModel.TabItems)
        {
            ApplyThemeToTabEditor(tab, e);
        }
    }
    
    #endregion

    #region Symbols
    // Events
    private void textEditor_TextArea_PointerExited(PointerEventArgs e, TextEditor editor)
    {
        hovered = false;
        HideSymbolToolTip(editor);
        e.Handled = true;
    }
    
    // Helpers
    private void CreateSymbolTooltip(ISymbol? symbol, List<DiagnosticMarker>? diagnostics, TextEditor editor)
    {
        _symbolToolTip = null;
        var panel = new StackPanel()
        {
            Spacing = 6,
            MaxWidth = 1600
        };
        
        if (symbol != null)
        {
            var tooltipEditor = new TextEditor
            {
                Text = symbol.ToDisplayString(
                    SymbolDisplayFormat.VisualBasicErrorMessageFormat),
    
                IsReadOnly = true,
                ShowLineNumbers = false,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4,2,2,2),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                WordWrap = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
    
            var codeEditor = GetCurrentTextCodeEditor();
            var tooltipInstallation = tooltipEditor.InstallTextMate(new RegistryOptions(ThemeName.DarkPlus));
            App.ApplyTheme(tooltipInstallation, _currentTheme);
    
            var csharpLanguage = codeEditor._registryOptions.GetLanguageByExtension(".cs");
    
            tooltipInstallation.SetGrammar(
                codeEditor._registryOptions.GetScopeByLanguageId(
                    csharpLanguage.Id));
    
            var editorBorder = new Border
            {
                Child = tooltipEditor,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2),
                Padding = new Thickness(2),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            editorBorder.Bind(Border.BackgroundProperty, new DynamicResourceExtension("EditorBackgroundBrush"));
            
            panel.Children.Add(editorBorder);
        }
        
        if (diagnostics?.Count > 0)
        {
            if (symbol != null) panel.Children.Add(new Separator());
            foreach (var diagnostic in diagnostics)
            {
                panel.Children.Add(new Border
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    Padding = new Thickness(8,4,8,8),
                    Child = new TextBlock
                    {
                        Text = diagnostic.GetSimplifiedMessage(),
                        TextWrapping = TextWrapping.Wrap,
                    }
                });
            }
        }

        _symbolToolTip = new ToolTip
        {
            Content = panel,
            MaxWidth = 1000
        };
        
        _lastHoveredSymbol = symbol;
        ShowSymbolToolTip(editor);
        
    }
    private void ShowSymbolToolTip(TextEditor editor)
    {
        if (_symbolToolTip == null) return;
        ToolTip.SetTip(
            editor.TextArea.TextView,
            _symbolToolTip);

        ToolTip.SetIsOpen(
            editor.TextArea.TextView,
            true);
    }
    private void HideSymbolToolTip(TextEditor editor)
    {
        if (_symbolToolTip != null) ToolTip.SetIsOpen(editor.TextArea.TextView, false);
        _symbolToolTip = null;
        _lastHoveredSymbol = null;
    }
    #endregion

    #region Definitions
    public async Task GoToDefinition(TextEditor editor)
    {
        if (_navigatingToDefinition)
            return;

        _navigatingToDefinition = true;

        try
        {
            var document = GetDocumentForEditor(editor);

            if (document == null)
                return;

            var position = editor.CaretOffset;
            if (position == null) return;

            var symbol = await _symbolService.GetSymbolAtPosition(
                document,
                position);

            if (symbol == null)
                return;

            var location = symbol.Locations.FirstOrDefault(x => x.IsInSource);

            if (location == null)
            {
                await DecompileFromSymbol(symbol);
                return;
            }

            var targetDocument = document.Project.Solution.GetDocument(
                location.SourceTree);

            if (targetDocument == null)
                return;

            var targetPath = targetDocument.FilePath;

            if (string.IsNullOrEmpty(targetPath))
                return;

            var lineSpan = location.GetLineSpan();
            var line = lineSpan.StartLinePosition.Line + 1;
            var col = lineSpan.StartLinePosition.Character + 1;
            
            foreach (var tab in viewModel.TabItems)
            {
                if (tab.FilePath.Equals(targetPath))
                {
                    _tabControl.SelectedItem = tab;

                    Dispatcher.UIThread.Post(() =>
                    {
                        var targetEditor =
                            (tab.Content as TextCodeEditor)?.Content as TextEditor;

                        if (targetEditor != null)
                            MoveToPosition(targetEditor, line, col);
                    });

                    return;
                }
            }

            OpenFileAtLocation(targetPath, line, col);
        }
        finally
        {
            _navigatingToDefinition = false;
        }
    }
    private async Task DecompileFromSymbol(ISymbol symbol)
    {
        string dllPath = "";
        PEFile module = null;
        foreach (var reference in project.MetadataReferences)
        {
            var simpleDisplay = reference.Display.Replace(".dll", "").Split("\\")[^1];
            if (simpleDisplay.Equals(symbol.ContainingAssembly.Name))
            {
                dllPath = reference.Display;
                module = new PEFile(dllPath);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(dllPath)) return;
        
        var progressBar = new ProgressWindow();
        progressBar.Show();
        var progress = new Progress<int>(percent =>
        {
            progressBar.SetProgress(percent);
            if (percent >= 100)
            {
                progressBar.Close();
            }
        });
        
        string headType;
        if (symbol.Kind == SymbolKind.NamedType ||  symbol.Kind == SymbolKind.Namespace)
        {
            headType = symbol.MetadataName;
        }
        else
        {
            headType = symbol.ContainingSymbol.ToString()?.Split(".")[^1];
        }

        
        var metadata = module?.Metadata;
        TypeDefinition typeDef = new TypeDefinition();
        foreach (var def in metadata.TypeDefinitions.Select(handle => new { Handle = handle, Name = metadata.GetString(metadata.GetTypeDefinition(handle).Name) }))
        {
            if (headType.Equals(def.Name))
            {
                typeDef = metadata.GetTypeDefinition(def.Handle);
                break;
            }
        }

        if (typeDef.Equals(new TypeDefinition()))
        {
            new InfoWindow($"Error decompiling", InfoWindowType.Error, $"No matching type definition found: {headType}", identifier:"DFS-1").Show();
            ((IProgress<int>)progress).Report(100);
            return;
        }
        
        string name = metadata.GetString(typeDef.Name);
        var ns = metadata.GetString(typeDef.Namespace);
        var fullTypeName = new FullTypeName(string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}");
        var fullPath = TrySimplifyPath(dllPath) + ":" + (string.IsNullOrEmpty(ns) ? fullTypeName.Name : fullTypeName.FullName);
        
        CSharpDecompiler decompiler;
        try
        {
            var resolver = new UniversalAssemblyResolver(
                dllPath,
                false,
                null);

            resolver.AddSearchDirectory(scriptAssembliesPath);
            resolver.AddSearchDirectory(modAssembliesPath);
            foreach (string dir in Directory.GetDirectories(modAssembliesPath))
            {
                resolver.AddSearchDirectory(dir);
            }
            decompiler = new CSharpDecompiler(
                dllPath,
                resolver, new DecompilerSettings());

        }
        catch (Exception ex)
        {
            new InfoWindow($"Error decompiling", InfoWindowType.Error, $"{fullTypeName}\n{ex.Message}\n---TRACE---\n{ex.StackTrace}", identifier:"DFS-2").Show();
            return;
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                DecompiledTabData data;
                if (!decompiledMap.TryGetValue(fullPath, out data))
                {
                    var decompiledCode = decompiler.DecompileTypeAsString(fullTypeName);
                    AddNewFileToProject(name, SourceText.From(decompiledCode), fullPath);
                    data = new DecompiledTabData
                    {
                        Content = decompiledCode,
                        FullPath = fullPath,
                        Name = name,
                        Symbol = symbol,
                        Mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid)
                    };
                }
                
                bool found = false;
                TabItemViewModel tab = null;
                foreach (var tabItem in viewModel.TabItems)
                {
                    if (tabItem.FilePath.Equals(fullPath))
                    {
                        found = true;
                        tab = tabItem;
                        data = tab.DecompiledData;
                        _tabControl.SelectedItem = tabItem;
                        break;
                    }
                }
                if (!found) tab = AddNewDecompiledTab(data);
                
                var (line, col) =  FindSymbolPosition(data.Content, symbol);
                
                MoveToPosition((tab.Content as TextCodeEditor).Content as TextEditor, line+1,col+1);
            }
            catch (Exception ex)
            {
                new InfoWindow($"Error decompiling", InfoWindowType.Error, $"{fullTypeName}\n{ex.Message}\n---TRACE---\n{ex.StackTrace}", identifier:"DFS-3").Show();
            }

            ((IProgress<int>)progress).Report(100);
        });
    }
    private (int line, int column) FindSymbolPosition(string source, ISymbol symbol)
    {
        string searchText;

        switch (symbol)
        {
            case IMethodSymbol method:
                searchText = method.MethodKind == MethodKind.Constructor
                    ? method.ContainingType.Name + "("
                    : method.MetadataName + "(";
                break;

            case IPropertySymbol property:
                searchText = property.MetadataName;
                break;

            case IFieldSymbol field:
                searchText = field.MetadataName;
                break;

            case IEventSymbol @event:
                searchText = @event.MetadataName;
                break;

            case INamedTypeSymbol type:
                searchText = type.MetadataName;
                break;

            default:
                searchText = symbol.MetadataName;
                break;
        }

        var index = source.IndexOf(
            searchText,
            StringComparison.Ordinal);

        if (index < 0)
            return (1, 1);

        var line = 0;
        var lineStart = 0;

        for (var i = 0; i < index; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, index - lineStart);
    }
    private void OpenFileAtLocation(string filePath, int line, int column)
    {
        AddNewTab(filePath);
        var editor = GetCurrentTextEditor();
        MoveToPosition(editor, line, column);
    }
    private void MoveToPosition(TextEditor editor, int line, int column)
    {
        Dispatcher.UIThread.Post(() =>
        {
            editor.Focusable = true;
            editor.Focus();

            editor.CaretOffset = editor.Document.GetOffset(line, column);
            editor.TextArea.Caret.BringCaretToView();
        });
    }

    private string TrySimplifyPath(string path)
    {
        return path.Replace(App.Settings.SteamDirectory, "Isle Goblin");
    }
    #endregion
    
    #region Folding, Diagnostics, And Comments
    private async Task UpdateFoldings(TextCodeEditor codeEditor)
    {
        if (documentMap.TryGetValue(codeEditor.Content as TextEditor, out var doc))
        {
            _ = new CSharpFoldingStrategy().UpdateFoldings(codeEditor, doc);
        };
    }
    
    private async Task UpdateDiagnostics(Document document, TextCodeEditor codeEditor)
    {
        if (document == null) 
            return;

        var compilation = await document.Project.GetCompilationAsync();

        if (compilation == null)
            return;

        var diagnostics = compilation.GetDiagnostics();

        var markers = new List<DiagnosticMarker>();

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Location == Location.None)
                continue;

            if (!diagnostic.Location.IsInSource)
                continue;

            var sourceTree = diagnostic.Location.SourceTree;

            if (sourceTree == null)
                continue;

            if (sourceTree.FilePath != document.FilePath)
                continue;

            var span = diagnostic.Location.SourceSpan;

            if (span.Length == 0)
                continue;

            IBrush brush = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => Brushes.Red,
                DiagnosticSeverity.Warning => Brushes.Orange,
                DiagnosticSeverity.Info => Brushes.DodgerBlue,
                _ => Brushes.Gray
            };

            markers.Add(new DiagnosticMarker(
                span.Start,
                span.End,
                diagnostic.GetMessage(),
                diagnostic.Severity,
                brush));
        }

        codeEditor._diagnosticRenderer?.SetDiagnostics(markers);
    }
    private async Task ScheduleDiagnostics(TextCodeEditor codeEditor, Document document)
    {
        _diagnosticCancellation?.Cancel();
        _diagnosticCancellation?.Dispose();

        _diagnosticCancellation =
            new CancellationTokenSource();

        var token =
            _diagnosticCancellation.Token;

        try
        {
            await Task.Delay(1000, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
            return;

        await UpdateDiagnostics(document, codeEditor);
    }

    public void ToggleFolding(bool isFolded)
    {
        var codeEditor = GetCurrentTextCodeEditor();
        foreach (var folding in codeEditor._foldingManager.AllFoldings)
        {
            folding.IsFolded = isFolded;
        }
    }
    
    public void Comment()
    {
        var editor = GetCurrentTextEditor();
        var lineNum = editor.TextArea.Caret.Line;
        var line = editor.Document.GetLineByNumber(lineNum);
        
        var text = editor.Document.GetText(line.Offset, line.Length);
        var index = 0;

        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }
        
        editor.Document.Insert(line.Offset + index, "// ");
    }
    public void Uncomment()
    {
        var editor = GetCurrentTextEditor();
        var line = editor.Document.GetLineByNumber(editor.TextArea.Caret.Line);
        var text = editor.Document.GetText(line.Offset, line.Length);

        var index = GetCommentOffset(text);

        if (!text.AsSpan(index).StartsWith("//"))
            return;

        var length = text.AsSpan(index).StartsWith("// ") ? 3 : 2;

        editor.Document.Remove(line.Offset + index, length);
    }
    
    private static int GetCommentOffset(string text)
    {
        var index = 0;

        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;

        return index;
    }
    #endregion
    
    #region Autocompletion
    // Events
    private void textEditor_TextArea_TextEntering(TextInputEventArgs e, TextEditor editor)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;

        if (_completionWindow != null)
        {
            var c = e.Text[0];

            if (c == '\t' ||
                c == '\n' ||
                c == '\r' ||
                c == '(' ||
                c == '[' ||
                c == ';' ||
                c == ',')
            {
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
        
        if (e.Text.Length == 1)
        {
            var c = e.Text[0];

            if (c == ')' ||
                c == ']' ||
                c == '}')
            {
                if (HandleClosingCharacter(editor, c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        _insightWindow?.Hide();
    }
    private void textEditor_KeyDown(KeyEventArgs e, TextEditor editor)
    {
        if (_completionWindow == null)
            return;
        if (e.Key == Key.Back)
        {
            var caret = editor.CaretOffset;

            if (caret > 0 && caret < editor.Text.Length)
            {
                var previous = editor.Text[caret - 1];
                var next = editor.Text[caret];

                bool matchingPair =
                    (previous == '(' && next == ')') ||
                    (previous == '[' && next == ']') ||
                    (previous == '{' && next == '}') ||
                    (previous == '"' && next == '"') ||
                    (previous == '\'' && next == '\'');

                if (matchingPair)
                {
                    HandleAutoCloseDeletion(editor);
                    e.Handled = true;
                    return;
                }
            }
        }
        else if (e.Key == Key.Tab)
        {
            _completionWindow.CompletionList.RequestInsertion(
                new KeyEventArgs
                {
                    RoutedEvent = KeyDownEvent,
                    Key = Key.Tab
                });

            e.Handled = true;
        }
    }
    private async void textEditor_TextArea_TextEntered(TextInputEventArgs e, TextEditor textEditor)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;
        
        HandleAutoClosing(textEditor, e.Text);

        if (_completionWindow != null)
        {
            FilterCompletionWindow(textEditor);
            return;
        }

        var document = GetDocumentForEditor(textEditor);

        if (document == null)
            return;

        if (!ShouldTriggerCompletion(textEditor))
            return;

        var position = textEditor.CaretOffset;

        _completionCancellation?.Cancel();
        _completionCancellation?.Dispose();

        _completionCancellation = new CancellationTokenSource();

        var requestId = ++_completionRequestId;

        document = document.WithText(
            SourceText.From(textEditor.Text));

        var completionService =
            CompletionService.GetService(document);

        if (completionService == null)
            return;

        Microsoft.CodeAnalysis.Completion.CompletionList? completions;
        try
        {
            completions =
                await completionService.GetCompletionsAsync(
                    document,
                    position);
        }
        catch
        {
            return;
        }

        if (_completionCancellation.IsCancellationRequested)
            return;

        if (requestId != _completionRequestId)
            return;

        if (completions == null ||
            completions.ItemsList.Count == 0)
        {
            return;
        }

        try
        {
            
            ShowCompletionWindow(
                textEditor,
                completions);
        }
        catch (Exception exception)
        {
            throw;
        }
    }
    
    // Completion Window
    private void ShowCompletionWindow(TextEditor textEditor, Microsoft.CodeAnalysis.Completion.CompletionList completions)
    {
        try
        {
            _completionWindow?.Close();
    
            var startOffset = GetCompletionStart(textEditor);
    
            _completionWindow = new CompletionWindow(textEditor.TextArea);
    
            _completionWindow.Closed += (_, _) =>
            {
                _completionWindow = null;
            };
    
            var data = _completionWindow.CompletionList.CompletionData;
    
            foreach (var item in completions.ItemsList)
            {
                data.Add(new CompletionData(
                    item.DisplayText,
                    item.DisplayText,
                    startOffset));
            }
    
            if (data.Count == 0)
            {
                _completionWindow.Close();
                return;
            }
    
            _completionWindow.CompletionList.TabIndex = 0;
    
            _completionWindow.Show();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private bool ShouldTriggerCompletion(TextEditor editor)
    {
        if (editor.CaretOffset <= 0)
            return false;

        char character = editor.Text[editor.CaretOffset - 1];

        return char.IsLetterOrDigit(character) ||
               character == '_' ||
               character == '.';
    }

    // Filtering
    private async void FilterCompletionWindow(TextEditor editor)
    {
        _filterCancellation?.Cancel();
        _filterCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        _filterCancellation = cancellation;

        try
        {
            await Task.Delay(30, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellation.IsCancellationRequested)
            return;

        if (_completionWindow == null)
            return;

        SelectCompletion(editor);
    }
    private void SelectCompletion(TextEditor editor)
    {
        if (_completionWindow == null)
            return;

        var prefix = GetCompletionPrefix(editor);

        _completionWindow.CompletionList.SelectItem(prefix);
    }
    private string GetCompletionPrefix(TextEditor editor)
    {
        var start = GetCompletionStart(editor);
        var length = editor.CaretOffset - start;

        if (length <= 0)
            return string.Empty;

        return editor.Text.Substring(start, length);
    }
    
    // Helpers
    private int GetCompletionStart(TextEditor editor)
    {
        var text = editor.Text;
        var position = editor.CaretOffset;

        var start = position;

        while (start > 0)
        {
            var c = text[start - 1];

            if (!char.IsLetterOrDigit(c) && c != '_')
                break;

            start--;
        }

        return start;
    }
    
    
    // Auto Closing
    private void HandleAutoCloseDeletion(TextEditor editor)
    {
        var caret = editor.CaretOffset;

        if (caret <= 0 || caret >= editor.Text.Length)
            return;

        var previous = editor.Text[caret - 1];
        var next = editor.Text[caret];

        bool matchingPair =
            (previous == '(' && next == ')') ||
            (previous == '[' && next == ']') ||
            (previous == '{' && next == '}') ||
            (previous == '"' && next == '"') ||
            (previous == '\'' && next == '\'');

        if (!matchingPair)
            return;

        editor.Document.Remove(caret - 1, 2);

        editor.CaretOffset = caret - 1;
    }
    private bool HandleClosingCharacter(TextEditor editor, char character)
    {
        if (editor.CaretOffset >= editor.Text.Length)
            return false;

        if (editor.Text[editor.CaretOffset] != character)
            return false;

        editor.CaretOffset++;

        return true;
    }
    private void HandleAutoClosing(TextEditor editor, string text)
    {
        if (text.Length != 1)
            return;

        char opening = text[0];

        char? closing = opening switch
        {
            '(' => ')',
            '[' => ']',
            '{' => '}',
            '"' => '"',
            '\'' => '\'',
            _ => null
        };

        if (closing == null)
            return;

        var caret = editor.CaretOffset;

        if (opening is '"' or '\'')
        {
            if (caret < editor.Text.Length &&
                editor.Text[caret] == opening)
            {
                editor.CaretOffset = caret + 1;
                return;
            }
        }

        editor.Document.Insert(caret, closing.Value.ToString());

        editor.CaretOffset = caret;
    }
    #endregion
    
    #endregion
    
    #endregion

    #region Chords
    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt)
            return;

        var currentGesture = new KeyGesture(e.Key, e.KeyModifiers);

        if (_activePrefix != null)
        {
            var match = _chordBindings.FirstOrDefault(b => 
                b.FirstGesture.Equals(_activePrefix) && b.SecondGesture.Equals(currentGesture));

            if (match != null)
            {
                match.Action.Invoke();
                e.Handled = true;
            }

            ResetChordState();
            return;
        }
        
        var single = _chordBindings.FirstOrDefault(b => b.FirstGesture.Equals(currentGesture) && b.SecondGesture == null);
        if (single != null && _activePrefix == null)
        {
            single.Action.Invoke();
            e.Handled = true;
            ResetChordState();
            return;
        }

        var isPrefix = _chordBindings.Any(b => b.FirstGesture.Equals(currentGesture));
        if (isPrefix)
        {
            _activePrefix = currentGesture;
            _chordTimer?.Start();
            _onStatusChanged?.Invoke($"({currentGesture}) was pressed. Waiting for second key chord...");
            e.Handled = true; 
        }
    }

    private void ResetChordState()
    {
        _chordTimer?.Stop();
        _activePrefix = null;
        _onStatusChanged?.Invoke(string.Empty);
    }
    #endregion
}

public class CompletionData : ICompletionData
{
    private readonly string _text;
    private readonly string _description;
    private readonly int _startOffset;
    private ICompletionData _completionDataImplementation;

    public CompletionData(string text, string description, int startOffset)
    {
        _text = text;
        _description = description;
        _startOffset = startOffset;
    }

    public object Content => _text;

    public object? Description => _description;

    public double Priority => 0;

    public string Text => _text;

    public string FilterText => _text;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var length = textArea.Caret.Offset - _startOffset;

        if (length < 0)
            return;

        textArea.Document.Replace(
            _startOffset,
            length,
            _text);
    }

    public IImage Image => _completionDataImplementation.Image;
}

public class TextCodeEditor : UserControl
{
    public TextMate.Installation _textMateInstallation;
    public FoldingManager? _foldingManager;
    public CSharpDiagnosticRenderer? _diagnosticRenderer;
    public RegistryOptions _registryOptions;
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
            ConvertTabsToSpaces = true
        };
        Content = new TextEditor
        {
            ShowLineNumbers = App.Settings.ShowLineNumbers,
            FontSize = 14,
            FontFamily = new FontFamily("Cascadia Code"),
            SyntaxHighlighting = syntax,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Options = options,
            Document = new TextDocument(code),
        };
        
    }
    public TextCodeEditor(DecompiledTabData data)
    {
        IHighlightingDefinition syntax = HighlightingManager.Instance.GetDefinition("C#");
        TextEditorOptions options = new TextEditorOptions
        {
            HighlightCurrentLine = true,
            EnableHyperlinks = true,
            CutCopyWholeLine = true,
            AllowToggleOverstrikeMode = true,
            ShowBoxForControlCharacters = true,
            ConvertTabsToSpaces = true
        };
        Content = new TextEditor
        {
            ShowLineNumbers = App.Settings.ShowLineNumbers,
            FontSize = 14,
            FontFamily = new FontFamily("Cascadia Code"),
            SyntaxHighlighting = syntax,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Options = options,
            Document = new TextDocument(data.Content)
        };
    }
}

public class DecompiledTabData
{
    public string Name;

    public string Content;

    public string FullPath;
    
    public ISymbol Symbol;
    public Guid Mvid;

    public override string ToString() => Name;
}

public class ChordedKeyBind
{
    public KeyGesture FirstGesture { get; set; } 
    public KeyGesture? SecondGesture { get; set; } 
    public Action Action { get; set; }

    public ChordedKeyBind(string first, string? second, Action action)
    {
        FirstGesture = KeyGesture.Parse(first);
        if (!string.IsNullOrEmpty(second))
        {
            SecondGesture = KeyGesture.Parse(second);
        }
        else
        {
            SecondGesture = null;
        }
        Action = action;
    }
}