using System.Text.Json;
using System.Text.RegularExpressions;
using Blazix.BaseUI.Parity.Tests.Client;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the Navigation Menu hero port and its published interaction sequence to
/// the vendored React Base UI Tailwind demo.
/// </summary>
public sealed partial class Task16NavigationMenuHeroFixtureContractTests
{
    [Fact]
    public void AuthorsTheHeroAtTheExactRegistryConventionPath()
    {
        File.Exists(ReactSourcePath()).ShouldBeTrue();
        File.Exists(RazorSourcePath()).ShouldBeTrue();
    }

    [Fact]
    public void PreservesTheExactUpstreamTailwindClassSurface()
    {
        ExtractReactClasses(File.ReadAllText(ReactSourcePath())).ShouldBe(
            ExtractRazorClasses(File.ReadAllText(RazorSourcePath())));
    }

    [Fact]
    public void PreservesTheHeroStructureContentAndPerSideCollisionPadding()
    {
        var razor = File.ReadAllText(RazorSourcePath());

        foreach (var fragment in new[]
        {
            "<NavigationMenuRoot", "<NavigationMenuList", "<NavigationMenuItem>",
            "<NavigationMenuTrigger", "Overview", "Handbook", "<NavigationMenuIcon",
            "<NavigationMenuContent", "<NavigationMenuLink", "GitHub",
            "<NavigationMenuPortal>", "<NavigationMenuPositioner SideOffset=\"10\"",
            "CollisionPaddingPerSide=\"@(new Blazix.BaseUI.SidePadding(5, 20, 5, 20))\"",
            "CollisionAvoidanceSideMode.None", "<NavigationMenuPopup",
            "<NavigationMenuArrow", "<NavigationMenuViewport"
        })
        {
            razor.ShouldContain(fragment);
        }

        CountOccurrences(razor, "<NavigationMenuItem>").ShouldBe(3);
        CountOccurrences(razor, "<NavigationMenuTrigger").ShouldBe(2);
        CountOccurrences(razor, "<NavigationMenuContent").ShouldBe(2);
        CountOccurrences(razor, "new(\"").ShouldBe(7);
    }

    [Fact]
    public void ProposedManifestEntryExactlyMatchesPublishedFixtureAndRegistryResolution()
    {
        var fixture = FixtureManifest.Parse(ProposedManifestEntry).Single();
        var published = FixtureManifest.Load().Single(entry => entry.Id == "navigation-menu/hero");

        JsonSerializer.Serialize(fixture).ShouldBe(JsonSerializer.Serialize(published));
        FixtureRegistry.Ids.ShouldContain(fixture.Id);
        FixtureRegistry.Resolve(fixture.Component, fixture.Demo)?.FullName.ShouldBe(
            "Blazix.BaseUI.Parity.Tests.Client.Fixtures.NavigationMenu.Hero");

        fixture.Id.ShouldBe("navigation-menu/hero");
        fixture.React.ShouldBe("navigation-menu/demos/hero/tailwind/index.tsx");
        fixture.Blazor.ShouldBe("NavigationMenu/Hero");
        fixture.Steps.Select(step => step.Name).ShouldBe(
            ["initial", "open-overview", "switch-handbook", "close"]);
        fixture.Steps.Select(step => step.Settle).ShouldBe(
            ["render", "animation", "animation", "animation"]);

        fixture.Steps[1].Do.Single().Click.ShouldBe(OverviewTriggerSelector);
        fixture.Steps[1].Do.Single().Complete!.Count.ShouldBe(3);
        fixture.Steps[2].Do.Single().Hover.ShouldBe(HandbookTriggerSelector);
        fixture.Steps[2].Do.Single().Complete!.Count.ShouldBe(4);
        fixture.Steps[3].Do.Single().Key.ShouldBe("Escape");
        fixture.Steps[3].Do.Single().Complete!.Count.ShouldBe(2);
    }

    private static string[] ExtractReactClasses(string source)
    {
        var constants = ReactClassConstantRegex().Matches(source)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => string.Concat(QuotedSegmentRegex().Matches(match.Groups["value"].Value)
                    .Select(segment => segment.Groups["value"].Value)),
                StringComparer.Ordinal);

