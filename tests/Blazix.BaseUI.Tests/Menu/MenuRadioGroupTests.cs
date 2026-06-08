namespace Blazix.BaseUI.Tests.Menu;

public class MenuRadioGroupTests : BunitContext, IMenuRadioGroupContract
{
    public MenuRadioGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateRadioGroupInRoot(
        bool defaultOpen = true,
        object? groupValue = null,
        object? defaultValue = null,
        bool groupDisabled = false,
        RenderFragment<RenderProps<MenuRadioGroupState>>? render = null,
        EventCallback<object?>? valueChanged = null,
        EventCallback<MenuRadioGroupChangeEventArgs>? onValueChange = null)
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
                        var attrIndex = 1;

                        if (groupValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Value", groupValue);
                        if (defaultValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
                        if (groupDisabled)
                            popupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                        if (render is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Render", render);
                        if (valueChanged.HasValue)
                            popupBuilder.AddAttribute(attrIndex++, "ValueChanged", valueChanged.Value);
                        if (onValueChange.HasValue)
                            popupBuilder.AddAttribute(attrIndex++, "OnValueChange", onValueChange.Value);

                        popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(groupBuilder =>
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
        };
    }

    [Fact]
    public Task HasRoleGroup()
    {
        var cut = Render(CreateRadioGroupInRoot());

        var group = cut.Find("[role='group']");
        group.GetAttribute("role")!.ShouldBe("group");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuRadioGroupState>> renderAsSection = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateRadioGroupInRoot(render: renderAsSection));

        var group = cut.Find("section[role='group']");
        group.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToRadioItems()
    {
        var cut = Render(CreateRadioGroupInRoot(defaultValue: "option1"));

        // The first radio item should be checked
        var items = cut.FindAll("[role='menuitemradio']");
        items.Count.ShouldBe(2);

        items[0].GetAttribute("aria-checked")!.ShouldBe("true");
        items[1].GetAttribute("aria-checked")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsValueParameter()
    {
        object? currentValue = "option1";

        var cut = Render(CreateRadioGroupInRoot(
            groupValue: currentValue,
            valueChanged: EventCallback.Factory.Create<object?>(this, val => currentValue = val)
        ));

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].GetAttribute("aria-checked")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultValue()
    {
        var cut = Render(CreateRadioGroupInRoot(defaultValue: "option2"));

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].GetAttribute("aria-checked")!.ShouldBe("false");
        items[1].GetAttribute("aria-checked")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnValueChange()
    {
        var invoked = false;
        object? receivedValue = null;

        var cut = Render(CreateRadioGroupInRoot(
            defaultValue: "option1",
            onValueChange: EventCallback.Factory.Create<MenuRadioGroupChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedValue = args.Value;
            })
        ));

        var items = cut.FindAll("[role='menuitemradio']");
        items[1].Click(); // Click option2

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe("option2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsCancelInOnValueChange()
    {
        var cut = Render(CreateRadioGroupInRoot(
            defaultValue: "option1",
            onValueChange: EventCallback.Factory.Create<MenuRadioGroupChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var items = cut.FindAll("[role='menuitemradio']");
        items[0].GetAttribute("aria-checked")!.ShouldBe("true");
        items[1].GetAttribute("aria-checked")!.ShouldBe("false");

        items[1].Click(); // Try to select option2

        // Selection should not change because we canceled
        items[0].GetAttribute("aria-checked")!.ShouldBe("true");
        items[1].GetAttribute("aria-checked")!.ShouldBe("false");

        return Task.CompletedTask;
    }
}
