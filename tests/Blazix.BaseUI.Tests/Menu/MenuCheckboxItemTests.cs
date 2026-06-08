namespace Blazix.BaseUI.Tests.Menu;

public class MenuCheckboxItemTests : BunitContext, IMenuCheckboxItemContract
{
    public MenuCheckboxItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateCheckboxItemInRoot(
        bool defaultOpen = true,
        bool? itemChecked = null,
        bool defaultChecked = false,
        bool itemDisabled = false,
        bool closeOnClick = false,
        string? label = null,
        string? id = null,
        RenderFragment<RenderProps<MenuCheckboxItemState>>? render = null,
        Func<MenuCheckboxItemState, string?>? classValue = null,
        Func<MenuCheckboxItemState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        EventCallback<MenuCheckboxItemChangeEventArgs>? onCheckedChange = null,
        bool nativeButton = false)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuCheckboxItem>(0);
                        var attrIndex = 1;

                        if (itemChecked.HasValue)
                            popupBuilder.AddAttribute(attrIndex++, "Checked", itemChecked.Value);
                        popupBuilder.AddAttribute(attrIndex++, "DefaultChecked", defaultChecked);
                        if (itemDisabled)
                            popupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                        if (nativeButton)
                            popupBuilder.AddAttribute(attrIndex++, "NativeButton", true);
                        popupBuilder.AddAttribute(attrIndex++, "CloseOnClick", closeOnClick);
                        if (label is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Label", label);
                        if (id is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Id", id);
                        if (render is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Render", render);
                        if (classValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                        if (styleValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            popupBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                        if (onCheckedChange.HasValue)
                            popupBuilder.AddAttribute(attrIndex++, "OnCheckedChange", onCheckedChange.Value);
                        popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Checkbox Item")));
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
    public Task HasRoleMenuitemcheckbox()
    {
        var cut = Render(CreateCheckboxItemInRoot());

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("role")!.ShouldBe("menuitemcheckbox");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuCheckboxItemState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCheckboxItemInRoot(render: renderAsSpan));

        var item = cut.Find("span[role='menuitemcheckbox']");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsNativeButtonMode()
    {
        var cut = Render(CreateCheckboxItemInRoot(nativeButton: true));

        var item = cut.Find("button[role='menuitemcheckbox']");
        item.GetAttribute("type")!.ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedFalseWhenUnchecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedTrueWhenChecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.HasAttribute("data-checked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUnchecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.HasAttribute("data-unchecked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsCheckedParameter()
    {
        var cut = Render(CreateCheckboxItemInRoot(itemChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("true");

        // Clicking should not change the controlled value
        item.Click();

        // In controlled mode, the value stays the same until parent updates
        item.GetAttribute("aria-checked")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultChecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TogglesOnClick()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("false");

        item.Click();

        item.GetAttribute("aria-checked")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnCheckedChange()
    {
        var invoked = false;
        var receivedChecked = false;

        var cut = Render(CreateCheckboxItemInRoot(
            defaultChecked: false,
            onCheckedChange: EventCallback.Factory.Create<MenuCheckboxItemChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedChecked = args.Checked;
            })
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.Click();

        invoked.ShouldBeTrue();
        receivedChecked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsCancelInOnCheckedChange()
    {
        var cut = Render(CreateCheckboxItemInRoot(
            defaultChecked: false,
            onCheckedChange: EventCallback.Factory.Create<MenuCheckboxItemChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("false");

        item.Click();

        // State should not change because we canceled
        item.GetAttribute("aria-checked")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersLabelAsDataAttribute()
    {
        var cut = Render(CreateCheckboxItemInRoot(label: "My Checkbox"));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("data-label")!.ShouldBe("My Checkbox");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateCheckboxItemInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "checkbox-item" },
                { "aria-label", "Toggle setting" }
            }
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("data-testid")!.ShouldBe("checkbox-item");
        item.GetAttribute("aria-label")!.ShouldBe("Toggle setting");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateCheckboxItemInRoot(
            classValue: _ => "my-checkbox-class"
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("class")!.ShouldContain("my-checkbox-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateCheckboxItemInRoot(
            styleValue: _ => "color: red"
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDefaultId()
    {
        var cut = Render(CreateCheckboxItemInRoot());

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesProvidedId()
    {
        var cut = Render(CreateCheckboxItemInRoot(id: "custom-checkbox-id"));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("id")!.ShouldBe("custom-checkbox-id");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ResolvesIdFromAdditionalAttributes()
    {
        var cut = Render(CreateCheckboxItemInRoot(
            additionalAttributes: new Dictionary<string, object> { ["id"] = "checkbox-from-attrs" }));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("id")!.ShouldBe("checkbox-from-attrs");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateCheckboxItemInRoot(itemDisabled: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-disabled")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateCheckboxItemInRoot(itemDisabled: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledItemDoesNotToggle()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false, itemDisabled: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked")!.ShouldBe("false");

        item.Click();

        item.GetAttribute("aria-checked")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeIncludesReason()
    {
        MenuCheckboxItemChangeReason? receivedReason = null;

        var cut = Render(CreateCheckboxItemInRoot(
            defaultChecked: false,
            onCheckedChange: EventCallback.Factory.Create<MenuCheckboxItemChangeEventArgs>(this, args =>
            {
                receivedReason = args.Reason;
            })
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.Click();

        receivedReason.ShouldNotBeNull();
        receivedReason.ShouldBe(MenuCheckboxItemChangeReason.ItemPress);

        return Task.CompletedTask;
    }
}
