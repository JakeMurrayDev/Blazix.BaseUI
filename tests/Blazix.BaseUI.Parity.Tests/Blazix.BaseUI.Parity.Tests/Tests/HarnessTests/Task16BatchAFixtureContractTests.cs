using System.Text.RegularExpressions;
using Blazix.BaseUI.Parity.Tests.Client;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the Task 16 batch A Blazor fixtures to their exact upstream Tailwind demos
/// and published manifest contracts.
/// </summary>
public sealed partial class Task16BatchAFixtureContractTests
{
    private static readonly FixtureSource[] Fixtures =
    [
        new("avatar/hero", "Avatar/Hero", "avatar/demos/hero/tailwind/index.tsx"),
        new("separator/hero", "Separator/Hero", "separator/demos/hero/tailwind/index.tsx"),
        new("progress/hero", "Progress/Hero", "progress/demos/hero/tailwind/index.tsx"),
        new("meter/hero", "Meter/Hero", "meter/demos/hero/tailwind/index.tsx"),
        new("accordion/multiple", "Accordion/Multiple", "accordion/demos/multiple/tailwind/index.tsx"),
        new("tabs/hero", "Tabs/Hero", "tabs/demos/hero/tailwind/index.tsx"),
        new("toolbar/hero", "Toolbar/Hero", "toolbar/demos/hero/tailwind/index.tsx"),
        new("checkbox/hero", "Checkbox/Hero", "checkbox/demos/hero/tailwind/index.tsx")
    ];

    [Fact]
    public void RegistersEveryBatchAFixtureWithTheExpectedType()
    {
        foreach (var fixture in Fixtures)
        {
            FixtureRegistry.Ids.ShouldContain(fixture.Id);
            FixtureRegistry.Resolve(fixture.Id.Split('/')[0], fixture.Id.Split('/')[1])?.FullName
                .ShouldBe(
                    $"Blazix.BaseUI.Parity.Tests.Client.Fixtures.{fixture.Blazor.Replace('/', '.')}");
        }
    }

    [Fact]
    public void PreservesEveryUpstreamTailwindClassMultiset()
    {
        foreach (var fixture in Fixtures)
        {
            var react = File.ReadAllText(ReactSourcePath(fixture));
            var razor = File.ReadAllText(RazorSourcePath(fixture));

            ExtractReactClasses(react).ShouldBe(
                ExtractRazorClasses(razor),
                $"Tailwind class drift for {fixture.Id}");
        }
    }

    [Fact]
    public void PinsDemoSpecificStructureAndContent()
    {
        AssertRazorContains("Avatar/Hero", "<AvatarFallback Delay=\"600\"", ">LT<");
        AssertRazorContains(
            "Separator/Hero",
            "Orientation=\"Orientation.Vertical\"",
            ">Home<", ">Pricing<", ">Blog<", ">Support<", ">Log in<", ">Sign up<");
        AssertRazorContains(
            "Progress/Hero",
            "<ProgressRoot Value=\"20\"",
            ">Export data<",
            "<ProgressValue",
            "<ProgressIndicator");
        AssertRazorContains(
            "Meter/Hero",
            "<MeterRoot Value=\"24\"",
            ">Storage Used<",
            "<MeterValue",
            "<MeterIndicator");
        AssertRazorContains(
            "Accordion/Multiple",
            "Multiple=\"true\"",
            "What is Base UI?",
            "How do I get started?",
            "Can I use it for my project?",
            "M1.5 8h13M8 14.5v-13");
        AssertRazorContains(
            "Tabs/Hero",
            "DefaultValue=\"@overview\"",
            "Value=\"@overview\"",
            "Value=\"@projects\"",
            "Value=\"@account\"",
            "Workspace stats and activity.",
            "Milestones and deadlines.",
            "Profile and preferences.");
        AssertRazorContains(
            "Toolbar/Hero",
            "aria-label=\"Alignment\"",
            "Align Left",
            "Align Right",
            "aria-label=\"Numerical format\"",
            "Helvetica",
            "Arial",
            "Edited 51m ago");
        AssertRazorContains(
            "Checkbox/Hero",
            "DefaultChecked=\"true\"",
            "Enable notifications",
            "m2.5 8.5 4 4 7-9");
    }

