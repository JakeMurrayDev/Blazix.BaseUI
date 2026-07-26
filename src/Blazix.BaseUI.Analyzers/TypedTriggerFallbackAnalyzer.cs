using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Requires a payload-free trigger component beside each generic TypedTrigger component.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedTriggerFallbackAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0018";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Typed trigger needs a payload-free counterpart",
        "Generic trigger '{0}' needs a non-generic '{1}' counterpart for payload-free use",
        "API Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Payload-free trigger consumers should not need to specify TValue=object.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || type.Arity != 1 ||
            type.DeclaredAccessibility != Accessibility.Public ||
            !type.Name.EndsWith("TypedTrigger", System.StringComparison.Ordinal))
        {
            return;
        }

        var fallbackName = type.Name.Substring(0, type.Name.Length - "TypedTrigger".Length) + "Trigger";
        var hasFallback = type.ContainingNamespace.GetTypeMembers(fallbackName)
            .Any(candidate => candidate.Arity == 0 && candidate.DeclaredAccessibility == Accessibility.Public);
        if (hasFallback)
            return;

        var location = type.Locations.FirstOrDefault(candidate => candidate.IsInSource);
        if (location is not null)
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name, fallbackName));
    }
}
