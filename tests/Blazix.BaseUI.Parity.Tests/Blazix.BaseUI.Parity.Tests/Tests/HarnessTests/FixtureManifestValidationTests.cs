using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

public sealed class FixtureManifestValidationTests
{
    [Fact]
    public void RejectsDuplicateFixtureIdsOrdinally()
    {
        var json = $"[{Entry("switch/hero")},{Entry("switch/hero")}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("duplicate fixture id", Case.Insensitive);
    }

    [Fact]
    public void RejectsDuplicateJsonPropertyNames()
    {
        var json = """
            [{
              "id": "switch/hero",
              "id": "select/hero",
              "component": "switch",
              "react": "switch/demos/hero/tailwind/index.tsx",
              "blazor": "Switch/Hero"
            }]
            """;

        Should.Throw<JsonException>(() => FixtureManifest.Parse(json));
    }

    [Theory]
    [InlineData("null", "array")]
    [InlineData("[]", "at least one")]
    [InlineData("[\"LIGHT\"]", "light or dark")]
    [InlineData("[\"dark\",\"dark\"]", "duplicate")]
    [InlineData("[\"light\",\"sepia\"]", "light or dark")]
    public void RejectsNullEmptyUnknownCaseVariantAndDuplicateThemes(
        string themes,
        string expectedMessage)
    {
        var json = $"[{Entry("switch/hero", themes)}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain(expectedMessage, Case.Insensitive);
    }

