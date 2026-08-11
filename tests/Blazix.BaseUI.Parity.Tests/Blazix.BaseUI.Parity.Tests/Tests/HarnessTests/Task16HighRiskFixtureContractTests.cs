using System.Text.Json;
using System.Text.RegularExpressions;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the published Task 16 high-risk fixture batch to the exact pinned React demos and
/// browser-observable manifest contracts.
/// </summary>
public sealed partial class Task16HighRiskFixtureContractTests
{
    private static readonly (string Id, string React, string Blazor)[] AuthoredFixtures =
    [
        ("select/hero", "select/demos/hero/tailwind/index.tsx", "Select/Hero"),
        ("form/hero", "form/demos/hero/tailwind/index.tsx", "Form/Hero"),
        ("number-field/hero", "number-field/demos/hero/tailwind/index.tsx", "NumberField/Hero"),
        ("otp-field/hero", "otp-field/demos/hero/tailwind/index.tsx", "OtpField/Hero"),
        ("popover/detached-triggers-simple", "popover/demos/detached-triggers-simple/tailwind/index.tsx", "Popover/DetachedTriggersSimple"),
        ("scroll-area/hero", "scroll-area/demos/hero/tailwind/index.tsx", "ScrollArea/Hero"),
        ("combobox/hero", "combobox/demos/hero/tailwind/index.tsx", "Combobox/Hero")
    ];

    [Fact]
    public void PinsExactEffectiveTailwindClassMultisetsFromPinnedReactSources()
    {
        foreach (var fixture in AuthoredFixtures)
        {
            var react = File.ReadAllText(ReactSourcePath(fixture.React));
            var razor = File.ReadAllText(RazorSourcePath(fixture.Blazor));

            ExtractReactEffectiveClasses(react).ShouldBe(
                ExtractRazorClasses(razor),
                $"Tailwind class drift for {fixture.Id}");
        }
    }

    [Fact]
    public void PinsExactDemoContentAndPartStructure()
    {
        AssertSourceContract("Select/Hero",
            "<SelectRoot", "<SelectLabel", "<SelectTrigger", "<SelectValue", "<SelectIcon",
            "<SelectPortal", "<SelectPositioner", "<SelectPopup", "<SelectScrollUpArrow",
            "<SelectList", "<SelectItem ", "<SelectItemIndicator", "<SelectItemText",
            "<SelectScrollDownArrow", "Apple", "Select apple", "Granny Smith", "Pink Lady");
        AssertSourceContract("Form/Hero",
            "<Form ", "<FieldRoot", "<FieldLabel", "<FieldControl", "<FieldError", "<Button ",
            "https://example.com", "The example domain is not allowed", "Task.Delay(1000)");
        AssertSourceContract("NumberField/Hero",
            "<NumberFieldRoot", "DefaultValue=\"100\"", "<NumberFieldScrubArea", "<label",
            "<NumberFieldScrubAreaCursor", "<NumberFieldGroup", "<NumberFieldDecrement",
            "<NumberFieldInput", "<NumberFieldIncrement", "Amount");
        AssertSourceContract("OtpField/Hero",
            "<OtpFieldRoot", "Length=\"6\"", "<OtpFieldInput", "index < 6",
            "Verification code", "Character {index + 1} of 6",
            "Enter the 6-character code we sent to your device.");
        AssertSourceContract("Popover/DetachedTriggersSimple",
            "<PopoverTrigger", "<PopoverRoot", "<PopoverPortal", "<PopoverPositioner",
            "<PopoverPopup", "<PopoverArrow", "<PopoverTitle", "<PopoverDescription",
            "PopoverHandleFactory.CreateHandle()", "Notifications",
            "You are all caught up. Good job!");
        AssertSourceContract("ScrollArea/Hero",
            "<ScrollAreaRoot", "<ScrollAreaViewport", "<ScrollAreaContent",
            "<ScrollAreaScrollbar", "<ScrollAreaThumb", "Vernacular architecture",
            "sustainable design");
        AssertSourceContract("Combobox/Hero",
            "<ComboboxRoot", "<ComboboxInputGroup", "<ComboboxInput ", "<ComboboxClear",
            "<ComboboxTrigger", "<ComboboxPortal", "<ComboboxPositioner", "<ComboboxPopup",
            "<ComboboxEmpty", "<ComboboxList", "<ComboboxCollection", "<ComboboxItem ",
            "<ComboboxItemIndicator", "Choose a fruit", "No fruits found.", "Passionfruit");

        var detached = File.ReadAllText(RazorSourcePath("Popover/DetachedTriggersSimple"));
        detached.IndexOf("<PopoverTrigger", StringComparison.Ordinal).ShouldBeLessThan(
            detached.IndexOf("<PopoverRoot", StringComparison.Ordinal));
    }

