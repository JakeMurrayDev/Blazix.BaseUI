namespace Blazix.BaseUI.Tests.Input;

public class InputTests : BunitContext, IInputContract
{
    public InputTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
    }

    private RenderFragment CreateInput(
        string? value = null,
        bool disabled = false,
        RenderFragment<RenderProps<InputState>>? render = null,
        Func<InputState, string>? classValue = null,
        Func<InputState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Input.Input>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsInputByDefault()
    {
        var cut = Render(CreateInput());

        var input = cut.Find("input");
        input.TagName.ShouldBe("INPUT");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["placeholder"] = "Enter text" };
        var cut = Render(CreateInput(additionalAttributes: attrs));

        var input = cut.Find("input");
        input.GetAttribute("placeholder").ShouldBe("Enter text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsValueToFieldControl()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Input.Input>(0);
            builder.AddAttribute(1, "Value", "hello");
            builder.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<string>(this, _ => { }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        var input = cut.Find("input");
        input.GetAttribute("value").ShouldBe("hello");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsDisabledToFieldControl()
    {
        var cut = Render(CreateInput(disabled: true));

        var input = cut.Find("input");
        input.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        Func<InputState, string> classValue = state => "my-input";
        var cut = Render(CreateInput(classValue: classValue));

        var input = cut.Find("input");
        input.GetAttribute("class").ShouldContain("my-input");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        Func<InputState, string> styleValue = state => "border: 1px solid red";
        var cut = Render(CreateInput(styleValue: styleValue));

        var input = cut.Find("input");
        input.GetAttribute("style").ShouldContain("border: 1px solid red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<InputState>> renderAsTextarea = props => builder =>
        {
            builder.OpenElement(0, "textarea");
            builder.AddMultipleAttributes(1, props.Attributes);
            builder.CloseElement();
        };

        var cut = Render(CreateInput(render: renderAsTextarea));

        var textarea = cut.Find("textarea");
        textarea.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
