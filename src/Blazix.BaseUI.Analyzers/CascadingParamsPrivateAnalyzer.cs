using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags <c>[CascadingParameter]</c> properties that are not declared as
/// <c>private</c>, enforcing the convention that cascading parameters should
/// not be exposed as part of a component's public API.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CascadingParamsPrivateAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0010";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "[CascadingParameter] should be private",
        "[CascadingParameter] property '{0}' should be private",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Properties decorated with [CascadingParameter] should be declared as private " +
            "to avoid exposing internal component dependencies as public API.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var prop = (PropertyDeclarationSyntax)context.Node;

        if (!AnalyzerHelpers.ShouldAnalyze(prop))
            return;

        var symbol = context.SemanticModel.GetDeclaredSymbol(prop);
        if (symbol is null)
            return;

        bool hasCascadingParameter = symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() ==
            "Microsoft.AspNetCore.Components.CascadingParameterAttribute");

        if (!hasCascadingParameter)
            return;

        if (symbol.DeclaredAccessibility != Accessibility.Private)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                prop.Identifier.GetLocation(),
                prop.Identifier.Text));
        }
    }
}
