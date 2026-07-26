using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags utility parameters typed as object when every use casts to one concrete type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidableObjectBoxingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0019";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Utility parameter can be strongly typed",
        "Utility parameter '{0}' is always cast to '{1}'; use that type instead of object",
        "API Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Utility methods should preserve type information when an object parameter is only used as one concrete type.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (method.Parent is not ClassDeclarationSyntax containingClass ||
            !containingClass.Identifier.Text.EndsWith("Utilities", System.StringComparison.Ordinal) ||
            method.Body is null || HasAttributeDictionary(method, context))
        {
            return;
        }

        foreach (var parameter in method.ParameterList.Parameters)
        {
            var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken);
            if (parameterSymbol?.Type.SpecialType != SpecialType.System_Object)
                continue;

            var references = method.Body.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => SymbolEqualityComparer.Default.Equals(
                    context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol,
                    parameterSymbol))
                .ToArray();
            if (references.Length == 0 || references.Any(reference => reference.Parent is not CastExpressionSyntax))
                continue;

            var castTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var reference in references)
            {
                var cast = (CastExpressionSyntax)reference.Parent!;
                var castType = context.SemanticModel.GetTypeInfo(cast.Type, context.CancellationToken).Type;
                if (castType is null || castType.SpecialType == SpecialType.System_Object)
                    continue;
                castTypes.Add(castType);
            }

            if (castTypes.Count == 1)
            {
                var castType = castTypes.Single().ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Type!.GetLocation(), parameter.Identifier.ValueText, castType));
            }
        }
    }

    private static bool HasAttributeDictionary(MethodDeclarationSyntax method, SyntaxNodeAnalysisContext context)
    {
        var dictionaryType = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
        if (dictionaryType is null)
            return false;

        return method.ParameterList.Parameters.Any(parameter =>
        {
            var parameterType = context.SemanticModel.GetTypeInfo(parameter.Type!, context.CancellationToken).Type as INamedTypeSymbol;
            return parameterType is not null &&
                SymbolEqualityComparer.Default.Equals(parameterType.OriginalDefinition, dictionaryType) &&
                parameterType.TypeArguments[0].SpecialType == SpecialType.System_String &&
                parameterType.TypeArguments[1].SpecialType == SpecialType.System_Object;
        });
    }
}