    [Fact]
    public void PreservesDeclaredThemeOrder()
    {
        var json = $"[{Entry("switch/hero", "[\"dark\",\"light\"]")}]";

        var entry = FixtureManifest.Parse(json).Single();

        entry.Themes.ShouldBe(["dark", "light"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Switch/hero")]
    [InlineData("switch/Hero")]
    [InlineData("switch//hero")]
    [InlineData("switch/../hero")]
    [InlineData("switch/hero_test")]
    [InlineData("switch/hero--open")]
    [InlineData("switch/-hero")]
    [InlineData("switch/hero-")]
    [InlineData("switch/héro")]
    public void RejectsUnsafeOrNonLowercaseFixtureIds(string id)
    {
        var json = $"[{Entry(id)}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("safe lowercase fixture id", Case.Insensitive);
    }

    [Fact]
    public void RejectsCaseVariantFixtureIdsBeforeTheyCanCollide()
    {
        var json = $"[{Entry("switch/hero")},{Entry("Switch/hero")}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("safe lowercase fixture id", Case.Insensitive);
    }

    [Fact]
    public void RejectsAComponentThatDoesNotExactlyMatchTheFixtureId()
    {
        var json = $"[{Entry("switch/hero", component: "select")}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("component", Case.Insensitive);
    }

    [Theory]
    [InlineData("select/demos/hero/tailwind/index.tsx")]
    [InlineData("./switch/demos/hero/tailwind/index.tsx")]
    [InlineData("Switch/demos/hero/tailwind/index.tsx")]
    [InlineData("switch\\demos\\hero\\tailwind\\index.tsx")]
    public void RejectsReactPathsThatDoNotExactlyNameTheFixture(string react)
    {
        var json = $"[{Entry("switch/hero", react: react)}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("switch/demos/hero/tailwind/index.tsx");
    }

    [Fact]
    public void RejectsSwappedReactPathsAcrossOtherwiseValidFixtures()
    {
        var switchEntry = Entry(
            "switch/hero",
            react: "select/demos/hero/tailwind/index.tsx");
        var selectEntry = Entry(
            "select/hero",
            component: "select",
            react: "switch/demos/hero/tailwind/index.tsx",
            blazor: "Select/Hero");
        var json = $"[{switchEntry},{selectEntry}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("switch/demos/hero/tailwind/index.tsx");
    }

    [Theory]
    [InlineData("switch/Hero")]
    [InlineData("Switch/hero")]
    [InlineData("Select/Hero")]
    [InlineData("Switch\\Hero")]
    public void RejectsBlazorPathsThatDoNotExactlyNameTheFixture(string blazor)
    {
        var json = $"[{Entry("switch/hero", blazor: blazor)}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("Switch/Hero");
    }

    [Fact]
    public void RejectsSwappedDeclaredBlazorPortsEvenWhenBothAreRegistered()
    {
        FixtureEntry[] entries =
        [
            Fixture("switch/hero", "Harness/CaptureProbe"),
            Fixture("harness/capture-probe", "Switch/Hero")
        ];

        Should.Throw<InvalidOperationException>(() => FixtureManifest.ReconcileRegistry(entries))
            .Message.ShouldContain("Switch/Hero");
    }

    [Fact]
    public void RejectsADeclaredBlazorPortMissingFromTheRegistry()
    {
        var entry = Fixture("unregistered/hero", "Unregistered/Hero");

        Should.Throw<InvalidOperationException>(() =>
                FixtureManifest.ReconcileRegistry([entry]))
            .Message.ShouldContain("no registered Blazor port", Case.Insensitive);
    }

    [Theory]
    [InlineData("null", "must not be blank")]
    [InlineData("\"\"", "must not be blank")]
    [InlineData("\"   \"", "must not be blank")]
    [InlineData("\"Open\"", "safe lowercase step token")]
    [InlineData("\"open close\"", "safe lowercase step token")]
    [InlineData("\"open/close\"", "safe lowercase step token")]
    [InlineData("\"open_close\"", "safe lowercase step token")]
    [InlineData("\"open--close\"", "safe lowercase step token")]
    [InlineData("\"-open\"", "safe lowercase step token")]
    [InlineData("\"open-\"", "safe lowercase step token")]
    [InlineData("\"ópén\"", "safe lowercase step token")]
    public void RejectsBlankOrUnsafeStepTokens(string name, string expectedMessage)
    {
        var steps = $"[{{\"name\":{name}}}]";
        var json = $"[{Entry("switch/hero", steps: steps)}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain(expectedMessage, Case.Insensitive);
    }

    [Fact]
    public void RejectsDuplicateStepTokensWithinAFixture()
    {
        var steps = "[{\"name\":\"open\"},{\"name\":\"open\"}]";
        var json = $"[{Entry("switch/hero", steps: steps)}]";

        Should.Throw<FormatException>(() => FixtureManifest.Parse(json))
            .Message.ShouldContain("duplicate step token 'open'", Case.Insensitive);
    }

    [Fact]
    public void AcceptsExactReactBlazorAndUniqueStepDeclarations()
    {
        var steps = "[{\"name\":\"initial\"},{\"name\":\"toggle-off\"}]";
        var entries = FixtureManifest.Parse($"[{Entry("switch/hero", steps: steps)}]");

        Should.NotThrow(() => FixtureManifest.ReconcileRegistry(entries));
        entries.Single().Steps.Select(step => step.Name).ShouldBe(["initial", "toggle-off"]);
    }

    private static FixtureEntry Fixture(string id, string blazor) => new()
    {
        Id = id,
        Component = id[..id.IndexOf('/')],
        React = "unused",
        Blazor = blazor
    };

    private static string Entry(
        string id,
        string themes = "[\"light\"]",
        string? component = null,
        string? react = null,
        string? blazor = null,
        string steps = "[{\"name\":\"initial\"}]") => $$"""
        {
          "id": {{JsonSerializer.Serialize(id)}},
          "component": {{JsonSerializer.Serialize(component ?? "switch")}},
          "react": {{JsonSerializer.Serialize(react ?? "switch/demos/hero/tailwind/index.tsx")}},
          "blazor": {{JsonSerializer.Serialize(blazor ?? "Switch/Hero")}},
          "themes": {{themes}},
          "steps": {{steps}}
        }
        """;
}
