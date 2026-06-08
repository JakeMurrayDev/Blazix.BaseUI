using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags methods that are declared as <c>async void</c>, which should use
/// <c>async Task</c> or <c>async ValueTask</c> instead to ensure exceptions
/// are observable and callers can await completion.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoAsyncVoidAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0002";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Async void method detected",
        "Method '{0}' is async void \u2014 use async Task or async ValueTask",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Async void methods cannot be awaited and swallow exceptions. " +
            "Use async Task or async ValueTask instead.");

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

        bool hasAsync = false;
        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                hasAsync = true;
                break;
            }
        }

        if (!hasAsync)
            return;

        var returnType = method.ReturnType;
        if (returnType is PredefinedTypeSyntax predefined &&
            predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                method.Identifier.GetLocation(),
                method.Identifier.Text));
        }
    }
}
