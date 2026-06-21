namespace Blazix.BaseUI.Tests.OtpField;

public class OtpFieldInputTests : BunitContext
{
    public OtpFieldInputTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupOtpFieldModule(JSInterop);
    }

    [Fact]
    public Task Input_ThrowsOutsideRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<OtpFieldInput>(0);
            builder.CloseComponent();
        }));

        exception.Message.ShouldBe("Base UI: OtpFieldRootContext is missing. OtpField parts must be placed within <OtpFieldRoot>.");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Mask_UsesPasswordTypeUnlessInputOverridesType()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<OtpFieldRoot>(0);
            builder.AddAttribute(1, "Length", 2);
            builder.AddAttribute(2, "Mask", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(inputBuilder =>
            {
                inputBuilder.OpenComponent<OtpFieldInput>(0);
                inputBuilder.CloseComponent();
                inputBuilder.OpenComponent<OtpFieldInput>(1);
                inputBuilder.AddMultipleAttributes(2, new Dictionary<string, object> { ["type"] = "tel" });
                inputBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var inputs = cut.FindAll("input[data-blazix-otp-input]");
        inputs[0].GetAttribute("type").ShouldBe("password");
        inputs[1].GetAttribute("type").ShouldBe("tel");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledValue_DoesNotUpdateRenderedSlotsUntilValueParameterChanges()
    {
        var changedValue = string.Empty;
        var cut = Render(builder =>
        {
            builder.OpenComponent<OtpFieldRoot>(0);
            builder.AddAttribute(1, "Length", 4);
            builder.AddAttribute(2, "Value", "12");
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string>(this, value => changedValue = value));
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(inputBuilder =>
            {
                for (var index = 0; index < 4; index++)
                {
                    inputBuilder.OpenComponent<OtpFieldInput>(index);
                    inputBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        });

        cut.Find("input[data-blazix-otp-input]").Input(new ChangeEventArgs { Value = "1234" });

        changedValue.ShouldBe("1234");
        cut.FindAll("input[data-blazix-otp-input]")
            .Select(input => input.GetAttribute("value") ?? string.Empty)
            .ShouldBe(["1", "2", "", ""]);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledRoot_BlocksInputMutationAndAddsDisabledAttributes()
    {
        var changes = new List<OtpFieldValueChangeEventArgs>();
        var cut = Render(builder =>
        {
            builder.OpenComponent<OtpFieldRoot>(0);
            builder.AddAttribute(1, "Length", 3);
            builder.AddAttribute(2, "DefaultValue", "1");
            builder.AddAttribute(3, "Disabled", true);
            builder.AddAttribute(4, "OnValueChange", EventCallback.Factory.Create<OtpFieldValueChangeEventArgs>(this, args => changes.Add(args)));
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(inputBuilder =>
            {
                for (var index = 0; index < 3; index++)
                {
                    inputBuilder.OpenComponent<OtpFieldInput>(index);
                    inputBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        });

        var inputs = cut.FindAll("input[data-blazix-otp-input]");
        inputs[0].HasAttribute("disabled").ShouldBeTrue();
        inputs[0].HasAttribute("data-disabled").ShouldBeTrue();

        inputs[0].Input(new ChangeEventArgs { Value = "123" });

        changes.ShouldBeEmpty();
        inputs = cut.FindAll("input[data-blazix-otp-input]");
        inputs.Select(input => input.GetAttribute("value") ?? string.Empty).ShouldBe(["1", "", ""]);

        return Task.CompletedTask;
    }
}