        return ReactLiteralClassRegex().Matches(source)
            .Select(match => match.Groups["value"].Value)
            .Concat(ReactClassReferenceRegex().Matches(source)
                .Select(match => constants[match.Groups["name"].Value]))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] ExtractRazorClasses(string source)
    {
        var constants = RazorClassConstantRegex().Matches(source)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["value"].Value,
                StringComparer.Ordinal);

        return RazorLiteralClassRegex().Matches(source)
            .Select(match => match.Groups["value"].Value)
            .Concat(RazorClassReferenceRegex().Matches(source)
                .Select(match => constants[match.Groups["name"].Value]))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static int CountOccurrences(string source, string value)
        => Regex.Matches(source, Regex.Escape(value), RegexOptions.CultureInvariant).Count;

    private static string ReactSourcePath()
        => Path.Combine(
            RepositoryRoot(), ".base-ui", "docs", "src", "app", "(docs)", "react", "components",
            "navigation-menu", "demos", "hero", "tailwind", "index.tsx");

    private static string RazorSourcePath()
        => Path.Combine(
            ParityPaths.HarnessRoot, "Blazix.BaseUI.Parity.Tests.Client", "Fixtures",
            "NavigationMenu", "Hero.razor");

    private static string RepositoryRoot()
        => Path.GetFullPath(Path.Combine(ParityPaths.HarnessRoot, "..", ".."));

    private const string OverviewTriggerSelector = "ul > li:nth-child(1) > button[aria-expanded]";
    private const string HandbookTriggerSelector = "ul > li:nth-child(2) > button[aria-expanded]";

    private const string ProposedManifestEntry = """
        [
          {
            "id": "navigation-menu/hero",
            "component": "navigation-menu",
            "react": "navigation-menu/demos/hero/tailwind/index.tsx",
            "blazor": "NavigationMenu/Hero",
            "themes": ["light"],
            "pixelThreshold": 0.001,
            "steps": [
              { "name": "initial" },
              {
                "name": "open-overview",
                "settle": "animation",
                "do": [{
                  "click": "ul > li:nth-child(1) > button[aria-expanded]",
                  "complete": [
                    { "selector": "ul > li:nth-child(1) > button[aria-expanded]", "attribute": "aria-expanded", "equals": "true" },
                    { "selector": "[role='presentation'][data-side]", "state": "visible" },
                    { "selector": "a[href='/react/overview/quick-start']", "state": "visible" }
                  ]
                }]
              },
              {
                "name": "switch-handbook",
                "settle": "animation",
                "do": [{
                  "hover": "ul > li:nth-child(2) > button[aria-expanded]",
                  "complete": [
                    { "selector": "ul > li:nth-child(1) > button[aria-expanded]", "attribute": "aria-expanded", "equals": "false" },
                    { "selector": "ul > li:nth-child(2) > button[aria-expanded]", "attribute": "aria-expanded", "equals": "true" },
                    { "selector": "a[href='/react/overview/quick-start']", "state": "hidden" },
                    { "selector": "a[href='/react/handbook/styling']", "state": "visible" }
                  ]
                }]
              },
              {
                "name": "close",
                "settle": "animation",
                "do": [{
                  "key": "Escape",
                  "complete": [
                    { "selector": "ul > li:nth-child(2) > button[aria-expanded]", "attribute": "aria-expanded", "equals": "false" },
                    { "selector": "[role='presentation'][data-side]", "state": "detached" }
                  ]
                }]
              }
            ]
          }
        ]
        """;

    [GeneratedRegex("className=\"(?<value>[^\"]+)\"")]
    private static partial Regex ReactLiteralClassRegex();

    [GeneratedRegex("className=\\{(?<name>[A-Za-z][A-Za-z0-9]*)\\}")]
    private static partial Regex ReactClassReferenceRegex();

    [GeneratedRegex("const\\s+(?<name>[A-Za-z][A-Za-z0-9]*)\\s*=\\s*(?<value>(?:'[^']*'\\s*\\+?\\s*)+);")]
    private static partial Regex ReactClassConstantRegex();

    [GeneratedRegex("'(?<value>[^']*)'")]
    private static partial Regex QuotedSegmentRegex();

    [GeneratedRegex("\\bclass=\"(?!@)(?<value>[^\"]+)\"")]
    private static partial Regex RazorLiteralClassRegex();

    [GeneratedRegex("\\bclass=\"@(?<name>[A-Za-z][A-Za-z0-9]*)\"")]
    private static partial Regex RazorClassReferenceRegex();

    [GeneratedRegex("const\\s+string\\s+(?<name>[A-Za-z][A-Za-z0-9]*)\\s*=\\s*\"(?<value>[^\"]+)\"")]
    private static partial Regex RazorClassConstantRegex();
}
