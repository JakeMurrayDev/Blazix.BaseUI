using Blazix.BaseUI.Playwright.Tests.Infrastructure;

namespace Blazix.BaseUI.Playwright.Tests.Tests;

public class RenderModeExtensionsTests
{
    [Fact]
    public void WithShowNestedDrawerUsesDrawerQueryParameter()
    {
        var url = new TestPageUrlBuilder("http://localhost", "/tests/drawer", TestRenderMode.Server)
            .WithShowNestedDrawer(true)
            .Build();

        Assert.Contains("showNestedDrawer=true", url, StringComparison.Ordinal);
        Assert.DoesNotContain("showNestedDialog", url, StringComparison.Ordinal);
    }
}
