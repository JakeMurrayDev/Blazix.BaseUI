using BlazorBaseUI.Demo.Client.Shared;
using BlazorBaseUI.Demo.Client.Shared.Sections;
using BlazorBaseUI.Tooltip;

namespace BlazorBaseUI.Tests.Demo;

public class DemoShellTests : BunitContext
{
    public DemoShellTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTabsModule(JSInterop);
        JsInteropSetup.SetupTooltipModule(JSInterop);
        JsInteropSetup.SetupFloatingDelayGroupModule(JSInterop);
    }

    [Fact]
    public void ComponentShowcase_RendersFluentHeaderAndRenderModeTabs()
    {
        var cut = Render<ComponentShowcase>(parameters => parameters
            .Add(p => p.ComponentName, "Button")
            .Add(p => p.Description, "A command surface primitive.")
            .Add(p => p.BaseRoute, "/button")
            .Add(p => p.IsWasmMode, false)
            .AddChildContent("<p>Example body</p>"));

        cut.Find(".demo-showcase").ShouldNotBeNull();
        cut.Find(".demo-showcase__header").ShouldNotBeNull();
        cut.Find(".demo-render-tabs").GetAttribute("role").ShouldBe("tablist");
        cut.FindAll(".demo-render-tab").Count.ShouldBe(2);
        cut.Find(".demo-chip").TextContent.ShouldContain("InteractiveServer");
        cut.Markup.ShouldContain("Example body");
    }

    [Fact]
    public void ComponentShowcase_RendersWasmModeAsSelected()
    {
        var cut = Render<ComponentShowcase>(parameters => parameters
            .Add(p => p.ComponentName, "Switch")
            .Add(p => p.Description, "A boolean input primitive.")
            .Add(p => p.BaseRoute, "/switch")
            .Add(p => p.IsWasmMode, true));

        var wasmTab = cut.Find("[data-demo-render-mode='wasm']");
        wasmTab.GetAttribute("aria-selected").ShouldBe("true");
        cut.Find(".demo-chip").TextContent.ShouldContain("InteractiveWebAssembly");
    }

    [Fact]
    public void DemoSection_RendersFluentPreviewAndCodeDisclosure()
    {
        var cut = Render<DemoSection>(parameters => parameters
            .Add(p => p.Title, "Basic Usage")
            .Add(p => p.Description, "Shows the default surface.")
            .Add(p => p.CodeSnippet, "<Button>Save</Button>")
            .AddChildContent("<button>Save</button>"));

        cut.Find(".demo-section").ShouldNotBeNull();
        cut.Find(".demo-section__header").TextContent.ShouldContain("Basic Usage");
        cut.Find(".demo-section__preview").TextContent.ShouldContain("Save");
        cut.Find(".demo-disclosure").ShouldNotBeNull();
        cut.Find(".demo-section__code").TextContent.ShouldContain("<Button>Save</Button>");
    }

    [Fact]
    public void TooltipSection_DefaultNotHoverableExampleDisablesHoverablePopup()
    {
        var cut = Render<TooltipSection>(parameters => parameters
            .Add(p => p.IsWasmMode, false));

        var defaultNotHoverableRoot = cut
            .FindComponents<TooltipRoot>()
            .First(root => root.Markup.Contains("Default (Not Hoverable)", StringComparison.Ordinal));
        var hoverableRoot = cut
            .FindComponents<TooltipRoot>()
            .First(root => root.Markup.Contains("Hoverable Popup", StringComparison.Ordinal));

        defaultNotHoverableRoot.Instance.DisableHoverablePopup.ShouldBeTrue();
        hoverableRoot.Instance.DisableHoverablePopup.ShouldBeFalse();
    }

    [Fact]
    public void TooltipSection_DoesNotRenderDuplicateElementIds()
    {
        var cut = Render<TooltipSection>(parameters => parameters
            .Add(p => p.IsWasmMode, false));

        var duplicateIds = cut.FindAll("[id]")
            .Select(element => element.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        duplicateIds.ShouldBeEmpty();
    }
}