    [Fact]
    public void PinsSelectAndComboboxValuesAndLabelsInSourceOrder()
    {
        var selectReact = File.ReadAllText(ReactSourcePath("select/demos/hero/tailwind/index.tsx"));
        var selectRazor = File.ReadAllText(RazorSourcePath("Select/Hero"));
        ExtractReactOptions(selectReact).ShouldBe(
        [
            "gala|Gala", "fuji|Fuji", "honeycrisp|Honeycrisp",
            "granny-smith|Granny Smith", "pink-lady|Pink Lady"
        ]);
        ExtractRazorOptions(selectRazor).ShouldBe(ExtractReactOptions(selectReact));

        var comboboxReact = File.ReadAllText(ReactSourcePath("combobox/demos/hero/tailwind/index.tsx"));
        var comboboxRazor = File.ReadAllText(RazorSourcePath("Combobox/Hero"));
        ExtractReactOptions(comboboxReact).ShouldBe(ExtractRazorOptions(comboboxRazor));
        ExtractReactOptions(comboboxReact).Length.ShouldBe(25);
    }

    [Fact]
    public void PublishedManifestMatchesTheStrictStateCoveringProposals()
    {
        var proposedFixtures = FixtureManifest.Parse(ProposedManifestEntries);
        var authoredIds = AuthoredFixtures.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var publishedFixtures = FixtureManifest.Load()
            .Where(item => authoredIds.Contains(item.Id))
            .ToArray();

        JsonSerializer.Serialize(publishedFixtures)
            .ShouldBe(JsonSerializer.Serialize(proposedFixtures));
        proposedFixtures.Select(item => item.Id).ShouldBe(AuthoredFixtures.Select(item => item.Id));
        proposedFixtures.SelectMany(item => item.Steps).SelectMany(item => item.Do)
            .ShouldAllBe(action =>
            action.ActionOnly == null && action.Complete != null && action.Complete.Count > 0);

        proposedFixtures.Single(item => item.Id == "select/hero").Steps
            .Where(step => step.Name is "open" or "select-fuji")
            .ShouldAllBe(step => step.Settle == "animation");
        proposedFixtures.Single(item => item.Id == "popover/detached-triggers-simple").Steps
            .Where(step => step.Name is "open" or "close")
            .ShouldAllBe(step => step.Settle == "animation");
        proposedFixtures.Single(item => item.Id == "combobox/hero").Steps
            .Where(step => step.Name is "open" or "select-banana")
            .ShouldAllBe(step => step.Settle == "animation");
    }

    private static void AssertSourceContract(string blazor, params string[] exactMarkers)
    {
        var source = File.ReadAllText(RazorSourcePath(blazor));
        foreach (var marker in exactMarkers)
        {
            source.ShouldContain(marker);
        }
    }

    private static string[] ExtractReactEffectiveClasses(string source)
    {
        var constants = ReactStringConstantRegex().Matches(source)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["value"].Value,
                StringComparer.Ordinal);
        var classes = ReactLiteralClassRegex().Matches(source)
            .Select(match => match.Groups["value"].Value)
            .ToList();

        foreach (Match match in ReactReferenceClassRegex().Matches(source))
        {
            classes.Add(constants[match.Groups["name"].Value]);
        }

