namespace Blazix.BaseUI.Tests.Switch;

public class SwitchThumbTests : BunitContext, ISwitchThumbContract
{
    public SwitchThumbTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSwitchModule(JSInterop);
    }

    private RenderFragment CreateSwitchWithThumb(
        bool? isChecked = null,
        bool defaultChecked = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        Func<SwitchRootState, string>? thumbClassValue = null,
        Func<SwitchRootState, string>? thumbStyleValue = null,
        IReadOnlyDictionary<string, object>? thumbAdditionalAttributes = null,
        RenderFragment<RenderProps<SwitchRootState>>? thumbRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<SwitchRoot>(0);
            var attrIndex = 1;

            if (isChecked.HasValue)
                builder.AddAttribute(attrIndex++, "Checked", isChecked.Value);
            if (defaultChecked)
                builder.AddAttribute(attrIndex++, "DefaultChecked", true);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SwitchThumb>(0);
                var thumbAttrIndex = 1;

                if (thumbClassValue is not null)
                    innerBuilder.AddAttribute(thumbAttrIndex++, "ClassValue", thumbClassValue);
                if (thumbStyleValue is not null)
                    innerBuilder.AddAttribute(thumbAttrIndex++, "StyleValue", thumbStyleValue);
                if (thumbAdditionalAttributes is not null)
                    innerBuilder.AddAttribute(thumbAttrIndex++, "AdditionalAttributes", thumbAdditionalAttributes);
                if (thumbRender is not null)
                    innerBuilder.AddAttribute(thumbAttrIndex++, "Render", thumbRender);

                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateThumbWithContext(SwitchRootContext context,
        Func<SwitchRootState, string>? classValue = null,
        Func<SwitchRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<SwitchRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<CascadingValue<SwitchRootContext>>(0);
            builder.AddAttribute(1, "Value", context);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SwitchThumb>(0);
                var attrIndex = 1;

                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbRender: props => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, props.Attributes);
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
                builder.AddContent(3, props.ChildContent);
                builder.CloseElement();
            },
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbAdditionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "thumb" },
                { "aria-label", "Switch indicator" }
            }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.GetAttribute("aria-label").ShouldBe("Switch indicator");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbClassValue: _ => "thumb-class",
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.GetAttribute("class").ShouldContain("thumb-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbStyleValue: _ => "background: red",
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.GetAttribute("style").ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbClassValue: _ => "dynamic-class",
            thumbAdditionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "thumb" },
                { "class", "static-class" }
            }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        var classAttr = thumb.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DataAttributesCanBeOverridden()
    {
        var cut = Render(CreateSwitchWithThumb(
            defaultChecked: true,
            disabled: true,
            thumbAdditionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "thumb" },
                { "data-checked", "custom-checked" },
                { "data-disabled", "custom-disabled" }
            }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.GetAttribute("data-checked").ShouldBe("custom-checked");
        thumb.GetAttribute("data-disabled").ShouldBe("custom-disabled");

        return Task.CompletedTask;
    }

    // Style hooks (data attributes) tests
    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateSwitchWithThumb(
            defaultChecked: true,
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.HasAttribute("data-checked").ShouldBeTrue();
        thumb.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUnchecked()
    {
        var cut = Render(CreateSwitchWithThumb(
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.HasAttribute("data-unchecked").ShouldBeTrue();
        thumb.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSwitchWithThumb(
            disabled: true,
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateSwitchWithThumb(
            readOnly: true,
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateSwitchWithThumb(
            required: true,
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Context tests
    [Fact]
    public Task ReceivesStateFromContext()
    {
        var context = new SwitchRootContext
        {
            Checked = true,
            Disabled = true,
            ReadOnly = true,
            Required = true,
            State = new SwitchRootState(
                Checked: true,
                Disabled: true,
                ReadOnly: true,
                Required: true,
                Valid: null,
                Touched: false,
                Dirty: false,
                Filled: true,
                Focused: false)
        };

        var cut = Render(CreateThumbWithContext(
            context,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var thumb = cut.Find("[data-testid='thumb']");
        thumb.HasAttribute("data-checked").ShouldBeTrue();
        thumb.HasAttribute("data-disabled").ShouldBeTrue();
        thumb.HasAttribute("data-readonly").ShouldBeTrue();
        thumb.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsOutsideRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<SwitchThumb>(0);
            builder.AddAttribute(1, "AdditionalAttributes",
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "thumb" } });
            builder.CloseComponent();
        }));

        exception.Message.ShouldBe("Base UI: SwitchRootContext is missing. Switch parts must be placed within <Switch.Root>.");
        return Task.CompletedTask;
    }

    // State tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        SwitchRootState? capturedState = null;

        var cut = Render(CreateSwitchWithThumb(
            defaultChecked: true,
            disabled: true,
            thumbClassValue: state =>
            {
                capturedState = state;
                return "thumb-class";
            },
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();
        capturedState.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        SwitchRootState? capturedState = null;

        var cut = Render(CreateSwitchWithThumb(
            defaultChecked: true,
            thumbStyleValue: state =>
            {
                capturedState = state;
                return "color: blue";
            },
            thumbAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();

        return Task.CompletedTask;
    }
}
