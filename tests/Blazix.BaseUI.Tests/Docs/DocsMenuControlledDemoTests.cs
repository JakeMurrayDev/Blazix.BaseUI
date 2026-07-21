using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.Docs;

public class DocsMenuControlledDemoTests : BunitContext
{
    public DocsMenuControlledDemoTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    [Fact]
    public void CssControlledPlaybackButtonShowsOnlyPlaybackItems()
    {
        var cut = Render<Blazix.BaseUI.Docs.Client.Components.Demos.Menu.DetachedTriggersControlled.Css.MenuDetachedTriggersControlledCss>();

        AssertControlledPlaybackItems(cut);
    }

    [Fact]
    public void TailwindControlledPlaybackButtonShowsOnlyPlaybackItems()
    {
        var cut = Render<Blazix.BaseUI.Docs.Client.Components.Demos.Menu.DetachedTriggersControlled.Tailwind.MenuDetachedTriggersControlledTailwind>();

        AssertControlledPlaybackItems(cut);
    }

    private static void AssertControlledPlaybackItems<TComponent>(IRenderedComponent<TComponent> cut)
        where TComponent : IComponent
    {
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Open playback (controlled)", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.ShouldContain(">Play<");
            markup.ShouldContain("Add to queue");
            markup.ShouldNotContain("Add to library");
            markup.ShouldNotContain("Menu payload unavailable");
        });
    }
}
