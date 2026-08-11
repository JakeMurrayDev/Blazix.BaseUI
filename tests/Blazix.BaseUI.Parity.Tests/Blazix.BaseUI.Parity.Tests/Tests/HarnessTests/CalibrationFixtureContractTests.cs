using System.Text.RegularExpressions;
using Blazix.BaseUI.Parity.Tests.Client;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the authored Task 15 corpus to the exact upstream demos and state-covering action grammar.
/// This test deliberately inspects source as well as parsed manifest data: a renamed demo, drifted
/// Tailwind port, or weakened completion predicate must not silently refresh a baseline.
/// </summary>
public sealed partial class CalibrationFixtureContractTests
{
    private static readonly string[] ExpectedFixtureIds =
    [
        "switch/hero",
        "collapsible/hero",
        "popover/hero",
        "select/grouped",
        "field/hero"
    ];

    [Fact]
    public void PinsTheExactOrderedCalibrationPrefixAndRegistrySurface()
    {
        var publishedFixtures = FixtureManifest.Load();
        var fixtures = publishedFixtures.Take(ExpectedFixtureIds.Length).ToArray();

        publishedFixtures.Select(item => item.Id).Take(ExpectedFixtureIds.Length)
            .ShouldBe(ExpectedFixtureIds);
        fixtures.Select(item => item.Id).ShouldBe(ExpectedFixtureIds);
        fixtures.Select(item => item.React).ShouldBe(
        [
            "switch/demos/hero/tailwind/index.tsx",
            "collapsible/demos/hero/tailwind/index.tsx",
            "popover/demos/hero/tailwind/index.tsx",
            "select/demos/grouped/tailwind/index.tsx",
            "field/demos/hero/tailwind/index.tsx"
        ]);
        fixtures.Select(item => item.Blazor).ShouldBe(
            ["Switch/Hero", "Collapsible/Hero", "Popover/Hero", "Select/Grouped", "Field/Hero"]);
        foreach (var fixture in fixtures)
        {
            fixture.Themes.ShouldBe(["light"]);
            fixture.PixelThreshold.ShouldBe(0.001);
        }

        FixtureRegistry.Ids.Where(ExpectedFixtureIds.Contains).OrderBy(item => item)
            .ShouldBe(ExpectedFixtureIds.OrderBy(item => item));
        foreach (var fixture in fixtures)
        {
            var type = FixtureRegistry.Resolve(fixture.Component, fixture.Demo);
            type.ShouldNotBeNull();
            type.FullName.ShouldBe(
                $"Blazix.BaseUI.Parity.Tests.Client.Fixtures.{fixture.Blazor.Replace('/', '.')}");
        }
    }