    [Fact]
    public void MatchesTheExactPublishedManifestEntries()
    {
        Task16ManifestContract.AssertExactEntries(
        [
            new("avatar/hero", "avatar/demos/hero/tailwind/index.tsx", "Avatar/Hero",
                "initial|render|"),
            new("separator/hero", "separator/demos/hero/tailwind/index.tsx", "Separator/Hero",
                "initial|render|"),
            new("progress/hero", "progress/demos/hero/tailwind/index.tsx", "Progress/Hero",
                "initial|render|"),
            new("meter/hero", "meter/demos/hero/tailwind/index.tsx", "Meter/Hero",
                "initial|render|"),
            new("accordion/multiple", "accordion/demos/multiple/tailwind/index.tsx", "Accordion/Multiple",
                "initial|render|",
                "open-first|animation|click:[data-parity-root] > div > div:nth-child(1) > h3 > button=>[data-parity-root] > div > div:nth-child(1) > h3 > button[attribute:aria-expanded=true],[data-parity-root] > div > div:nth-child(1) > [role='region'][state:visible]",
                "open-second|animation|click:[data-parity-root] > div > div:nth-child(2) > h3 > button=>[data-parity-root] > div > div:nth-child(2) > h3 > button[attribute:aria-expanded=true],[data-parity-root] > div > div:nth-child(2) > [role='region'][state:visible],[data-parity-root] > div > div:nth-child(1) > h3 > button[attribute:aria-expanded=true]",
                "close-first|animation|click:[data-parity-root] > div > div:nth-child(1) > h3 > button=>[data-parity-root] > div > div:nth-child(1) > h3 > button[attribute:aria-expanded=false],[data-parity-root] > div > div:nth-child(1) > [role='region'][state:detached],[data-parity-root] > div > div:nth-child(2) > h3 > button[attribute:aria-expanded=true]"),
            new("tabs/hero", "tabs/demos/hero/tailwind/index.tsx", "Tabs/Hero",
                "initial|render|",
                "activate-projects|animation|click:[role='tab']:nth-of-type(2)=>[role='tab']:nth-of-type(2)[attribute:aria-selected=true],[role='tabpanel'][property:innerText=Milestones and deadlines.]",
                "focus-account|render|key:ArrowRight=>[role='tab']:nth-of-type(3)[focus:equals]",
                "activate-account|animation|key:Enter=>[role='tab']:nth-of-type(3)[attribute:aria-selected=true],[role='tabpanel'][property:innerText=Profile and preferences.]"),
            new("toolbar/hero", "toolbar/demos/hero/tailwind/index.tsx", "Toolbar/Hero",
                "initial|render|",
                "press-align-left|render|click:[aria-label='Align left']=>[aria-label='Align left'][attribute:aria-pressed=true]",
                "focus-align-right|render|key:ArrowRight=>[aria-label='Align right'][focus:equals]",
                "press-align-right|render|key:Enter=>[aria-label='Align right'][attribute:aria-pressed=true],[aria-label='Align left'][attribute:aria-pressed=false]",
                "open-font-select|animation|click:@trigger=>@popup[state:visible],@trigger[attribute:aria-expanded=true],@item(0)[attribute:data-highlighted=]",
                "select-arial|animation|key:ArrowDown=>@item(1)[attribute:data-highlighted=],@item(1)[focus:equals]||key:Enter=>@popup[state:hidden],@trigger[attribute:aria-expanded=false],@trigger[property:innerText=Arial]"),
            new("checkbox/hero", "checkbox/demos/hero/tailwind/index.tsx", "Checkbox/Hero",
                "initial|render|",
                "toggle-off|animation|click:[role='checkbox']=>[role='checkbox'][attribute:aria-checked=false],[role='checkbox'] > span[state:detached]")
        ]);
    }

    private static void AssertRazorContains(string blazor, params string[] expected)
    {
        var source = File.ReadAllText(RazorSourcePath(new FixtureSource("", blazor, "")));

        foreach (var value in expected)
        {
            source.ShouldContain(value);
        }
    }

    private static string ReactSourcePath(FixtureSource fixture)
        => Path.Combine(
            RepositoryRoot(),
            ".base-ui",
            "docs",
            "src",
            "app",
            "(docs)",
            "react",
            "components",
            fixture.React);

