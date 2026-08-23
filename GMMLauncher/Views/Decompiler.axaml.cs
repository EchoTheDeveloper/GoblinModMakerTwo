using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation.CSharp;
using GMMBackend;
using GMMLauncher.ViewModels;

namespace GMMLauncher.Views
{
    public partial class Decompiler : Window
    {
        private TextEditor? decompiledCode { get; }
        
        public Decompiler(CodeEditor codeEditor)
        {
            DataContext = new DecompilerViewModel(this);
            InitializeComponent();

            Title = "Decompiler";
            (DataContext as DecompilerViewModel)?.LoadAssembly(this, false);

            decompiledCode = this.FindControl<TextEditor>("DecompiledCode");
            if (decompiledCode != null)
            {

                 TextMate.Installation _textMateInstallation = decompiledCode.InstallTextMate(new RegistryOptions(ThemeName.DarkPlus));
                decompiledCode.ShowLineNumbers = App.Settings.ShowLineNumbers;
                decompiledCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                App.ApplyTheme(_textMateInstallation, App.Settings.SelectedTheme);
                _textMateInstallation.AppliedTheme += (_, installation) => codeEditor.TextMateInstallationOnAppliedTheme(installation);
                decompiledCode.TextArea.IndentationStrategy = new CSharpIndentationStrategy(decompiledCode.Options);
                decompiledCode.TextArea.LeftMargins.Add(new CustomMargin());
                Language csharpLanguage = new RegistryOptions(ThemeName.DarkPlus).GetLanguageByExtension(".cs");
                _textMateInstallation.SetGrammar(new RegistryOptions(ThemeName.DarkPlus).GetScopeByLanguageId(csharpLanguage.Id));
            }
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            WindowManager.Add(this);
        }
    }
}