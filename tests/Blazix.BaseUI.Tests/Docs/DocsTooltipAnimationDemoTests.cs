namespace Blazix.BaseUI.Tests.Docs;

public sealed class DocsTooltipAnimationDemoTests
{
    [Fact]
    public void CssViewportDemoSizesPositionerAndPopupFromMeasuredVariables()
    {
        var demo = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Tooltip/DetachedTriggersFull/Css/TooltipDetachedTriggersFullCss.razor");
        var css = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/tooltip.css");

        demo.ShouldContain("class=\"blx-tooltip-positioner\"");
        demo.ShouldContain("blx-tooltip-popup--viewport");
        css.ShouldContain("width: var(--positioner-width);");
        css.ShouldContain("height: var(--positioner-height);");
        css.ShouldContain("width: var(--popup-width, auto);");
        css.ShouldContain("height: var(--popup-height, auto);");
    }

    [Fact]
    public void TailwindViewportDemoSizesPositionerAndPopupFromMeasuredVariables()
    {
        var demo = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Tooltip/DetachedTriggersFull/Tailwind/TooltipDetachedTriggersFullTailwind.razor");

        demo.ShouldContain("w-[var(--positioner-width)]");
        demo.ShouldContain("h-[var(--positioner-height)]");
        demo.ShouldContain("w-[var(--popup-width,auto)]");
        demo.ShouldContain("h-[var(--popup-height,auto)]");
    }

    [Fact]
    public void ViewportInteropObservesRejectedServerCallbacks()
    {
        var javascript = ReadRepoFile("src/Blazix.BaseUI/wwwroot/blazix-baseui-tooltip-viewport.js");

        javascript.ShouldContain("invokeMethodAsync('OnTransitionStarted', direction).catch");
        javascript.ShouldContain("invokeMethodAsync('OnTransitionEnded').catch");
    }

    [Fact]
    public void FloatingPositioningUsesViewportDimensionsForRootAdaptiveOrigin()
    {
        var javascript = ReadRepoFile("src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js");

        javascript.ShouldContain("offsetParent === document.body || offsetParent === document.documentElement");
        javascript.ShouldContain("document.documentElement.clientHeight");
        javascript.ShouldContain("document.documentElement.clientWidth");
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Blazix.BaseUI.slnx")))
            directory = directory.Parent;

        directory.ShouldNotBeNull();
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
