using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags accesses to <c>Element.Value</c> that are not guarded by an
/// <c>Element.HasValue</c> check, which is required to avoid
/// <see cref="System.InvalidOperationException"/> when the element reference
/// has not yet been captured.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ElementHasValueGuardAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0012";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Element.Value accessed without HasValue guard",
        "Element.Value accessed without Element.HasValue guard",
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Element is a nullable ElementReference. Accessing .Value without " +
            "first checking .HasValue can throw an InvalidOperationException " +
            "before the element reference has been captured by Blazor.");

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

        if (!(memberAccess.Expression is IdentifierNameSyntax identifier))
            return;

        if (identifier.Identifier.Text != "Element")
            return;

        if (HasEnclosingHasValueGuard(memberAccess) ||
            HasShortCircuitGuard(memberAccess) ||
            HasPrecedingEarlyReturnGuard(memberAccess))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            memberAccess.GetLocation()));
    }

    private static bool HasEnclosingHasValueGuard(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax)
                break;

            if (current is IfStatementSyntax ifStmt)
            {
                var conditionText = ifStmt.Condition.ToString();
                if (conditionText.Contains("Element.HasValue"))
                    return true;
            }

            // Conditional expression guard: `Element.HasValue ? Element.Value : ...`
            if (current is ConditionalExpressionSyntax cond)
            {
                if (cond.Condition.ToString().Contains("Element.HasValue"))
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Recognizes short-circuit guards of the form
    /// <c>Element.HasValue &amp;&amp; (... Element.Value ...)</c>.
    /// The C# compiler short-circuits the right operand of <c>&amp;&amp;</c> when
    /// the left operand is false, so <c>Element.Value</c> on the right is safe.
    /// </summary>
    private static bool HasShortCircuitGuard(SyntaxNode node)
    {
        var current = node;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax or LambdaExpressionSyntax
                or AnonymousMethodExpressionSyntax or LocalFunctionStatementSyntax)
                break;

            if (current.Parent is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.LogicalAndExpression) &&
                binary.Right == current)
            {
                if (LeftHasHasValueCheck(binary.Left))
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool LeftHasHasValueCheck(ExpressionSyntax left)
    {
        // Walk left-associatively: `A && B && Element.Value` parses as
        // `(A && B) && Element.Value`. Check the rightmost operand of any
        // chained && first, then fall back to the text of the whole subtree.
        var leftText = left.ToString();
        return leftText.Contains("Element.HasValue");
    }

    private static bool HasPrecedingEarlyReturnGuard(SyntaxNode node)
    {
        // Walk up through all enclosing blocks to the method level,
        // because the Element.Value access may be nested inside a try block
        // while the guard is in an outer block (e.g., the method body).
        var current = node;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax)
                break;

            if (current is BlockSyntax block)
            {
                // Find the child statement that contains our node
                StatementSyntax? containingStatement = null;
                foreach (var stmt in block.Statements)
                {
                    if (stmt.Span.Contains(node.Span))
                    {
                        containingStatement = stmt;
                        break;
                    }
                }

                if (containingStatement is not null)
                {
                    foreach (var statement in block.Statements)
                    {
                        if (statement.SpanStart >= containingStatement.SpanStart)
                            break;

                        if (statement is IfStatementSyntax ifStmt &&
                            ifStmt.Condition.ToString().Contains("Element.HasValue"))
                        {
                            if (ContainsReturn(ifStmt.Statement))
                                return true;
                        }
                    }
                }
            }

            current = current.Parent;
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
