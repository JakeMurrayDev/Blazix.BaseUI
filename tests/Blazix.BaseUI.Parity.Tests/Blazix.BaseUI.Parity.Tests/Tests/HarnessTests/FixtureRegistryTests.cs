using System.Reflection;
using Blazix.BaseUI.Parity.Tests.Client;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;
using CollisionFixtures = Blazix.BaseUI.Parity.Tests.Client.Fixtures.Duplicate;
using UnsafeFixtures = Blazix.BaseUI.Parity.Tests.Client.Fixtures._Unsafe;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins <see cref="FixtureRegistry"/>'s id surface. It is pure reflection, so it needs
/// neither a browser nor a server.
/// </summary>
public sealed class FixtureRegistryTests
{
    [Fact]
    public void RegistersEveryMilestoneFixtureByKebabCasedId()
    {
        var expected = MilestoneFixtureCatalog.Ids
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        FixtureRegistry.Ids
            .Where(item => !item.StartsWith("harness/", StringComparison.Ordinal))
            .OrderBy(item => item, StringComparer.Ordinal)
            .ShouldBe(expected);
    }

    [Fact]
    public void ExcludesCompilerGeneratedNestedTypes()
    {
        // CaptureProbe carries a non-capturing lambda, so the Razor compiler emits a
        // nested closure class whose FullName still splits into two segments.
        FixtureRegistry.Ids.ShouldContain("harness/capture-probe");
        FixtureRegistry.Ids.ShouldAllBe(id => !id.Contains('+'));
    }

    [Fact]
    public void RejectsTypesThatCollapseToTheSameFixtureId()
    {
        var types = new[]
        {
            typeof(CollisionFixtures.Hero),
            typeof(CollisionFixtures.hero)
        };

        var buildIndex = typeof(FixtureRegistry).GetMethod(
            "BuildIndex",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildIndex.ShouldNotBeNull();
        var invocation = Should.Throw<TargetInvocationException>(() =>
            buildIndex.Invoke(null, [types]));
        var collision = invocation.InnerException.ShouldBeOfType<InvalidOperationException>();
        collision.Message.ShouldContain("duplicate fixture id", Case.Insensitive);
        collision.Message.ShouldContain(typeof(CollisionFixtures.Hero).FullName!);
        collision.Message.ShouldContain(typeof(CollisionFixtures.hero).FullName!);
    }

    [Fact]
    public void RejectsReflectedTypesThatProduceUnsafeFixtureIds()
    {
        var failure = InvokeBuildIndex([typeof(UnsafeFixtures._Hero)]);

        failure.Message.ShouldContain("safe lowercase fixture id", Case.Insensitive);
        failure.Message.ShouldContain("_-unsafe/_-hero");
    }

    private static InvalidOperationException InvokeBuildIndex(Type[] types)
    {
        var buildIndex = typeof(FixtureRegistry).GetMethod(
            "BuildIndex",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildIndex.ShouldNotBeNull();
        var invocation = Should.Throw<TargetInvocationException>(() =>
            buildIndex.Invoke(null, [types]));
        return invocation.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
}
