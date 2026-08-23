using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

public class CSharpSymbolService
{
    public async Task<ISymbol?> GetSymbolAtPosition(Document document, int position)
    {
        var root = await document.GetSyntaxRootAsync();

        if (root == null)
            return null;

        var token = root.FindToken(position);

        if (token.Parent == null)
            return null;

        var semanticModel = await document.GetSemanticModelAsync();

        if (semanticModel == null)
            return null;
        
        var symbolInfo = semanticModel.GetSymbolInfo(token.Parent);

        return symbolInfo.Symbol ?? semanticModel.GetDeclaredSymbol(token.Parent);
    }
}