    private static string RazorSourcePath(FixtureSource fixture)
        => Path.Combine(
            ParityPaths.HarnessRoot,
            "Blazix.BaseUI.Parity.Tests.Client",
            "Fixtures",
            fixture.Blazor.Replace('/', Path.DirectorySeparatorChar) + ".razor");

    private static string RepositoryRoot()
        => Path.GetFullPath(Path.Combine(ParityPaths.HarnessRoot, "..", ".."));

    private static string[] ExtractReactClasses(string source)
        => ReactClassRegex().Matches(source)
            .Concat(ReactClassConstantRegex().Matches(source))
            .Select(match => match.Groups["value"].Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private static string[] ExtractRazorClasses(string source)
        => RazorClassRegex().Matches(source)
            .Select(match => match.Groups["value"].Value)
            .Where(value => !value.StartsWith('@'))
            .Concat(RazorClassConstantRegex().Matches(source).Select(match => match.Groups["value"].Value))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    [GeneratedRegex("className=\"(?<value>[^\"]+)\"")]
    private static partial Regex ReactClassRegex();

    [GeneratedRegex("const\\s+\\w+ClassName\\s*=\\s*'(?<value>[^']+)'")]
    private static partial Regex ReactClassConstantRegex();

    [GeneratedRegex("""(?:class|ClassValue)="(?:@\(_ => ")?(?<value>[^"]+)""")]
    private static partial Regex RazorClassRegex();

    [GeneratedRegex("private const string \\w*ClassName = \"(?<value>[^\"]+)\"")]
    private static partial Regex RazorClassConstantRegex();

    private sealed record FixtureSource(string Id, string Blazor, string React);
}

internal sealed record PublishedFixtureExpectation(
    string Id,
    string React,
    string Blazor,
    params string[] Steps);

internal static class Task16ManifestContract
{
    internal static void AssertExactEntries(IReadOnlyList<PublishedFixtureExpectation> expectations)
    {
        var manifest = FixtureManifest.Load();

        foreach (var expected in expectations)
        {
            var actual = manifest.Single(entry => entry.Id == expected.Id);
            actual.Component.ShouldBe(expected.Id[..expected.Id.IndexOf('/')]);
            actual.React.ShouldBe(expected.React);
            actual.Blazor.ShouldBe(expected.Blazor);
            actual.Themes.ShouldBe(["light"]);
            actual.PixelThreshold.ShouldBe(0.001);
            actual.Steps.Select(CanonicalStep).ShouldBe(expected.Steps, $"Manifest drift for {expected.Id}");
        }
    }

    private static string CanonicalStep(StepEntry step)
        => $"{step.Name}|{step.Settle}|{string.Join("||", step.Do.Select(CanonicalAction))}";

    private static string CanonicalAction(StepAction action)
    {
        var operation = action switch
        {
            { Click: { } selector } => $"click:{selector}",
            { Hover: { } selector } => $"hover:{selector}",
            { Key: { } key } => $"key:{key}",
            { Type: { } text, Into: { } selector } => $"type:{text}@{selector}",
            { Focus: { } selector } => $"focus:{selector}",
            { Blur: { } selector } => $"blur:{selector}",
            { Scroll: { } selector } => $"scroll:{selector}",
            { Wait: { } selector } => $"wait:{selector}",
            _ => throw new InvalidOperationException("Unsupported manifest action.")
        };

        var completion = action.Complete is not null
            ? string.Join(',', action.Complete.Select(CanonicalCompletion))
            : $"action-only:{action.ActionOnly?.Reason}";
        return $"{operation}=>{completion}";
    }

    private static string CanonicalCompletion(CompletionPredicate completion)
        => completion switch
        {
            { State: { } state } => $"{completion.Selector}[state:{state}]",
            { Attribute: { } attribute } => $"{completion.Selector}[attribute:{attribute}={completion.Expected}]",
            { Property: { } property } => $"{completion.Selector}[property:{property}={completion.Expected}]",
            { InputValue: { } value } => $"{completion.Selector}[inputValue:{value}]",
            { Focus: { } focus } => $"{completion.Selector}[focus:{focus}]",
            _ => throw new InvalidOperationException("Unsupported manifest completion predicate.")
        };
}
