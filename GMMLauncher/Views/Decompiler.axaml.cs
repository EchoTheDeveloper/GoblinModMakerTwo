using System.IO;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation.CSharp;
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
            
            (DataContext as DecompilerViewModel)?.LoadAssembly(this, false);

            decompiledCode = this.FindControl<TextEditor>("DecompiledCode");
            if (decompiledCode != null)
            {
                TextMate.Installation _textMateInstallation;
                decompiledCode.ShowLineNumbers = App.Settings.ShowLineNumbers;
                decompiledCode.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                _textMateInstallation = decompiledCode.InstallTextMate(codeEditor._registryOptions);
                _textMateInstallation.AppliedTheme += (o, installation) => codeEditor.TextMateInstallationOnAppliedTheme(o, installation, decompiledCode);
                decompiledCode.TextArea.IndentationStrategy = new CSharpIndentationStrategy(decompiledCode.Options);
                decompiledCode.TextArea.LeftMargins.Insert(0, codeEditor._margin);
                Language csharpLanguage = codeEditor._registryOptions.GetLanguageByExtension(".cs");
                _textMateInstallation.SetGrammar(codeEditor._registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
            }
        }
    }
}