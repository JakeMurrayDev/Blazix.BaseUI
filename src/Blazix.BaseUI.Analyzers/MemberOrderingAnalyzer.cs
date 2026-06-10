using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Enforces the member ordering convention for Blazor components as defined in AGENTS.md:
/// 1. Constants, 2. Fields, 3. Private properties, 4. Parameter properties,
/// 5. Public properties, 6. Internal properties, 7. Lifecycle methods,
/// 8. Dispose, 9. Public methods, 10. Internal methods, 11. Private methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MemberOrderingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Member ordering violation",
        "'{0}' ({1}) should not appear after '{2}' ({3})",
        "Ordering",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Blazor component members must follow the ordering defined in AGENTS.md: " +
            "Constants, Fields, Private properties, Parameter properties, Public properties, " +
            "Internal properties, Lifecycle methods, Dispose, Public methods, Internal methods, " +
            "Private methods.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    private static readonly ImmutableHashSet<string> LifecycleMethodNames = ImmutableHashSet.Create(
        "OnInitialized", "OnInitializedAsync",
        "OnParametersSet", "OnParametersSetAsync",
        "OnAfterRender", "OnAfterRenderAsync",
        "SetParametersAsync", "ShouldRender");

    private static readonly ImmutableHashSet<string> DisposeMethodNames = ImmutableHashSet.Create(
        "Dispose", "DisposeAsync");

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

        if (classDecl.Members.Count < 2)
            return;

        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (typeSymbol is null || !AnalyzerHelpers.InheritsFromComponentBase(typeSymbol))
            return;

        bool isGeneratedFile = AnalyzerHelpers.IsGeneratedFile(classDecl.SyntaxTree);

        int highestCategory = 0;
        string highestCategoryMemberName = "";

        foreach (var member in classDecl.Members)
        {
            if (isGeneratedFile && !AnalyzerHelpers.IsMappedToUserCode(member))
                continue;

            int category = GetCategory(member, context.SemanticModel);
            if (category == 0)
                continue;

            string memberName = GetMemberName(member);

            if (category < highestCategory)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    member.GetLocation(),
                    memberName,
                    GetCategoryName(category),
                    highestCategoryMemberName,
                    GetCategoryName(highestCategory)));
            }
            else
            {
                highestCategory = category;
                highestCategoryMemberName = memberName;
            }
        }
    }

    private static int GetCategory(MemberDeclarationSyntax member, SemanticModel model)
    {
        switch (member)
        {
            case FieldDeclarationSyntax field:
                return field.Modifiers.Any(SyntaxKind.ConstKeyword) ? 1 : 2;
            case PropertyDeclarationSyntax prop:
                return GetPropertyCategory(prop, model);
            case MethodDeclarationSyntax method:
                return GetMethodCategory(method);
            default:
                return 0;
        }
    }

    private static int GetPropertyCategory(PropertyDeclarationSyntax prop, SemanticModel model)
    {
        var symbol = model.GetDeclaredSymbol(prop);
        if (symbol is not null)
        {
            bool hasParameter = symbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.ToDisplayString() ==
                "Microsoft.AspNetCore.Components.ParameterAttribute");
            if (hasParameter)
                return 4;
        }

        if (prop.Modifiers.Any(SyntaxKind.PublicKeyword))
            return 5;
        if (prop.Modifiers.Any(SyntaxKind.InternalKeyword))
            return 6;

        return 3;
    }

    private static int GetMethodCategory(MethodDeclarationSyntax method)
    {
        string name = method.Identifier.Text;

        // BuildRenderTree is always generated by the Razor compiler — skip it
        if (name == "BuildRenderTree")
            return 0;

        if (method.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
            LifecycleMethodNames.Contains(name))
            return 7;

        if (DisposeMethodNames.Contains(name))
            return 8;

        if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
            return 9;
        if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
            return 10;

        return 11;
    }

    private static string GetMemberName(MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case FieldDeclarationSyntax field:
                return field.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "field";
            case PropertyDeclarationSyntax prop:
                return prop.Identifier.Text;
            case MethodDeclarationSyntax method:
                return method.Identifier.Text;
            default:
                return "member";
        }
    }

    private static string GetCategoryName(int category)
    {
        switch (category)
        {
            case 1: return "Constants";
            case 2: return "Fields";
            case 3: return "Private properties";
            case 4: return "Parameter properties";
            case 5: return "Public properties";
            case 6: return "Internal properties";
            case 7: return "Lifecycle methods";
            case 8: return "Dispose";
            case 9: return "Public methods";
            case 10: return "Internal methods";
            case 11: return "Private methods";
            default: return "Unknown";
        }
    }
}
