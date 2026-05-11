using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace BlazorBaseUI.Analyzers;

/// <summary>
/// Flags Blazor component lifecycle method overrides that are missing an
/// <c>&lt;inheritdoc /&gt;</c> XML documentation comment, enforcing consistent
/// documentation across all lifecycle hooks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LifecycleInheritdocAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0015";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Lifecycle override missing <inheritdoc />",
        "Lifecycle override '{0}' should have /// <inheritdoc /> documentation",
        "Documentation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Blazor component lifecycle method overrides should have " +
            "/// <inheritdoc /> XML documentation for consistent documentation.");

    private static readonly ImmutableHashSet<string> LifecycleMethodNames = ImmutableHashSet.Create(
        "OnInitialized",
        "OnInitializedAsync",
        "OnParametersSet",
        "OnParametersSetAsync",
        "OnAfterRender",
        "OnAfterRenderAsync",
        "SetParametersAsync",
        "Dispose",
        "DisposeAsync");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        if (!AnalyzerHelpers.ShouldAnalyze(method))
            return;

        var methodName = method.Identifier.Text;

        if (!LifecycleMethodNames.Contains(methodName))
            return;

        bool hasOverride = false;
        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.OverrideKeyword))
            {
                hasOverride = true;
                break;
            }
        }

        if (!hasOverride)
            return;

        // Verify the containing type inherits from ComponentBase
        var containingType = context.SemanticModel.GetDeclaredSymbol(method)?.ContainingType;
        if (containingType is null || !AnalyzerHelpers.InheritsFromComponentBase(containingType))
            return;

        // Check leading trivia for /// <inheritdoc />
        if (HasInheritdocComment(method))
            return;

        // Razor compiler strips XML doc comments from generated code.
        // Fall back to reading the original .razor source via AdditionalFiles.
        if (HasInheritdocInMappedSource(method, context))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            method.Identifier.GetLocation(),
            methodName));
    }

    private static bool HasInheritdocComment(MethodDeclarationSyntax method)
    {
        // Check leading trivia (works for most cases). A custom <summary> block
        // is treated as authored documentation that intentionally overrides
        // <inheritdoc/>, so we accept either marker.
        foreach (var trivia in method.GetLeadingTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                var fullString = trivia.ToFullString();
                if (fullString.Contains("inheritdoc") || fullString.Contains("<summary>"))
                    return true;
            }
        }

        // Fallback: scan source lines before the method for /// <inheritdoc or
        // a <summary> block. The scan caps at 10 lines (covers most attribute
        // stacks and multi-line XML doc comments without sweeping into the
        // previous member).
        var sourceText = method.SyntaxTree.GetText();
        var methodLine = sourceText.Lines.GetLineFromPosition(method.SpanStart).LineNumber;
        var linesToScan = System.Math.Min(10, methodLine);
        for (int i = 1; i <= linesToScan; i++)
        {
            var lineText = sourceText.Lines[methodLine - i].ToString();
            if (lineText.Contains("/// <inheritdoc") || lineText.Contains("<summary>"))
                return true;
        }

        return false;
    }

    private static bool HasInheritdocInMappedSource(
        MethodDeclarationSyntax method,
        SyntaxNodeAnalysisContext context)
    {
        var location = method.GetLocation();
        var mappedSpan = location.GetMappedLineSpan();

        if (!mappedSpan.HasMappedPath ||
            !mappedSpan.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            return false;

        var razorPath = mappedSpan.Path;
        var mappedLine = mappedSpan.StartLinePosition.Line; // 0-based

        // Find the .razor file in AdditionalFiles (registered via Directory.Build.props)
        SourceText? razorText = null;
        foreach (var file in context.Options.AdditionalFiles)
        {
            if (file.Path.Equals(razorPath, StringComparison.OrdinalIgnoreCase))
            {
                razorText = file.GetText(context.CancellationToken);
                break;
            }
        }

        if (razorText is null)
            return false;

        // Scan source lines before the mapped line for /// <inheritdoc or a
        // <summary> block. Capped at 10 lines to cover multi-line XML doc
        // comments and attribute stacks without sweeping into the previous
        // member declaration.
        var linesToScan = Math.Min(10, mappedLine);
        for (int i = 1; i <= linesToScan; i++)
        {
            var lineText = razorText.Lines[mappedLine - i].ToString();
            if (lineText.Contains("/// <inheritdoc") || lineText.Contains("<summary>"))
                return true;
        }

        return false;
    }
}
