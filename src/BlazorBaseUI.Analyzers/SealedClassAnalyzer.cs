using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorBaseUI.Analyzers;

/// <summary>
/// Flags non-sealed, non-abstract classes that are never inherited from within
/// the compilation, enforcing the convention that classes should be sealed by
/// default to communicate intent and enable compiler optimizations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SealedClassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0011";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Type should be sealed",
        "Type '{0}' should be sealed",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Internal classes that are not inherited from should be declared as sealed " +
            "to communicate intent and enable compiler optimizations. Public types are " +
            "intentionally exempt because sealing them is a binary-breaking change for " +
            "downstream consumers.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var candidates = new ConcurrentBag<INamedTypeSymbol>();
            var baseTypeNames = new ConcurrentBag<string>();

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var symbol = (INamedTypeSymbol)symbolContext.Symbol;

                if (symbol.BaseType is not null)
                {
                    baseTypeNames.Add(symbol.BaseType.ToDisplayString());

                    if (symbol.BaseType.IsGenericType)
                    {
                        baseTypeNames.Add(symbol.BaseType.OriginalDefinition.ToDisplayString());
                    }
                }

                if (symbol.TypeKind != TypeKind.Class)
                    return;

                if (symbol.IsSealed || symbol.IsAbstract || symbol.IsStatic)
                    return;

                // Public types are exempt: sealing them is a binary-breaking change
                // for downstream consumers. Only internal types are enforced.
                if (symbol.DeclaredAccessibility != Accessibility.Internal)
                    return;

                // Skip partial stubs with semicolon token (e.g., `public partial class Foo;`)
                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                {
                    var syntaxNode = syntaxRef.GetSyntax();
                    if (syntaxNode is ClassDeclarationSyntax classDecl &&
                        classDecl.SemicolonToken != default)
                    {
                        return;
                    }
                }

                // Skip generated files
                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                {
                    if (syntaxRef.SyntaxTree.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                candidates.Add(symbol);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                var inherited = new System.Collections.Generic.HashSet<string>(baseTypeNames);

                foreach (var candidate in candidates)
                {
                    if (inherited.Contains(candidate.ToDisplayString()))
                        continue;

                    var location = candidate.Locations.FirstOrDefault();
                    if (location is not null)
                    {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            Rule,
                            location,
                            candidate.Name));
                    }
                }
            });
        });
    }
}
