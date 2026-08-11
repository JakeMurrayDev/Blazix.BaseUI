using Blazix.BaseUI.Parity.Tests.Client;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

public sealed class MilestoneFixtureCatalogTests
{
    private static readonly string[] ExpectedFixtureIds =
    [
        "switch/hero",
        "collapsible/hero",
        "popover/hero",
        "select/grouped",
        "field/hero",
        "avatar/hero",
        "separator/hero",
        "progress/hero",
        "meter/hero",
        "accordion/multiple",
        "dialog/hero",
        "drawer/hero",
        "toast/hero",
        "tooltip/hero",
        "preview-card/hero",
        "menu/arrow",
        "select/hero",
        "menu/checkbox-items",
        "menubar/hero",
        "tabs/hero",
        "toolbar/hero",
        "form/hero",
        "number-field/hero",
        "checkbox/hero",
        "otp-field/hero",
        "popover/detached-triggers-simple",
        "navigation-menu/hero",
        "scroll-area/hero",
        "combobox/hero"
    ];

    [Fact]
    public void PinsTheOrderedTwentyNineFixtureTwentySixComponentCatalog()
    {
        var path = Path.Combine(ParityPaths.Manifest, "milestone-1.json");
        File.Exists(path).ShouldBeTrue();
        var parsed = MilestoneFixtureCatalog.Parse(File.ReadAllText(path));

        parsed.ShouldBe(ExpectedFixtureIds);
        MilestoneFixtureCatalog.Ids.ShouldBe(ExpectedFixtureIds);
        MilestoneFixtureCatalog.Components.ShouldBe(
        [
            "switch",
            "collapsible",
            "popover",
            "select",
            "field",
            "avatar",
            "separator",
            "progress",
            "meter",
            "accordion",
            "dialog",
            "drawer",
            "toast",
            "tooltip",
            "preview-card",
            "menu",
            "menubar",
            "tabs",
            "toolbar",
            "form",
            "number-field",
            "checkbox",
            "otp-field",
            "navigation-menu",
            "scroll-area",
            "combobox"
        ]);
    }

    [Fact]
    public void PublishedManifestAndRegistryReconcileAsTheCompleteOrderedCatalog()
    {
        var entries = FixtureManifest.Load();
        var reconciliation = MilestoneFixtureCatalog.Reconcile(
            entries.Select(entry => entry.Id),
            FixtureRegistry.Ids);

        entries.Select(entry => entry.Id).ShouldBe(ExpectedFixtureIds);
        FixtureRegistry.Ids
            .Where(item => !item.StartsWith("harness/", StringComparison.Ordinal))
            .OrderBy(item => item, StringComparer.Ordinal)
            .ShouldBe(ExpectedFixtureIds.OrderBy(item => item, StringComparer.Ordinal));
        reconciliation.MissingManifestIds.ShouldBeEmpty();
        reconciliation.MissingRegistryIds.ShouldBeEmpty();
    }

    [Fact]
    public void FullCatalogReconcilesRegardlessOfRegistryDiscoveryOrder()
    {
        var registryIds = ExpectedFixtureIds
            .Reverse()
            .Append("harness/canary")
            .Append("harness/capture-probe");

        var reconciliation = MilestoneFixtureCatalog.Reconcile(ExpectedFixtureIds, registryIds);

        reconciliation.MissingManifestIds.ShouldBeEmpty();
        reconciliation.MissingRegistryIds.ShouldBeEmpty();
    }

    [Fact]
    public void RejectsMilestoneFixturesThatAreOutOfCatalogOrder()
    {
        string[] ids = ["select/grouped", "switch/hero"];

        Should.Throw<InvalidOperationException>(() =>
                MilestoneFixtureCatalog.Reconcile(ids, ids))
            .Message.ShouldContain("catalog order", Case.Insensitive);
    }

    [Fact]
    public void AllowsManifestFixturesOutsideTheMilestoneCatalogForIssue176Expansion()
    {
        string[] ids = ["switch/hero", "future-component/hero"];

        var reconciliation = MilestoneFixtureCatalog.Reconcile(ids, ids);

        reconciliation.MissingManifestIds.ShouldBe(ExpectedFixtureIds[1..]);
        reconciliation.MissingRegistryIds.ShouldBe(ExpectedFixtureIds[1..]);
    }

    [Fact]
    public void RejectsMissingAndUnexpectedRegistryIds()
    {
        Should.Throw<InvalidOperationException>(() =>
                MilestoneFixtureCatalog.Reconcile(["switch/hero"], []))
            .Message.ShouldContain("missing", Case.Insensitive);

        Should.Throw<InvalidOperationException>(() =>
                MilestoneFixtureCatalog.Reconcile(
                    ["switch/hero"],
                    ["switch/hero", "harness/canary", "select/grouped"]))
            .Message.ShouldContain("unexpected", Case.Insensitive);
    }

    [Theory]
    [InlineData("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"schemaVersion\": 1")]
    [InlineData("\"fixtureCount\": 29", "\"fixtureCount\": 28")]
    [InlineData("\"componentCount\": 26", "\"componentCount\": 25")]
    [InlineData("\"switch/hero\"", "\"Switch/hero\"")]
    [InlineData("\"switch/hero\"", "\"switch/hero/extra\"")]
    public void RejectsDuplicatePropertiesWrongCountsAndInvalidFixtureIds(
        string original,
        string replacement)
    {
        var json = File.ReadAllText(
            Path.Combine(ParityPaths.Manifest, "milestone-1.json"));

        Should.Throw<FormatException>(() =>
            MilestoneFixtureCatalog.Parse(json.Replace(
                original, replacement, StringComparison.Ordinal)));
    }
}