    [Fact]
    public void PinsEveryActionAndObservableCompletionInManifestOrder()
    {
        var fixtures = FixtureManifest.Load();
        var calibrationFixtures = fixtures.Take(ExpectedFixtureIds.Length).ToArray();
        var signatures = calibrationFixtures.ToDictionary(
            item => item.Id,
            item => item.Steps.Select(StepSignature).ToArray(),
            StringComparer.Ordinal);

        signatures["switch/hero"].ShouldBe(
        [
            "initial",
            "toggle-off:click=[role='switch'];complete[[role='switch']:attribute=aria-checked=false]"
        ]);
        signatures["collapsible/hero"].ShouldBe(
        [
            "initial",
            "open:click=button[aria-expanded];complete[button[aria-expanded]:attribute=aria-expanded=true|button[aria-expanded='true'] + div:state=visible]",
            "close:click=button[aria-expanded];complete[button[aria-expanded]:attribute=aria-expanded=false|button[aria-expanded='false'] + div:state=detached]"
        ]);
        signatures["popover/hero"].ShouldBe(
        [
            "initial",
            "open:click=button[aria-haspopup='dialog'];complete[[role='dialog']:state=visible|button[aria-haspopup='dialog']:attribute=aria-expanded=true]",
            "close:key=Escape;complete[[role='dialog']:state=detached]"
        ]);
        signatures["select/grouped"].ShouldBe(
        [
            "initial",
            "open:click=@trigger;complete[@popup:state=visible|@trigger:attribute=aria-expanded=true|@item(0):attribute=data-highlighted=|@item(0):focus=equals]",
            "arrow-down:key=ArrowDown;complete[@item(1):attribute=data-highlighted=|@item(1):focus=equals]",
            "arrow-up:key=ArrowUp;complete[@item(0):attribute=data-highlighted=|@item(0):focus=equals]",
            "select-banana:key=ArrowDown;complete[@item(1):attribute=data-highlighted=|@item(1):focus=equals]|key=Enter;complete[@popup:state=hidden|@trigger:attribute=aria-expanded=false|@trigger:property=innerText=Banana]"
        ]);
        signatures["field/hero"].ShouldBe(
        [
            "initial",
            "focus:focus=@input;complete[@input:focus=equals]",
            "type:type=Ada into @input;complete[@input:inputValue=Ada]",
            "blur:blur=@input;complete[@input:inputValue=Ada|@input:focus=not-equals|.text-red-700:state=detached]"
        ]);

        calibrationFixtures.Single(item => item.Id == "switch/hero").Steps.Single(item => item.Name == "toggle-off")
            .Settle.ShouldBe("animation");
        calibrationFixtures.Single(item => item.Id == "collapsible/hero").Steps.Single(item => item.Name == "open")
            .Settle.ShouldBe("animation");
        calibrationFixtures.Single(item => item.Id == "collapsible/hero").Steps.Single(item => item.Name == "close")
            .Settle.ShouldBe("animation");
        calibrationFixtures.Single(item => item.Id == "popover/hero").Steps.Single(item => item.Name == "open")
            .Settle.ShouldBe("animation");
        calibrationFixtures.Single(item => item.Id == "popover/hero").Steps.Single(item => item.Name == "close")
            .Settle.ShouldBe("animation");

        foreach (var action in fixtures
                     .SelectMany(item => item.Steps)
                     .SelectMany(item => item.Do))
        {
            action.ActionOnly.ShouldBeNull();
            action.Complete.ShouldNotBeNull();
            action.Complete.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public void GroupedSelectSettlesOnlyPopupLifecycleStepsOnAnimation()
    {
        var steps = FixtureManifest.Load().Single(item => item.Id == "select/grouped").Steps;

        steps.Single(item => item.Name == "open").Settle.ShouldBe("animation");
        steps.Single(item => item.Name == "select-banana").Settle.ShouldBe("animation");
        steps.Single(item => item.Name == "initial").Settle.ShouldBe("render");
        steps.Single(item => item.Name == "arrow-down").Settle.ShouldBe("render");
        steps.Single(item => item.Name == "arrow-up").Settle.ShouldBe("render");
    }

    [Fact]
    public void FrozenRazorPortsCarryTheExactUpstreamTailwindClassMultisets()
    {
        foreach (var fixture in FixtureManifest.Load().Take(ExpectedFixtureIds.Length))
        {
            var react = File.ReadAllText(ReactSourcePath(fixture));
            var razor = File.ReadAllText(RazorSourcePath(fixture));

            ExtractReactClasses(react).ShouldBe(
                ExtractRazorClasses(razor),
                $"Tailwind class drift for {fixture.Id}");
        }
    }

    [Fact]
    public void GroupedSelectPinsAllSixteenValuesAndLabelsInSourceOrder()
    {
        var fixture = FixtureManifest.Load().Single(item => item.Id == "select/grouped");
        var react = File.ReadAllText(ReactSourcePath(fixture));
        var razor = File.ReadAllText(RazorSourcePath(fixture));
        string[] options =
        [
            "apple|Apple", "banana|Banana", "mango|Mango", "kiwi|Kiwi",
            "grape|Grape", "orange|Orange", "strawberry|Strawberry", "watermelon|Watermelon",
            "broccoli|Broccoli", "carrot|Carrot", "cauliflower|Cauliflower", "cucumber|Cucumber",
            "kale|Kale", "pepper|Bell pepper", "spinach|Spinach", "zucchini|Zucchini"
        ];

        ExtractReactOptions(react).ShouldBe(options);
        ExtractRazorOptions(razor).ShouldBe(options);
    }

    private static string StepSignature(StepEntry step)
        => step.Do.Count == 0
            ? step.Name
            : $"{step.Name}:{string.Join('|', step.Do.Select(ActionSignature))}";

    private static string ActionSignature(StepAction action)
    {
        var verb = action.Click is not null ? $"click={action.Click}" :
            action.Key is not null ? $"key={action.Key}" :
            action.Type is not null ? $"type={action.Type} into {action.Into}" :
            action.Focus is not null ? $"focus={action.Focus}" :
            action.Blur is not null ? $"blur={action.Blur}" :
            throw new InvalidOperationException("Calibration action uses an unexpected verb.");
        return $"{verb};complete[{string.Join('|', action.Complete!.Select(PredicateSignature))}]";
    }

    private static string PredicateSignature(CompletionPredicate predicate)
    {
        var value = predicate.State is not null ? $"state={predicate.State}" :
            predicate.Attribute is not null ? $"attribute={predicate.Attribute}={predicate.Expected}" :
            predicate.Property is not null ? $"property={predicate.Property}={predicate.Expected}" :
            predicate.InputValue is not null ? $"inputValue={predicate.InputValue}" :
            predicate.Focus is not null ? $"focus={predicate.Focus}" :
            throw new InvalidOperationException("Calibration completion uses an unexpected predicate.");
        return $"{predicate.Selector}:{value}";
    }

    private static string ReactSourcePath(FixtureEntry fixture)
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

    private static string RazorSourcePath(FixtureEntry fixture)
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
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private static string[] ExtractReactOptions(string source)
        => ReactOptionRegex().Matches(source)
            .Select(match => $"{match.Groups["value"].Value}|{match.Groups["label"].Value}")
            .ToArray();

    private static string[] ExtractRazorOptions(string source)
        => RazorOptionRegex().Matches(source)
            .Select(match => $"{match.Groups["value"].Value}|{match.Groups["label"].Value}")
            .ToArray();

    [GeneratedRegex("className=\"(?<value>[^\"]+)\"")]
    private static partial Regex ReactClassRegex();

    [GeneratedRegex("const\\s+\\w+ClassName\\s*=\\s*'(?<value>[^']+)'")]
    private static partial Regex ReactClassConstantRegex();

    [GeneratedRegex("""(?:class|ClassValue)="(?:@\(_ => ")?(?<value>[^"]+)""")]
    private static partial Regex RazorClassRegex();

    [GeneratedRegex("value: '(?<value>[^']+)', label: '(?<label>[^']+)'")]
    private static partial Regex ReactOptionRegex();

    [GeneratedRegex("new\\(\"(?<value>[^\"]+)\", \"(?<label>[^\"]+)\"\\)")]
    private static partial Regex RazorOptionRegex();
}
