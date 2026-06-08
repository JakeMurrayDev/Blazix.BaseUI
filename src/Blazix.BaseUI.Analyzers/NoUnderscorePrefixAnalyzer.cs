using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags fields that use an underscore prefix (e.g., <c>_myField</c>), which
/// violates the project naming convention of not using underscore prefixes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoUnderscorePrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0014";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Underscore prefix on field name",
        "Field '{0}' uses underscore prefix \u2014 project convention is no underscore",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Fields should not use an underscore prefix per project convention. " +
            "Use camelCase without a leading underscore.");

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

        foreach (var variable in field.Declaration.Variables)
        {
            var name = variable.Identifier.Text;

            // Exclude discard pattern ("_") — only flag "_" followed by a letter
            if (name.Length < 2)
                continue;

            if (name[0] == '_' && char.IsLetter(name[1]))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    variable.Identifier.GetLocation(),
                    name));
            }
        }
    }
}
