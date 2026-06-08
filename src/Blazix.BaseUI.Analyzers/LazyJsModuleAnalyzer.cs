using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags <c>IJSObjectReference</c> fields that are not wrapped in
/// <c>Lazy&lt;Task&lt;IJSObjectReference&gt;&gt;</c>, enforcing the lazy module pattern
/// required for correct JS interop lifecycle management in Blazor components.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LazyJsModuleAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0003";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "IJSObjectReference field not wrapped in Lazy<Task<>>",
        "IJSObjectReference field '{0}' should be wrapped in Lazy<Task<IJSObjectReference>>",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "IJSObjectReference fields should be wrapped in Lazy<Task<IJSObjectReference>> " +
            "to ensure correct lazy initialization and lifecycle management.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var field = (FieldDeclarationSyntax)context.Node;

        if (!AnalyzerHelpers.ShouldAnalyze(field))
            return;

        if (!AnalyzerHelpers.IsRazorFile(field))
            return;

        var typeText = field.Declaration.Type.ToString();

        if (!typeText.Contains("IJSObjectReference"))
            return;

        if (typeText.Contains("Lazy<Task<IJSObjectReference>>"))
            return;

        // Skip lines that are local variable assignments from InvokeAsync
        if (field.ToString().Contains("InvokeAsync<IJSObjectReference>"))
            return;

        var variableName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "field";

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            field.GetLocation(),
            variableName));
    }
}
