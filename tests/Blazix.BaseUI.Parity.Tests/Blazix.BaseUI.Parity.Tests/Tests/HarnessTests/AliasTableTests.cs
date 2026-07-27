using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the alias expansion rules the manifest's step selectors rely on.
/// </summary>
public sealed class AliasTableTests
{
    [Fact]
    public void ExpandsComponentScopedAlias()
    {
        var table = AliasTable.Load();

        table.Expand("popover", "@trigger").ShouldBe("[aria-haspopup],[aria-expanded]");
    }

    [Fact]
    public void PassesThroughRawSelectors()
    {
        var table = AliasTable.Load();

        table.Expand("popover", "button.foo").ShouldBe("button.foo");
    }

    [Fact]
    public void ExpandsIndexedAlias()
    {
        var table = AliasTable.Load();

        table.Expand("menu", "@item(2)").ShouldBe("[role=menuitem]:nth-match(3)");
    }
}
