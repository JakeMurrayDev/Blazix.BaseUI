using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags classes that inherit from a base type but have an empty body (<c>{ }</c>),
/// suggesting the use of semicolon syntax (<c>;</c>) instead per project convention.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyClassBodyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0007";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Empty class body should use semicolon syntax",
        "Class '{0}' has an empty body \u2014 use semicolon syntax instead",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Classes that derive from a base type but contain no members should use " +
            "semicolon syntax (e.g., public sealed class Foo : Bar;) instead of empty braces.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        if (AnalyzerHelpers.IsGeneratedFile(classDecl.SyntaxTree))
            return;

        var filePath = classDecl.SyntaxTree.FilePath;
        if (filePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            return;

        if (classDecl.BaseList == null)
            return;

        if (classDecl.Members.Count != 0)
            return;

        if (!classDecl.SemicolonToken.IsMissing)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            classDecl.Identifier.GetLocation(),
            classDecl.Identifier.Text));
    }
}
