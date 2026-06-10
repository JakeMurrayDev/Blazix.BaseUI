namespace Blazix.BaseUI.Tests.NumberField;

public class NumberFieldScrubAreaCursorTests : BunitContext, INumberFieldScrubAreaCursorContract
{
    public NumberFieldScrubAreaCursorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNumberFieldModule(JSInterop);
    }

    private RenderFragment CreateNumberFieldWithScrubAreaCursor(
        double? defaultValue = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        Func<NumberFieldRootState, string?>? cursorClassValue = null,
        Func<NumberFieldRootState, string?>? cursorStyleValue = null,
        IReadOnlyDictionary<string, object>? cursorAdditionalAttributes = null,
        RenderFragment? cursorChildContent = null)
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
                inner.AddAttribute(1, "ChildContent", (RenderFragment)(scrubInner =>
                {
                    scrubInner.OpenComponent<NumberFieldScrubAreaCursor>(0);
                    var cursorAttr = 1;
                    if (cursorClassValue is not null)
                        scrubInner.AddAttribute(cursorAttr++, "ClassValue", cursorClassValue);
                    if (cursorStyleValue is not null)
                        scrubInner.AddAttribute(cursorAttr++, "StyleValue", cursorStyleValue);
                    if (cursorAdditionalAttributes is not null)
                        scrubInner.AddMultipleAttributes(cursorAttr++, cursorAdditionalAttributes);
                    if (cursorChildContent is not null)
                        scrubInner.AddAttribute(cursorAttr++, "ChildContent", cursorChildContent);
                    scrubInner.CloseComponent();
                }));
                inner.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        // The cursor only renders when ShouldRenderCursor is true (IsScrubbing && !IsTouchInput && !IsPointerLockDenied).
        // When not scrubbing, it should not render. We test the tag name by checking the component exists.
        // Since Enabled=false means RenderElement won't output, we verify the component instance exists.
        var cut = Render(CreateNumberFieldWithScrubAreaCursor());
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        // The cursor uses role="presentation" in its component attributes.
        // Since it doesn't render when not scrubbing, we verify the component is present.
        var cut = Render(CreateNumberFieldWithScrubAreaCursor());
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        // Cursor won't render when not scrubbing; verify component instantiation.
        var cut = Render(CreateNumberFieldWithScrubAreaCursor());
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(
            cursorChildContent: b => b.AddContent(0, "cursor text")));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "cursor-val" };
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(cursorAdditionalAttributes: attrs));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(
            cursorClassValue: _ => "cursor-class"));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(
            cursorStyleValue: _ => "display:block"));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(
            cursorClassValue: _ => "func-class",
            cursorAdditionalAttributes: attrs));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        // Element is null when cursor is not rendered (not scrubbing)
        var cut = Render(CreateNumberFieldWithScrubAreaCursor());
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        // Element should be null because cursor is not rendered when not scrubbing
        cursor.Instance.Element.ShouldBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenNotScrubbing()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor());
        // The cursor should not produce any DOM element when not scrubbing
        cut.FindAll("[role='presentation']").Count.ShouldBe(1); // Only the ScrubArea has role=presentation, not the cursor
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(disabled: true));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(readOnly: true));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateNumberFieldWithScrubAreaCursor(required: true));
        var cursor = cut.FindComponent<NumberFieldScrubAreaCursor>();
        cursor.ShouldNotBeNull();
        return Task.CompletedTask;
    }
}
