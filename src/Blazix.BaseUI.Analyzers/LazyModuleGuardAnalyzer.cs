using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags accesses to <c>moduleTask.Value</c> that are not guarded by an
/// <c>IsValueCreated</c> check, which is required to avoid forcing evaluation
/// of the lazy module during disposal when it was never used.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LazyModuleGuardAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0013";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "moduleTask.Value accessed without IsValueCreated guard",
        "moduleTask.Value accessed without IsValueCreated guard",
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Accessing moduleTask.Value without first checking IsValueCreated " +
            "forces the Lazy<T> to evaluate, which can trigger an unnecessary " +
            "JS module import during disposal if the module was never used.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (!AnalyzerHelpers.ShouldAnalyze(memberAccess))
            return;

        if (!AnalyzerHelpers.IsRazorFile(memberAccess))
            return;

        if (memberAccess.Name.Identifier.Text != "Value")
            return;

        if (!IsModuleTaskExpression(memberAccess.Expression))
            return;

        // Only enforce the guard inside Dispose/DisposeAsync — intentional
        // first-use in OnAfterRenderAsync is expected to trigger evaluation.
        if (!IsInsideDisposeMethod(memberAccess))
            return;

        if (HasEnclosingIsValueCreatedGuard(memberAccess) ||
            HasPrecedingEarlyReturnGuard(memberAccess, "IsValueCreated"))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            memberAccess.GetLocation()));
    }

    private static bool IsModuleTaskExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifier:
                return identifier.Identifier.Text.Contains("moduleTask");

            case PostfixUnaryExpressionSyntax postfix:
                if (postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                    return IsModuleTaskExpression(postfix.Operand);
                return false;

            default:
                return false;
        }
    }

    private static bool IsInsideDisposeMethod(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax method)
            {
                var name = method.Identifier.Text;
                return name == "Dispose" || name == "DisposeAsync";
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool HasEnclosingIsValueCreatedGuard(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax)
                break;

            if (current is IfStatementSyntax ifStmt)
            {
                var conditionText = ifStmt.Condition.ToString();
                if (conditionText.Contains("IsValueCreated"))
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool HasPrecedingEarlyReturnGuard(SyntaxNode node, string guardText)
    {
        // Find the enclosing statement that contains this node
        var enclosingStatement = node.Parent;
        while (enclosingStatement is not null && enclosingStatement is not StatementSyntax)
            enclosingStatement = enclosingStatement.Parent;

        if (enclosingStatement?.Parent is not BlockSyntax block)
            return false;

        // Scan preceding sibling statements for if (!...guardText...) { return; }
        foreach (var statement in block.Statements)
        {
            if (statement.SpanStart >= enclosingStatement.SpanStart)
                break;

            if (statement is IfStatementSyntax ifStmt &&
                ifStmt.Condition.ToString().Contains(guardText))
            {
                if (ContainsReturn(ifStmt.Statement))
                    return true;
            }
        }

        return false;
    }

    private static bool ContainsReturn(StatementSyntax statement)
    {
        if (statement is ReturnStatementSyntax)
            return true;

        if (statement is BlockSyntax block)
        {
            foreach (var s in block.Statements)
            {
                if (s is ReturnStatementSyntax)
                    return true;
            }
        }

        return false;
    }
}
