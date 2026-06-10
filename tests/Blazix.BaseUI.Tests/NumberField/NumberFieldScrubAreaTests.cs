namespace Blazix.BaseUI.Tests.NumberField;

public class NumberFieldScrubAreaTests : BunitContext, INumberFieldScrubAreaContract
{
    public NumberFieldScrubAreaTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNumberFieldModule(JSInterop);
    }

    private RenderFragment CreateNumberFieldWithScrubArea(
        double? defaultValue = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        ScrubDirection direction = ScrubDirection.Horizontal,
        Func<NumberFieldRootState, string?>? scrubClassValue = null,
        Func<NumberFieldRootState, string?>? scrubStyleValue = null,
        IReadOnlyDictionary<string, object>? scrubAdditionalAttributes = null,
        RenderFragment? scrubChildContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            var attrIndex = 1;

            if (defaultValue.HasValue)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue.Value);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldScrubArea>(0);
                var scrubAttr = 1;
                inner.AddAttribute(scrubAttr++, "Direction", direction);
                if (scrubClassValue is not null)
                    inner.AddAttribute(scrubAttr++, "ClassValue", scrubClassValue);
                if (scrubStyleValue is not null)
                    inner.AddAttribute(scrubAttr++, "StyleValue", scrubStyleValue);
                if (scrubAdditionalAttributes is not null)
                    inner.AddMultipleAttributes(scrubAttr++, scrubAdditionalAttributes);
                if (scrubChildContent is not null)
                    inner.AddAttribute(scrubAttr++, "ChildContent", scrubChildContent);
                inner.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateNumberFieldWithScrubArea());
        var scrub = cut.Find("[role='presentation']");
        scrub.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateNumberFieldWithScrubArea());
        var scrub = cut.Find("[role='presentation']");
        scrub.GetAttribute("role").ShouldBe("presentation");
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
                inner.OpenComponent<NumberFieldScrubArea>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<NumberFieldRootState>>)(props => b =>
                {
                    b.OpenElement(0, "div");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.AddContent(2, props.ChildContent);
                    b.CloseElement();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var scrub = cut.Find("[role='presentation']");
        scrub.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNumberFieldWithScrubArea(
            scrubChildContent: b => b.AddContent(0, "scrub content")));
        var scrub = cut.Find("[role='presentation']");
        scrub.TextContent.ShouldContain("scrub content");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "test-value" };
        var cut = Render(CreateNumberFieldWithScrubArea(scrubAdditionalAttributes: attrs));
        var scrub = cut.Find("[role='presentation']");
        scrub.GetAttribute("data-custom").ShouldBe("test-value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberFieldWithScrubArea(
            scrubClassValue: _ => "scrub-class"));
        var scrub = cut.Find("[role='presentation']");
        scrub.ClassList.ShouldContain("scrub-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberFieldWithScrubArea(
            scrubStyleValue: _ => "color:blue"));
        var scrub = cut.Find("[role='presentation']");
        // ScrubArea prepends user-select:none style
        scrub.GetAttribute("style").ShouldContain("color:blue");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberFieldWithScrubArea(
            scrubClassValue: _ => "func-class",
            scrubAdditionalAttributes: attrs));
        var scrub = cut.Find("[role='presentation']");
        scrub.ClassList.ShouldContain("func-class");
        scrub.ClassList.ShouldContain("attr-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateNumberFieldWithScrubArea());
        var scrub = cut.FindComponent<NumberFieldScrubArea>();
        scrub.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateNumberFieldWithScrubArea(disabled: true));
        var scrub = cut.Find("[role='presentation']");
        scrub.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var cut = Render(CreateNumberFieldWithScrubArea(readOnly: true));
        var scrub = cut.Find("[role='presentation']");
        scrub.HasAttribute("data-readonly").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateNumberFieldWithScrubArea(required: true));
        var scrub = cut.Find("[role='presentation']");
        scrub.HasAttribute("data-required").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataScrubbingAttribute()
    {
        // data-scrubbing is false by default; Blazor omits false boolean attributes from DOM
        var cut = Render(CreateNumberFieldWithScrubArea());
        var scrub = cut.Find("[role='presentation']");
        scrub.HasAttribute("data-scrubbing").ShouldBeFalse();
        return Task.CompletedTask;
    }
}
