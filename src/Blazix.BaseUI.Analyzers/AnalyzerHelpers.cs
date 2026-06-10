using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blazix.BaseUI.Analyzers;

internal static class AnalyzerHelpers
{
    internal static bool IsMappedToUserCode(SyntaxNode node)
    {
        var location = node.GetLocation();
        if (location.SourceTree is null)
            return false;

        var treePath = location.SourceTree.FilePath;
        var mappedPath = location.GetMappedLineSpan().Path;

        return !string.Equals(treePath, mappedPath, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsGeneratedFile(SyntaxTree tree)
    {
        return tree.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsRazorFile(SyntaxNode node)
    {
        var mappedPath = node.GetLocation().GetMappedLineSpan().Path;
        return mappedPath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool InheritsFromComponentBase(INamedTypeSymbol typeSymbol)
    {
        var baseType = typeSymbol.BaseType;
        while (baseType is not null)
        {
            if (baseType.ToDisplayString() == "Microsoft.AspNetCore.Components.ComponentBase")
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    internal static bool ShouldAnalyze(SyntaxNode node)
    {
        return !IsGeneratedFile(node.SyntaxTree) || IsMappedToUserCode(node);
    }
}