        foreach (Match match in ReactTemplateClassRegex().Matches(source))
        {
            classes.Add($"{constants[match.Groups["name"].Value]} {match.Groups["suffix"].Value}");
        }

        return classes.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

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

    private static string ReactSourcePath(string relativePath)
        => Path.Combine(
            RepositoryRoot(), ".base-ui", "docs", "src", "app", "(docs)", "react",
            "components", relativePath);

    private static string RazorSourcePath(string relativePath)
        => Path.Combine(
            ParityPaths.HarnessRoot, "Blazix.BaseUI.Parity.Tests.Client", "Fixtures",
            relativePath.Replace('/', Path.DirectorySeparatorChar) + ".razor");

    private static string RepositoryRoot()
        => Path.GetFullPath(Path.Combine(ParityPaths.HarnessRoot, "..", ".."));

    [GeneratedRegex("className=\"(?<value>[^\"]+)\"")]
    private static partial Regex ReactLiteralClassRegex();

    [GeneratedRegex("className=\\{(?<name>[A-Za-z][A-Za-z0-9]*)\\}")]
    private static partial Regex ReactReferenceClassRegex();

    [GeneratedRegex("className=\\{`\\$\\{(?<name>[A-Za-z][A-Za-z0-9]*)\\} (?<suffix>[^`]+)`\\}")]
    private static partial Regex ReactTemplateClassRegex();

    [GeneratedRegex("const\\s+(?<name>[A-Za-z][A-Za-z0-9]*)\\s*=\\s*'(?<value>[^']+)';")]
    private static partial Regex ReactStringConstantRegex();

    [GeneratedRegex("class=\"(?<value>[^\"]+)\"")]
    private static partial Regex RazorClassRegex();

    [GeneratedRegex("\\{ label: '(?<label>[^']+)', value: '(?<value>[^']+)' \\}")]
    private static partial Regex ReactOptionRegex();

    [GeneratedRegex("new\\(\"(?<value>[^\"]+)\", \"(?<label>[^\"]+)\"\\)")]
    private static partial Regex RazorOptionRegex();

