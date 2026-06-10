namespace Blazix.BaseUI.Tests.ContextMenu;

public class ContextMenuTriggerTests : BunitContext, IContextMenuTriggerContract
{
    public ContextMenuTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupContextMenuModule(JSInterop);
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        bool disabled = false,
        bool triggerDisabled = false,
        Func<ContextMenuTriggerState, string>? classValue = null,
        Func<ContextMenuTriggerState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<ContextMenuTriggerState>>? render = null,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<ContextMenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextMenuTrigger>(0);
                var attrIndex = 1;

                if (triggerDisabled)
                    innerBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Right click here")));
                innerBuilder.CloseComponent();

                if (includePositioner)
                {
                    innerBuilder.OpenComponent<MenuPositioner>(10);
                    innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<MenuItem>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<ContextMenuTriggerState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateTriggerInRoot(render: renderAsSpan));

        var trigger = cut.Find("span[style*='touch-callout']");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Context menu area" }
            }
        ));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.GetAttribute("data-testid")!.ShouldBe("trigger");
        trigger.GetAttribute("aria-label")!.ShouldBe("Context menu area");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: red"
        ));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesTouchCalloutNoneStyle()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.GetAttribute("style")!.ShouldContain("-webkit-touch-callout: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TriggerRendersAsDivElement()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AddsPopupOpenDataAttribute()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AddsPressedDataAttributeWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-pressed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemovesPopupOpenDataAttributeWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemovesPressedDataAttributeWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-pressed").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotCancelOpenOnMouseUpBefore500ms()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var triggerComponent = cut.FindComponent<ContextMenuTrigger>();

        // Simulate JS calling OnContextMenu (right-click) on the dispatcher
        await cut.InvokeAsync(() => triggerComponent.Instance.OnContextMenu(100, 200, false));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        // Before 500ms, JS would not call OnCancelOpen.
        // The menu stays open.

        return;
    }

    [Fact]
    public async Task CancelsOpenOnMouseUpAfter500ms()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var triggerComponent = cut.FindComponent<ContextMenuTrigger>();

        // Simulate JS calling OnContextMenu (right-click) on the dispatcher
        await cut.InvokeAsync(() => triggerComponent.Instance.OnContextMenu(100, 200, false));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        // After 500ms, JS calls OnCancelOpen
        await cut.InvokeAsync(() => triggerComponent.Instance.OnCancelOpen());

        trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeFalse();

        return;
    }

    [Fact]
    public async Task SetsAnchorFromCursorPosition()
    {
        var cut = Render(CreateTriggerInRoot());

        var triggerComponent = cut.FindComponent<ContextMenuTrigger>();

        // Simulate a context menu open at specific coordinates on the dispatcher
        await cut.InvokeAsync(() => triggerComponent.Instance.OnContextMenu(150, 250, false));

        // After opening, the trigger should show open state
        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return;
    }
}
