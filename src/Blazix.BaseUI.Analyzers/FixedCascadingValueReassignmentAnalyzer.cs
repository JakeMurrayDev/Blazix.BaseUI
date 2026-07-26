using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Flags replacement of a context instance supplied by a fixed CascadingValue.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FixedCascadingValueReassignmentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0016";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Fixed cascading context is replaced",
        "Fixed cascading context '{0}' is replaced; update its properties instead",
        "Blazor",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A CascadingValue with IsFixed=true promises that its Value reference will not change.");

    private static readonly Regex TagPattern = new Regex(
        @"<CascadingValue\b[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex FixedPattern = new Regex(
        @"\bIsFixed\s*=\s*""true""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ValuePattern = new Regex(
        @"\bValue\s*=\s*""@?(?<name>[A-Za-z_][A-Za-z0-9_]*)""",
        RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterAdditionalFileAction(AnalyzeFile);
    }

    private static void AnalyzeFile(AdditionalFileAnalysisContext context)
    {
        if (!context.AdditionalFile.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            return;

        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null)
            return;

        var content = text.ToString();
        foreach (Match tag in TagPattern.Matches(content))
        {
            if (!FixedPattern.IsMatch(tag.Value))
                continue;

            var value = ValuePattern.Match(tag.Value);
            if (!value.Success)
                continue;

            var name = value.Groups["name"].Value;
            var assignments = Regex.Matches(
                content,
                @"(?m)^[\t ]*(?:(?:private|protected|internal|public|static|readonly)\s+)*(?:[A-Za-z_][A-Za-z0-9_<>,\.\?\[\]]*\s+)?(?<target>(?:this\.)?" + Regex.Escape(name) + @")\s*=\s*new\b");
            if (assignments.Count < 2)
                continue;

            var reassignment = assignments[1].Groups["target"];

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                CreateLocation(context.AdditionalFile.Path, text, reassignment.Index, name.Length),
                name));
        }
    }

    private static Location CreateLocation(string path, SourceText text, int start, int length)
    {
        var span = new TextSpan(start, length);
        return Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
    }
}