    private const string ProposedManifestEntries = """
        [
          {
            "id": "select/hero",
            "component": "select",
            "react": "select/demos/hero/tailwind/index.tsx",
            "blazor": "Select/Hero",
            "steps": [
              { "name": "initial" },
              { "name": "open", "settle": "animation", "do": [{
                "click": "@trigger",
                "complete": [
                  { "selector": "@popup", "state": "visible" },
                  { "selector": "@trigger", "attribute": "aria-expanded", "equals": "true" },
                  { "selector": "@item(0)", "attribute": "data-highlighted", "equals": "" },
                  { "selector": "@item(0)", "focus": "equals" }
                ]
              }] },
              { "name": "select-fuji", "settle": "animation", "do": [
                { "key": "ArrowDown", "complete": [
                  { "selector": "@item(1)", "attribute": "data-highlighted", "equals": "" },
                  { "selector": "@item(1)", "focus": "equals" }
                ] },
                { "key": "Enter", "complete": [
                  { "selector": "@popup", "state": "hidden" },
                  { "selector": "@trigger", "attribute": "aria-expanded", "equals": "false" },
                  { "selector": "@trigger", "property": "innerText", "equals": "Fuji" }
                ] }
              ] }
            ]
          },
          {
            "id": "form/hero",
            "component": "form",
            "react": "form/demos/hero/tailwind/index.tsx",
            "blazor": "Form/Hero",
            "steps": [
              { "name": "initial" },
              { "name": "submit-example-domain", "do": [{
                "click": "button[type='submit']",
                "complete": [
                  { "selector": ".text-red-700", "state": "visible" },
                  { "selector": ".text-red-700", "property": "innerText", "equals": "The example domain is not allowed" },
                  { "selector": "button[type='submit']", "property": "disabled", "equals": "false" }
                ]
              }] }
            ]
          },
          {
            "id": "number-field/hero",
            "component": "number-field",
            "react": "number-field/demos/hero/tailwind/index.tsx",
            "blazor": "NumberField/Hero",
            "steps": [
              { "name": "initial" },
              { "name": "increment", "do": [{
                "click": "button[aria-label='Increase']",
                "complete": [{ "selector": "input[type='text']", "inputValue": "101" }]
              }] },
              { "name": "decrement", "do": [{
                "click": "button[aria-label='Decrease']",
                "complete": [{ "selector": "input[type='text']", "inputValue": "100" }]
              }] }
            ]
          },
          {
            "id": "otp-field/hero",
            "component": "otp-field",
            "react": "otp-field/demos/hero/tailwind/index.tsx",
            "blazor": "OtpField/Hero",
            "steps": [
              { "name": "initial" },
              { "name": "complete-code", "do": [{
                "type": "123456",
                "into": "[role='group'] input:first-of-type",
                "complete": [
                  { "selector": "[role='group']", "attribute": "data-complete", "equals": "" },
                  { "selector": "input[aria-hidden='true']", "inputValue": "123456" },
                  { "selector": "input[aria-label='Character 6 of 6']", "focus": "equals" }
                ]
              }] }
            ]
          },
          {
            "id": "popover/detached-triggers-simple",
            "component": "popover",
            "react": "popover/demos/detached-triggers-simple/tailwind/index.tsx",
            "blazor": "Popover/DetachedTriggersSimple",
            "steps": [
              { "name": "initial" },
              { "name": "open", "settle": "animation", "do": [{
                "click": "@trigger",
                "complete": [
                  { "selector": "@popup", "state": "visible" },
                  { "selector": "@trigger", "attribute": "aria-expanded", "equals": "true" }
                ]
              }] },
              { "name": "close", "settle": "animation", "do": [{
                "key": "Escape",
                "complete": [
                  { "selector": "@popup", "state": "detached" },
                  { "selector": "@trigger", "attribute": "aria-expanded", "equals": "false" }
                ]
              }] }
            ]
          },
          {
            "id": "scroll-area/hero",
            "component": "scroll-area",
            "react": "scroll-area/demos/hero/tailwind/index.tsx",
            "blazor": "ScrollArea/Hero",
            "steps": [
              { "name": "initial" },
              { "name": "focus-viewport", "do": [{
                "focus": "[role='presentation'][tabindex='0']",
                "complete": [{ "selector": "[role='presentation'][tabindex='0']", "focus": "equals" }]
              }] }
            ]
          },
          {
            "id": "combobox/hero",
            "component": "combobox",
            "react": "combobox/demos/hero/tailwind/index.tsx",
            "blazor": "Combobox/Hero",
            "steps": [
              { "name": "initial" },
              { "name": "open", "settle": "animation", "do": [{
                "click": "input[role='combobox']",
                "complete": [
                  { "selector": "@popup", "state": "visible" },
                  { "selector": "input[role='combobox']", "attribute": "aria-expanded", "equals": "true" },
                  { "selector": "input[role='combobox']", "focus": "equals" }
                ]
              }] },
              { "name": "filter-banana", "do": [{
                "type": "Ban",
                "into": "input[role='combobox']",
                "complete": [
                  { "selector": "input[role='combobox']", "inputValue": "Ban" },
                  { "selector": "@item(0)", "state": "visible" },
                  { "selector": "@item(0)", "property": "innerText", "equals": "Banana" }
                ]
              }] },
              { "name": "select-banana", "settle": "animation", "do": [
                { "key": "ArrowDown", "complete": [
                  { "selector": "@item(0)", "attribute": "data-highlighted", "equals": "" },
                  { "selector": "input[role='combobox']", "focus": "equals" }
                ] },
                { "key": "Enter", "complete": [
                  { "selector": "input[role='combobox']", "inputValue": "Banana" },
                  { "selector": "input[role='combobox']", "attribute": "aria-expanded", "equals": "false" },
                  { "selector": "@popup", "state": "detached" }
                ] }
              ] }
            ]
          }
        ]
        """;
}
