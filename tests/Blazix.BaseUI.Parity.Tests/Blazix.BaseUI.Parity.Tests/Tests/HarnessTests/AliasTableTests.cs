using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the alias expansion rules the manifest's step selectors rely on.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class AliasTableTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>
{
    // Three same-role items so an index has something to choose between: a one-item
    // page would let any expansion that merely parses look correct.
    private const string MenuMarkup =
        """
        <ul role="menu">
          <li role="menuitem">Cut</li>
          <li role="menuitem">Copy</li>
          <li role="menuitem">Paste</li>
        </ul>
        """;

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

        // Playwright's nth-match engine takes the selector list as its first argument;
        // the `[role=menuitem]:nth-match(3)` spelling is rejected at query time with
        // "nth-match engine expects non-empty selector list and an index argument".
        table.Expand("menu", "@item(2)").ShouldBe(":nth-match([role=menuitem], 3)");
    }

    [Theory]
    [InlineData(0, "Cut")]
    [InlineData(1, "Copy")]
    [InlineData(2, "Paste")]
    public async Task IndexedAliasResolvesInABrowser(int index, string expected)
    {
        // Driven through a live page rather than asserted as a string: the previous
        // spelling was self-consistent between manifest and test and still threw the
        // moment Playwright parsed it, so only a real query proves the syntax.
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetContentAsync(MenuMarkup);

        var expanded = AliasTable.Load().Expand("menu", $"@item({index})");
        var locator = page.Locator(expanded);

        // Asserted before the text: an expansion that matched every item would still
        // satisfy the assertion below through Playwright's strictness-free first match.
        (await locator.CountAsync()).ShouldBe(1);
        (await locator.InnerTextAsync()).ShouldBe(expected);
    }
}
