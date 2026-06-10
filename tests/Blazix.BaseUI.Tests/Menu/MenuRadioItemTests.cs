namespace Blazix.BaseUI.Tests.Menu;

public class MenuRadioItemTests : BunitContext, IMenuRadioItemContract
{
    public MenuRadioItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateRadioItemInRoot(
        bool defaultOpen = true,
        object? defaultValue = null,
        bool groupDisabled = false,
        bool itemDisabled = false,
        string? label = null,
        RenderFragment<RenderProps<MenuRadioItemState>>? render = null,
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
                        popupBuilder.OpenComponent<MenuRadioGroup>(0);
                        var groupAttrIndex = 1;

                        if (defaultValue is not null)
                            popupBuilder.AddAttribute(groupAttrIndex++, "DefaultValue", defaultValue);
                        if (groupDisabled)
                            popupBuilder.AddAttribute(groupAttrIndex++, "Disabled", true);

                        popupBuilder.AddAttribute(groupAttrIndex++, "ChildContent", (RenderFragment)(groupBuilder =>
                        {
                            groupBuilder.OpenComponent<MenuRadioItem>(0);
                            var firstItemAttrIndex = 1;
                            groupBuilder.AddAttribute(firstItemAttrIndex++, "Value", "option1");
                            if (itemDisabled)
                                groupBuilder.AddAttribute(firstItemAttrIndex++, "Disabled", true);
                            if (nativeButton)
                                groupBuilder.AddAttribute(firstItemAttrIndex++, "NativeButton", true);
                            if (label is not null)
                                groupBuilder.AddAttribute(firstItemAttrIndex++, "Label", label);
                            if (render is not null)
                                groupBuilder.AddAttribute(firstItemAttrIndex++, "Render", render);
                            groupBuilder.AddAttribute(firstItemAttrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 1")));
                            groupBuilder.CloseComponent();

                            groupBuilder.OpenComponent<MenuRadioItem>(4);
                            groupBuilder.AddAttribute(5, "Value", "option2");
                            groupBuilder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 2")));
                            groupBuilder.CloseComponent();
                        }));
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
    public Task HasRoleMenuitemradio()
    {
        var cut = Render(CreateRadioItemInRoot());

        var item = cut.Find("[role='menuitemradio']");
        item.GetAttribute("role")!.ShouldBe("menuitemradio");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuRadioItemState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateRadioItemInRoot(render: renderAsSpan));

        var item = cut.Find("span[role='menuitemradio']");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsNativeButtonMode()
    {
        var cut = Render(CreateRadioItemInRoot(nativeButton: true));

        var item = cut.Find("button[role='menuitemradio']");
        item.GetAttribute("type")!.ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedWhenSelected()
    {
        var cut = Render(CreateRadioItemInRoot(defaultValue: "option1"));

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].GetAttribute("aria-checked")!.ShouldBe("true");
        items[1].GetAttribute("aria-checked")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataCheckedWhenSelected()
    {
        var cut = Render(CreateRadioItemInRoot(defaultValue: "option1"));

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].HasAttribute("data-checked").ShouldBeTrue();
        items[1].HasAttribute("data-unchecked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectsOnClick()
    {
        object? selectedValue = "option1";

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
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
                        popupBuilder.OpenComponent<MenuRadioGroup>(0);
                        popupBuilder.AddAttribute(1, "DefaultValue", "option1");
                        popupBuilder.AddAttribute(2, "OnValueChange", EventCallback.Factory.Create<MenuRadioGroupChangeEventArgs>(this, args =>
                        {
                            selectedValue = args.Value;
                        }));
                        popupBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(groupBuilder =>
                        {
                            groupBuilder.OpenComponent<MenuRadioItem>(0);
                            groupBuilder.AddAttribute(1, "Value", "option1");
                            groupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 1")));
                            groupBuilder.CloseComponent();

                            groupBuilder.OpenComponent<MenuRadioItem>(3);
                            groupBuilder.AddAttribute(4, "Value", "option2");
                            groupBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Option 2")));
                            groupBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].GetAttribute("aria-checked")!.ShouldBe("true");

        items[1].Click();

        // Verify that the OnValueChange event was fired with the new value
        selectedValue.ShouldBe("option2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InheritsDisabledFromGroup()
    {
        var cut = Render(CreateRadioItemInRoot(groupDisabled: true));

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].HasAttribute("data-disabled").ShouldBeTrue();
        items[0].GetAttribute("aria-disabled")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersLabelAsDataAttribute()
    {
        var cut = Render(CreateRadioItemInRoot(label: "My Radio"));

        var item = cut.Find("[role='menuitemradio']");
        item.GetAttribute("data-label")!.ShouldBe("My Radio");

        return Task.CompletedTask;
    }
}
