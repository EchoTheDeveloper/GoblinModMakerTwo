using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using GMMLauncher.Views;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

public class CSharpFoldingStrategy
{
    public async Task UpdateFoldings(TextCodeEditor codeEditor, Document document)
    {
        if (codeEditor._foldingManager == null)
            return;

        if (codeEditor.Content is not TextEditor editor)
            return;

        if (document == null) return;

        var text = editor.Text;

        if (string.IsNullOrEmpty(text))
            return;

        document = document.WithText(SourceText.From(text));

        var root = await document.GetSyntaxRootAsync();

        if (root == null)
            return;

        var foldings = new List<NewFolding>();

        foreach (var node in root.DescendantNodes())
        {
            var start = GetFoldingStart(node);
            var end = GetFoldingEnd(node);

            if (start < 0 || end <= start || end > text.Length)
                continue;
            
            foldings.Add(new NewFolding(start, end)
            {
                Name = "{...}"
            });
        }

        codeEditor._foldingManager.UpdateFoldings(
            foldings
                .OrderBy(x => x.StartOffset)
                .ToList(), -1);
    }
    private static int GetFoldingStart(SyntaxNode node)
    {
        return node switch
        {
            BaseTypeDeclarationSyntax type =>
                type.Identifier.Span.End,

            NamespaceDeclarationSyntax ns =>
                ns.Name.Span.End,

            FileScopedNamespaceDeclarationSyntax ns =>
                ns.Name.FullSpan.End,

            MethodDeclarationSyntax method =>
                method.ParameterList.CloseParenToken.Span.End,

            ConstructorDeclarationSyntax constructor =>
                constructor.Identifier.Span.End,

            DestructorDeclarationSyntax destructor =>
                destructor.Identifier.Span.End,

            LocalFunctionStatementSyntax local =>
                local.Identifier.Span.End,

            PropertyDeclarationSyntax property =>
                property.Identifier.Span.End,

            AccessorDeclarationSyntax accessor =>
                accessor.Keyword.Span.End,

            IfStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            ElseClauseSyntax statement when statement.Statement is not IfStatementSyntax =>
                statement.ElseKeyword.Span.End,

            ForStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            ForEachStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            ForEachVariableStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            WhileStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            DoStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            SwitchStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            TryStatementSyntax statement =>
                statement.TryKeyword.Span.End,

            CatchClauseSyntax statement =>
                statement.CatchKeyword.Span.End,

            FinallyClauseSyntax statement =>
                statement.FinallyKeyword.Span.End,

            UsingStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            LockStatementSyntax statement =>
                statement.CloseParenToken.Span.End,

            CheckedStatementSyntax statement =>
                statement.Keyword.Span.End,

            UnsafeStatementSyntax statement =>
                statement.UnsafeKeyword.Span.End,

            _ => -1
        };
    }

    private static int GetFoldingEnd(SyntaxNode node)
    {
        return node switch
        {
            IfStatementSyntax statement =>
                GetStatementEnd(statement.Statement),

            ElseClauseSyntax statement =>
                GetStatementEnd(statement.Statement),

            TryStatementSyntax statement =>
                statement.Block.CloseBraceToken.Span.End,

            CatchClauseSyntax statement =>
                statement.Block.CloseBraceToken.Span.End,

            FinallyClauseSyntax statement =>
                statement.Block.CloseBraceToken.Span.End,

            _ => GetLastCloseBrace(node)
        };
    }

    private static int GetStatementEnd(StatementSyntax statement)
    {
        return statement switch
        {
            BlockSyntax block =>
                block.CloseBraceToken.Span.End,

            _ =>
                statement.Span.End
        };
    }

    private static int GetLastCloseBrace(SyntaxNode node)
    {
        var closeBrace = node.DescendantTokens()
            .LastOrDefault(x => x.IsKind(SyntaxKind.CloseBraceToken));

        return closeBrace == default
            ? -1
            : closeBrace.Span.End;
    }
}