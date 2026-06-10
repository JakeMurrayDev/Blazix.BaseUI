using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags JS interop calls (<c>InvokeVoidAsync</c> / <c>InvokeAsync</c>) that are not
/// wrapped in a try-catch for <c>JSDisconnectedException</c>, which is required for
/// circuit-safe Blazor Server operation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CircuitSafeJsGuardAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0009";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "JS interop call missing circuit-safe guard",
        "JS interop call '{0}' is not inside a try-catch for JSDisconnectedException or TaskCanceledException",
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "All JS interop calls must be wrapped in a try-catch that handles " +
            "JSDisconnectedException or TaskCanceledException to avoid unhandled exceptions when the circuit disconnects.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (!AnalyzerHelpers.ShouldAnalyze(invocation))
            return;

        if (!AnalyzerHelpers.IsRazorFile(invocation))
            return;

        if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "InvokeVoidAsync" && methodName != "InvokeAsync")
            return;

        if (IsModuleImport(memberAccess))
            return;

        if (IsStateHasChangedFirstArg(invocation))
            return;

        if (!IsJsInteropCall(context, invocation))
            return;

        if (!HasEnclosingJsDisconnectedCatch(invocation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                memberAccess.Name.GetLocation(),
                methodName));
        }
    }

    private static bool IsJsInteropCall(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol is null && symbolInfo.CandidateSymbols.Length > 0)
            methodSymbol = symbolInfo.CandidateSymbols[0] as IMethodSymbol;

        if (methodSymbol is null)
            return false;

        var ns = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        return ns is not null && ns.StartsWith("Microsoft.JSInterop");
    }

    private static bool IsModuleImport(MemberAccessExpressionSyntax memberAccess)
    {
        if (memberAccess.Name is GenericNameSyntax genericName)
        {
            foreach (var typeArg in genericName.TypeArgumentList.Arguments)
            {
                if (typeArg.ToString().Contains("IJSObjectReference"))
                    return true;
            }
        }

        return false;
    }

    private static bool IsStateHasChangedFirstArg(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count > 0)
        {
            var firstArgText = args[0].Expression.ToString();
            if (firstArgText == "StateHasChanged")
                return true;
        }

        return false;
    }

    private static bool HasEnclosingJsDisconnectedCatch(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax)
                break;

            // Lambda/anonymous method boundaries: the lambda may execute outside
            // any enclosing try-catch, so stop walking up the tree.
            if (current is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or LocalFunctionStatementSyntax)
                break;

            if (current is TryStatementSyntax tryStmt)
            {
                // Only treat as guarded when the invocation is inside the try block itself,
                // not inside the catch or finally blocks of the same try statement.
                if (!tryStmt.Block.Span.Contains(node.Span))
                {
                    current = current.Parent;
                    continue;
                }

                foreach (var catchClause in tryStmt.Catches)
                {
                    if (catchClause.Declaration is not null)
                    {
                        var typeText = catchClause.Declaration.Type.ToString();
                        if (typeText.Contains("JSDisconnectedException") || typeText.Contains("TaskCanceledException"))
                            return true;
                    }

                    if (catchClause.Filter is not null)
                    {
                        var filterText = catchClause.Filter.FilterExpression.ToString();
                        if (filterText.Contains("JSDisconnectedException") || filterText.Contains("TaskCanceledException"))
                            return true;
                    }
                }
            }

            current = current.Parent;
        }

        return false;
    }
}
