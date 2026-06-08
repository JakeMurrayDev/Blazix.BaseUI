namespace Blazix.BaseUI.Tests.Menu;

public class MenuRadioItemIndicatorTests : BunitContext, IMenuRadioItemIndicatorContract
{
    public MenuRadioItemIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateRadioItemWithIndicator(
        bool defaultOpen = true,
        object? defaultValue = null,
        bool groupDisabled = false,
        bool keepMounted = false,
        Func<MenuRadioItemIndicatorState, string?>? indicatorClassValue = null,
        Func<MenuRadioItemIndicatorState, string?>? indicatorStyleValue = null,
        IReadOnlyDictionary<string, object>? indicatorAdditionalAttributes = null,
        RenderFragment<RenderProps<MenuRadioItemIndicatorState>>? indicatorRender = null)
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
                            groupBuilder.AddAttribute(1, "Value", "option1");
                            groupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                            {
                                itemBuilder.OpenComponent<MenuRadioItemIndicator>(0);
                                var indicatorAttrIndex = 1;

                                if (keepMounted)
                                    itemBuilder.AddAttribute(indicatorAttrIndex++, "KeepMounted", true);
                                if (indicatorClassValue is not null)
                                    itemBuilder.AddAttribute(indicatorAttrIndex++, "ClassValue", indicatorClassValue);
                                if (indicatorStyleValue is not null)
                                    itemBuilder.AddAttribute(indicatorAttrIndex++, "StyleValue", indicatorStyleValue);
                                if (indicatorAdditionalAttributes is not null)
                                    itemBuilder.AddAttribute(indicatorAttrIndex++, "AdditionalAttributes", indicatorAdditionalAttributes);
                                if (indicatorRender is not null)
                                    itemBuilder.AddAttribute(indicatorAttrIndex++, "Render", indicatorRender);

                                itemBuilder.CloseComponent();
                            }));
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

    private RenderFragment CreateIndicatorWithContext(
        MenuRadioItemContext context,
        bool keepMounted = false,
        Func<MenuRadioItemIndicatorState, string?>? classValue = null,
        Func<MenuRadioItemIndicatorState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<CascadingValue<MenuRadioItemContext>>(0);
            builder.AddAttribute(1, "Value", context);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuRadioItemIndicator>(0);
                var attrIndex = 1;

                if (keepMounted)
                    innerBuilder.AddAttribute(attrIndex++, "KeepMounted", true);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering tests

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuRadioItemIndicatorState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorRender: renderAsDiv,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorAdditionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "indicator" },
                { "aria-label", "Radio indicator" }
            }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-label")!.ShouldBe("Radio indicator");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorClassValue: _ => "indicator-class",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("class")!.ShouldContain("indicator-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorStyleValue: _ => "background: green",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("style")!.ShouldContain("background: green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHidden()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-hidden")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    // Visibility tests

    [Fact]
    public Task DoesNotRenderWhenUnchecked()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicators = cut.FindAll("[data-testid='indicator']");
        indicators.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenChecked()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // keepMounted tests

    [Fact]
    public Task KeepsIndicatorMountedWhenUnchecked()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsIndicatorMountedWhenChecked()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // Data attribute tests

    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            defaultValue: "option1",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-checked").ShouldBeTrue();
        indicator.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUncheckedAndKeepMounted()
    {
        var cut = Render(CreateRadioItemWithIndicator(
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-unchecked").ShouldBeTrue();
        indicator.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var context = new MenuRadioItemContext
        {
            Checked = true,
            Highlighted = false,
            Disabled = true
        };

        var cut = Render(CreateIndicatorWithContext(
            context,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderDataHighlighted()
    {
        var context = new MenuRadioItemContext
        {
            Checked = true,
            Highlighted = true,
            Disabled = false
        };

        var cut = Render(CreateIndicatorWithContext(
            context,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-highlighted").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Context tests

    [Fact]
    public Task ReceivesStateFromRadioItemContext()
    {
        var context = new MenuRadioItemContext
        {
            Checked = true,
            Highlighted = true,
            Disabled = true
        };

        var cut = Render(CreateIndicatorWithContext(
            context,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-checked").ShouldBeTrue();
        indicator.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandlesNullContext()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRadioItemIndicator>(0);
            builder.AddAttribute(1, "KeepMounted", true);
            builder.AddAttribute(2, "AdditionalAttributes",
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator" } });
            builder.CloseComponent();
        });

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();
        indicator.HasAttribute("data-unchecked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // State tests

    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        MenuRadioItemIndicatorState? capturedState = null;

        var context = new MenuRadioItemContext
        {
            Checked = true,
            Highlighted = false,
            Disabled = true
        };

        var cut = Render(CreateIndicatorWithContext(
            context,
            classValue: state =>
            {
                capturedState = state;
                return "indicator-class";
            },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Checked.ShouldBeTrue();
        capturedState.Value.Disabled.ShouldBeTrue();
        capturedState.Value.Highlighted.ShouldBeFalse();

        return Task.CompletedTask;
    }
}
