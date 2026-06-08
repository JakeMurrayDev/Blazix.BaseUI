using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Honors the legacy <c>// lint-ignore:RULE-NN</c> suppression comments
/// authored when the lint stack was a bash script. When a diagnostic from
/// one of the BBUI rules is reported and the same line (or the immediately
/// preceding line) carries the matching <c>lint-ignore:RULE-NN</c> marker,
/// the diagnostic is suppressed via <see cref="SuppressionDescriptor"/>.
/// </summary>
/// <remarks>
/// Mapping is by trailing two digits: <c>BBUI0009</c> ↔ <c>RULE-09</c>.
/// HTML/Razor comments inside markup (<c>@* ... *@</c> outside <c>@code</c>
/// blocks) are not visible here; they are handled by the bash rules where
/// they apply (currently only RULE-05).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LintIgnoreSuppressor : DiagnosticSuppressor
{
    private static readonly ImmutableDictionary<string, SuppressionDescriptor> SuppressionMap = BuildMap();

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        BuildMap().Values.ToImmutableArray();

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (!SuppressionMap.TryGetValue(diagnostic.Id, out var descriptor))
                continue;

            var location = diagnostic.Location;
            var tree = location.SourceTree;
            if (tree is null)
                continue;

            var sourceText = tree.GetText(context.CancellationToken);
            var lineSpan = location.GetLineSpan();
            var lineNumber = lineSpan.StartLinePosition.Line;

            // RULE-NN derived from the trailing two digits of the diagnostic id.
            // BBUI0009 → "RULE-09".
            var ruleNumber = diagnostic.Id.Substring(diagnostic.Id.Length - 2);
            var marker = "lint-ignore:RULE-" + ruleNumber;

            if (LineContains(sourceText, lineNumber, marker) ||
                LineContains(sourceText, lineNumber - 1, marker))
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    private static bool LineContains(SourceText text, int lineNumber, string marker)
    {
        if (lineNumber < 0 || lineNumber >= text.Lines.Count)
            return false;

        var line = text.Lines[lineNumber];
        return line.ToString().IndexOf(marker, StringComparison.Ordinal) >= 0;
    }

    private static ImmutableDictionary<string, SuppressionDescriptor> BuildMap()
    {
        // Source-of-truth: every BBUI diagnostic id paired with its
        // // lint-ignore:RULE-NN marker. Update both columns when adding a new
        // analyzer. SuppressionId convention: BBUIxxxxS.
        // BBUI0001 (MemberOrderingAnalyzer) deliberately omitted: the bash RULE-01
        // is "no logic in stubs" (handled in lint-rules.sh) and shares the marker
        // text. Leaving BBUI0001 unsuppressed avoids cross-talk; if the analyzer
        // is ever enabled, callers should use `#pragma warning disable BBUI0001`.
        var entries = new (string DiagnosticId, string RuleNumber)[]
        {
            ("BBUI0002", "02"),
            ("BBUI0003", "03"),
            ("BBUI0007", "07"),
            ("BBUI0009", "09"),
            ("BBUI0010", "10"),
            ("BBUI0011", "11"),
            ("BBUI0012", "12"),
            ("BBUI0013", "13"),
            ("BBUI0014", "14"),
            ("BBUI0015", "15"),
        };

        var builder = ImmutableDictionary.CreateBuilder<string, SuppressionDescriptor>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var justification = "Suppressed via // lint-ignore:RULE-" + entry.RuleNumber + " comment.";
            builder.Add(
                entry.DiagnosticId,
                new SuppressionDescriptor(
                    id: entry.DiagnosticId + "S",
                    suppressedDiagnosticId: entry.DiagnosticId,
                    justification: justification));
        }

        return builder.ToImmutable();
    }
}
