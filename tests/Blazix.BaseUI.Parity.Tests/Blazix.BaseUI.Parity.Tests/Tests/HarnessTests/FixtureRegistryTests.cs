using Blazix.BaseUI.Parity.Tests.Client;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins <see cref="FixtureRegistry"/>'s id surface. It is pure reflection, so it needs
/// neither a browser nor a server.
/// </summary>
public sealed class FixtureRegistryTests
{
    [Fact]
    public void RegistersFixturesByKebabCasedId()
        => FixtureRegistry.Ids.ShouldContain("switch/hero");

    [Fact]
    public void ExcludesCompilerGeneratedNestedTypes()
    {
        // CaptureProbe carries a non-capturing lambda, so the Razor compiler emits a
        // nested closure class whose FullName still splits into two segments.
        FixtureRegistry.Ids.ShouldContain("harness/capture-probe");
        FixtureRegistry.Ids.ShouldAllBe(id => !id.Contains('+'));
    }
}
