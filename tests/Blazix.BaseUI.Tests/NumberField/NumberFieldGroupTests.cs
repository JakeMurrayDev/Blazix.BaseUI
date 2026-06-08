namespace Blazix.BaseUI.Tests.NumberField;

public class NumberFieldGroupTests : BunitContext, INumberFieldGroupContract
{
    public NumberFieldGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNumberFieldModule(JSInterop);
    }

    private RenderFragment CreateNumberField(
        double? defaultValue = null,
        RenderFragment? groupChildContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            var attrIndex = 1;

            if (defaultValue.HasValue)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                if (groupChildContent is not null)
                    inner.AddAttribute(1, "ChildContent", groupChildContent);
                inner.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateNumberFieldWithGroupProps(
        double? defaultValue = null,
        Func<NumberFieldRootState, string?>? classValue = null,
        Func<NumberFieldRootState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            var attrIndex = 1;

            if (defaultValue.HasValue)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                var groupAttr = 1;
                if (classValue is not null)
                    inner.AddAttribute(groupAttr++, "ClassValue", classValue);
                if (styleValue is not null)
                    inner.AddAttribute(groupAttr++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    inner.AddMultipleAttributes(groupAttr++, additionalAttributes);
                if (childContent is not null)
                    inner.AddAttribute(groupAttr++, "ChildContent", childContent);
                inner.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleGroup()
    {
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.GetAttribute("role").ShouldBe("group");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<NumberFieldRootState>>)(props => b =>
                {
                    b.OpenElement(0, "section");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.AddContent(2, props.ChildContent);
                    b.CloseElement();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var group = cut.Find("[role='group']");
        group.TagName.ShouldBe("SECTION");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var fragment = CreateNumberFieldWithGroupProps(
            childContent: b => b.AddContent(0, "child text"));

        var cut = Render(fragment);
        var group = cut.Find("[role='group']");
        group.TextContent.ShouldContain("child text");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "test-value" };
        var cut = Render(CreateNumberFieldWithGroupProps(additionalAttributes: attrs));
        var group = cut.Find("[role='group']");
        group.GetAttribute("data-custom").ShouldBe("test-value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberFieldWithGroupProps(
            classValue: _ => "my-class"));
        var group = cut.Find("[role='group']");
        group.ClassList.ShouldContain("my-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberFieldWithGroupProps(
            styleValue: _ => "color:red"));
        var group = cut.Find("[role='group']");
        group.GetAttribute("style").ShouldContain("color:red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberFieldWithGroupProps(
            classValue: _ => "func-class",
            additionalAttributes: attrs));
        var group = cut.Find("[role='group']");
        group.ClassList.ShouldContain("func-class");
        group.ClassList.ShouldContain("attr-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateNumberField());
        var group = cut.FindComponent<NumberFieldGroup>();
        group.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            builder.AddAttribute(1, "Disabled", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            builder.AddAttribute(1, "ReadOnly", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-readonly").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            builder.AddAttribute(1, "Required", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-required").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataScrubbingAttribute()
    {
        // data-scrubbing is false by default; Blazor omits false boolean attributes from DOM
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-scrubbing").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataTouchedAttribute()
    {
        // data-touched is false by default (no FieldContext); Blazor omits false boolean attributes
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-touched").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDirtyAttribute()
    {
        // data-dirty is false by default (no FieldContext); Blazor omits false boolean attributes
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-dirty").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFilledAttribute()
    {
        // data-filled is false by default (no value, no FieldContext); Blazor omits false boolean attributes
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-filled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFocusedAttribute()
    {
        // data-focused is false by default (no FieldContext); Blazor omits false boolean attributes
        var cut = Render(CreateNumberField());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-focused").ShouldBeFalse();
        return Task.CompletedTask;
    }
}
