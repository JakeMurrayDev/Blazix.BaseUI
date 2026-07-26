using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Blazix.BaseUI.Analyzers;

/// <summary>
/// Recommends CascadingValue's typed TValue parameter instead of an inline cast.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CastedCascadingValueAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BBUI0017";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Use a typed CascadingValue",
        "Use TValue=\"{0}\" Value=\"{1}\" instead of casting the cascading value",
        "Blazor",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "CascadingValue exposes TValue specifically for supplying a value through an interface type.");

    private static readonly Regex TagPattern = new Regex(
        @"<CascadingValue\b[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex CastPattern = new Regex(
        @"\bValue\s*=\s*""@\(\((?<type>[A-Za-z_][A-Za-z0-9_\.]*)\)(?<name>[A-Za-z_][A-Za-z0-9_]*)\)""",
        RegexOptions.Compiled);

    private static readonly Regex TypePattern = new Regex(
        @"\bTValue\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            if (TypePattern.IsMatch(tag.Value))
                continue;

            var cast = CastPattern.Match(tag.Value);
            if (!cast.Success)
                continue;

            var type = cast.Groups["type"].Value;
            var name = cast.Groups["name"].Value;
            var span = new TextSpan(tag.Index + cast.Index, cast.Length);
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                Location.Create(context.AdditionalFile.Path, span, text.Lines.GetLinePositionSpan(span)),
                type,
                name));
        }
    }
}
