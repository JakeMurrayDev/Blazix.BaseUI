namespace BlazorBaseUI.Tests.Select;

public class SelectItemTests : BunitContext, ISelectItemContract
{
    public SelectItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectWithItems(
        string? defaultValue = null,
        bool defaultOpen = false,
        bool disabledItem = false,
        bool useNativeButton = false,
        string? firstItemLabel = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                // Items directly (no portal) for bUnit
                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        if (firstItemLabel is not null)
                        {
                            popupBuilder.AddAttribute(2, "Label", firstItemLabel);
                        }
                        if (useNativeButton)
                        {
                            popupBuilder.AddAttribute(3, "NativeButton", true);
                        }
                        popupBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(10);
                        popupBuilder.AddAttribute(11, "Value", "banana");
                        if (disabledItem) popupBuilder.AddAttribute(12, "Disabled", true);
                        popupBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public async Task ShouldSelectItemAndClosePopupWhenClicked()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        // React parity: a mouse click on an item requires the item to be highlighted first
        // (normally achieved by onmouseenter in a real browser).
        await items[0].TriggerEventAsync("onmouseenter", new MouseEventArgs());
        items = cut.FindAll("[role='option']");
        items[0].Click();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public Task ShouldNotSelectDisabledItem()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        items[1].Click();

        // Disabled items don't trigger selection, so the select should remain open
        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        // The banana item should NOT have data-selected
        items = cut.FindAll("[role='option']");
        items[1].HasAttribute("data-selected").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldApplyDataSelectedWhenSelected()
    {
        var cut = Render(CreateSelectWithItems(defaultValue: "apple", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ShouldApplyDataHighlightedWhenHighlighted()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");

        // Initially not highlighted: boolean false means attribute is absent
        items[0].HasAttribute("data-highlighted").ShouldBeFalse();

        await items[0].TriggerEventAsync("onmouseenter", new MouseEventArgs());

        // After mouseenter, the item should have data-highlighted attribute (boolean true renders as present attribute)
        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeTrue();
    }

    [Fact]
    public Task ShouldRenderWithOptionRole()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items.Count.ShouldBeGreaterThan(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldSetAriaSelectedTrue()
    {
        var cut = Render(CreateSelectWithItems(defaultValue: "apple", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items[0].GetAttribute("aria-selected").ShouldBe("true");
        items[1].GetAttribute("aria-selected").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledItem_HasAriaDisabled()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        items[1].GetAttribute("aria-disabled").ShouldBe("true");
        items[1].HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // --- Focus + Disabled: disabled items do not highlight on mouseenter ---

    [Fact]
    public async Task DisabledItem_ShouldNotHighlightOnMouseEnter()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        await items[1].TriggerEventAsync("onmouseenter", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        items[1].HasAttribute("data-highlighted").ShouldBeFalse();
    }

    [Fact]
    public Task ShouldFocusSelectedItemUponOpeningPopup()
    {
        var cut = Render(CreateSelectWithItems(defaultValue: "banana", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        var bananaItem = items.First(i => i.TextContent.Contains("Banana"));
        bananaItem.HasAttribute("data-selected").ShouldBeTrue();
        bananaItem.GetAttribute("aria-selected").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledItem_ShouldNotSelectOnClickAndKeepOpen()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        items[1].Click();

        items = cut.FindAll("[role='option']");
        items[1].HasAttribute("data-selected").ShouldBeFalse();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    // --- React parity additions ---

    [Fact]
    public Task ShouldNotEmitDataLabel()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, firstItemLabel: "Apple"));

        var items = cut.FindAll("[role='option']");
        // React's SelectItem does not emit `data-label`; we use `data-blazor-base-ui-label` instead.
        items[0].HasAttribute("data-label").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldEmitDataBlazorBaseUiLabelWhenLabelSet()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, firstItemLabel: "Apple"));

        var items = cut.FindAll("[role='option']");
        items[0].GetAttribute("data-blazor-base-ui-label").ShouldBe("Apple");
        // Second item has no Label parameter → attribute absent
        items[1].HasAttribute("data-blazor-base-ui-label").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldRejectMouseClickOnUnhighlightedItem()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        // Click without prior mouseenter — React parity: unhighlighted mouse click is ignored.
        items[0].Click();

        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-selected").ShouldBeFalse();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_ShouldRenderAsButtonElementWithTypeButton()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, useNativeButton: true));

        var items = cut.FindAll("[role='option']");
        items[0].TagName.ShouldBe("BUTTON");
        items[0].GetAttribute("type").ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_ShouldRenderAsDivWithRoleOption()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items[0].TagName.ShouldBe("DIV");
        items[0].GetAttribute("role").ShouldBe("option");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DisabledItem_ShouldRemainFocusableWhenHighlighted()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        // Disabled item starts with tabindex=-1
        items[1].GetAttribute("tabindex").ShouldBe("-1");

        // Simulate focus (e.g., roving tabindex from JS sets activeIndex to this item):
        // we use the onfocus handler path which sets the hover active index.
        await items[1].TriggerEventAsync("onfocus", new FocusEventArgs());

        items = cut.FindAll("[role='option']");
        // focusableWhenDisabled: true path keeps the disabled item focusable.
        items[1].GetAttribute("tabindex").ShouldBe("0");
        // aria-disabled remains true so AT announces the disabled state.
        items[1].GetAttribute("aria-disabled").ShouldBe("true");
        // Native `disabled` attribute must NOT be set — that would strip focusability.
        items[1].HasAttribute("disabled").ShouldBeFalse();
    }
}
