using System.Collections.Immutable;
using Blazix.BaseUI.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Shouldly;

namespace Blazix.BaseUI.Analyzers.Tests;

public sealed class ConventionAnalyzerTests
{
    [Fact]
    public async Task FixedCascadingValue_FlagsContextReplacement()
    {
        const string razor = """
            <CascadingValue Value="context" IsFixed="true">
                @ChildContent
            </CascadingValue>
            @code {
                private Context context = new();
                private void Disable()
                {
                    context = new Context { Disabled = true };
                }
            }
            """;

        var diagnostics = await AnalyzeAdditionalFileAsync(new FixedCascadingValueReassignmentAnalyzer(), razor);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == FixedCascadingValueReassignmentAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task FixedCascadingValue_AllowsDirectPropertyChanges()
    {
        const string razor = """
            <CascadingValue Value="context" IsFixed="true">
                @ChildContent
            </CascadingValue>
            @code {
                private Context context = new();
                private void Disable() => context.Disabled = true;
            }
            """;

        var diagnostics = await AnalyzeAdditionalFileAsync(new FixedCascadingValueReassignmentAnalyzer(), razor);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == FixedCascadingValueReassignmentAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task FixedCascadingValue_AllowsOneTimeLifecycleInitialization()
    {
        const string razor = """
            <CascadingValue Value="context" IsFixed="true">@ChildContent</CascadingValue>
            @code {
                private Context context = null!;
                protected override void OnInitialized()
                {
                    context = new Context();
                }
            }
            """;

        var diagnostics = await AnalyzeAdditionalFileAsync(new FixedCascadingValueReassignmentAnalyzer(), razor);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == FixedCascadingValueReassignmentAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task FixedCascadingValue_IgnoresCommentedReplacement()
    {
        const string razor = """
            <CascadingValue Value="context" IsFixed="true">@ChildContent</CascadingValue>
            @code {
                private Context context = new();
                // context = new Context();
            }
            """;

        var diagnostics = await AnalyzeAdditionalFileAsync(new FixedCascadingValueReassignmentAnalyzer(), razor);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == FixedCascadingValueReassignmentAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task CastedCascadingValue_RequiresTValue()
    {
        const string razor = "<CascadingValue Value=\"@((IGroupContext)context)\">@ChildContent</CascadingValue>";

        var diagnostics = await AnalyzeAdditionalFileAsync(new CastedCascadingValueAnalyzer(), razor);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CastedCascadingValueAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task TypedCascadingValue_AllowsInterfaceTValue()
    {
        const string razor = "<CascadingValue TValue=\"IGroupContext\" Value=\"context\">@ChildContent</CascadingValue>";

        var diagnostics = await AnalyzeAdditionalFileAsync(new CastedCascadingValueAnalyzer(), razor);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == CastedCascadingValueAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task TypedTrigger_RequiresPayloadFreeCounterpart()
    {
        const string source = "namespace Components; public class TooltipTypedTrigger<TValue> { }";

        var diagnostics = await AnalyzeSourceAsync(new TypedTriggerFallbackAnalyzer(), source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == TypedTriggerFallbackAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task TypedTrigger_AllowsExistingPayloadFreeCounterpart()
    {
        const string source = "namespace Components; public class TooltipTypedTrigger<TValue> { } public class TooltipTrigger { }";

        var diagnostics = await AnalyzeSourceAsync(new TypedTriggerFallbackAnalyzer(), source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == TypedTriggerFallbackAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task UtilityObjectParameter_FlagsSingleConcreteCast()
    {
        const string source = "public static class ValueUtilities { public static int Parse(object value) { return (int)value + 1; } }";

        var diagnostics = await AnalyzeSourceAsync(new AvoidableObjectBoxingAnalyzer(), source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == AvoidableObjectBoxingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task UtilityObjectParameter_ExemptsAttributeDictionaries()
    {
        const string source = "using System.Collections.Generic; public static class AttributeUtilities { public static int Add(Dictionary<string, object> attrs, object value) { attrs[\"value\"] = value; return (int)value; } }";

        var diagnostics = await AnalyzeSourceAsync(new AvoidableObjectBoxingAnalyzer(), source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AvoidableObjectBoxingAnalyzer.DiagnosticId);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAdditionalFileAsync(DiagnosticAnalyzer analyzer, string razor) =>
        AnalyzeAsync(analyzer, "public sealed class Fixture { }", new InMemoryAdditionalText("Fixture.razor", razor));

    private static Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(DiagnosticAnalyzer analyzer, string source) =>
        AnalyzeAsync(analyzer, source);

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        params AdditionalText[] additionalFiles)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "AnalyzerFixture",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var options = new AnalyzerOptions(additionalFiles.ToImmutableArray());

        return await compilation.WithAnalyzers(ImmutableArray.Create(analyzer), options).GetAnalyzerDiagnosticsAsync();
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }
}
