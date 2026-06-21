using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Tests.OtpField;

public class OtpFieldRootTests : BunitContext
{
    private const int DefaultLength = 6;

    public OtpFieldRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupOtpFieldModule(JSInterop);
        JsInteropSetup.SetupFieldModule(JSInterop);
    }

    [Fact]
    public Task Root_RendersGroupAndHiddenValidationInput()
    {
        var cut = Render(CreateOtpField(name: "otp", required: true));

        var root = cut.Find("[role='group']");
        var inputs = cut.FindAll("input[data-blazix-otp-input]");
        var hidden = cut.Find("input[name='otp']");

        root.TagName.ShouldBe("DIV");
        inputs.Count.ShouldBe(DefaultLength);
        hidden.GetAttribute("type").ShouldBe("text");
        hidden.GetAttribute("autocomplete").ShouldBe("one-time-code");
        hidden.GetAttribute("inputmode").ShouldBe("numeric");
        hidden.GetAttribute("minlength").ShouldBe(DefaultLength.ToString());
        hidden.GetAttribute("maxlength").ShouldBe(DefaultLength.ToString());
        hidden.GetAttribute("pattern").ShouldBe(@"\d{6}");
        hidden.HasAttribute("required").ShouldBeTrue();
        hidden.GetAttribute("aria-hidden").ShouldBe("true");
        hidden.GetAttribute("tabindex").ShouldBe("-1");
        root.QuerySelector("input[data-blazix-otp-hidden-input]").ShouldBeNull();
        hidden.ParentElement.ShouldBe(root.ParentElement);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultValue_IsNormalizedSplitAndClamped()
    {
        var cut = Render(CreateOtpField(defaultValue: "12a34b56", name: "otp"));

        GetSlotValues(cut).ShouldBe(["1", "2", "3", "4", "5", "6"]);
        cut.Find("input[name='otp']").GetAttribute("value").ShouldBe("123456");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ValidationTypeAlpha_UsesAlphaPatternAndInputMode()
    {
        var cut = Render(CreateOtpField(defaultValue: "1a2b3Cd4", validationType: OtpFieldValidationType.Alpha, name: "otp"));

        GetSlotValues(cut).ShouldBe(["a", "b", "C", "d", "", ""]);
        cut.Find("input[data-blazix-otp-input]").GetAttribute("pattern").ShouldBe("[a-zA-Z]{1}");
        cut.Find("input[name='otp']").GetAttribute("pattern").ShouldBe("[a-zA-Z]{6}");
        cut.Find("input[name='otp']").GetAttribute("inputmode").ShouldBe("text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ValidationTypeNone_OmitsPatternAndAllowsInputModeOverride()
    {
        var cut = Render(CreateOtpField(
            defaultValue: "ab-12 cd",
            validationType: OtpFieldValidationType.None,
            inputMode: "numeric",
            name: "otp"));

        cut.Find("input[data-blazix-otp-input]").HasAttribute("pattern").ShouldBeFalse();
        cut.Find("input[name='otp']").HasAttribute("pattern").ShouldBeFalse();
        cut.Find("input[name='otp']").GetAttribute("inputmode").ShouldBe("numeric");
        cut.Find("input[name='otp']").GetAttribute("value").ShouldBe("ab-12c");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NormalizeValue_ComposesAfterValidationAndBeforeClamp()
    {
        var cut = Render(CreateOtpField(
            defaultValue: "ab-12 cd!",
            validationType: OtpFieldValidationType.Alphanumeric,
            normalizeValue: value => value.ToUpperInvariant(),
            name: "otp"));

        GetSlotValues(cut).ShouldBe(["A", "B", "1", "2", "C", "D"]);
        cut.Find("input[name='otp']").GetAttribute("value").ShouldBe("AB12CD");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Input_AttributesMatchReactParity()
    {
        var cut = Render(CreateOtpField(defaultValue: "12", id: "verification-code", readOnly: true, required: true));

        var inputs = cut.FindAll("input[data-blazix-otp-input]");

        inputs[0].GetAttribute("id").ShouldBe("verification-code");
        inputs[1].GetAttribute("id").ShouldBe("verification-code-2");
        inputs[0].GetAttribute("type").ShouldBe("text");
        inputs[0].GetAttribute("autocomplete").ShouldBe("one-time-code");
        inputs[1].GetAttribute("autocomplete").ShouldBe("off");
        inputs[0].GetAttribute("autocorrect").ShouldBe("off");
        inputs[0].GetAttribute("spellcheck").ShouldBe("false");
        inputs[0].GetAttribute("enterkeyhint").ShouldBe("next");
        inputs[5].GetAttribute("enterkeyhint").ShouldBe("done");
        inputs[0].GetAttribute("maxlength").ShouldBe(DefaultLength.ToString());
        inputs[1].HasAttribute("maxlength").ShouldBeFalse();
        inputs[0].GetAttribute("tabindex").ShouldBe("-1");
        inputs[2].GetAttribute("tabindex").ShouldBe("0");
        inputs[0].HasAttribute("readonly").ShouldBeTrue();
        inputs[0].HasAttribute("required").ShouldBeTrue();
        inputs[0].HasAttribute("data-readonly").ShouldBeTrue();
        inputs[0].HasAttribute("data-required").ShouldBeTrue();
        inputs[0].HasAttribute("data-filled").ShouldBeTrue();
        inputs[2].HasAttribute("data-filled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CompleteState_AddsDataCompleteToRootAndSlots()
    {
        var cut = Render(CreateOtpField(defaultValue: "123456"));

        cut.Find("[role='group']").HasAttribute("data-complete").ShouldBeTrue();
        foreach (var input in cut.FindAll("input[data-blazix-otp-input]"))
            input.HasAttribute("data-complete").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Field_LabelAndDescriptionAreAppliedToGroupAndSlots()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldLabel>(0);
                fieldBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object> { ["data-testid"] = "label" });
                fieldBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(labelBuilder => labelBuilder.AddContent(0, "Verification code")));
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldDescription>(2);
                fieldBuilder.AddAttribute(3, "AdditionalAttributes", new Dictionary<string, object> { ["data-testid"] = "description" });
                fieldBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(descriptionBuilder => descriptionBuilder.AddContent(0, "Enter the code.")));
                fieldBuilder.CloseComponent();

                fieldBuilder.AddContent(5, CreateOtpField(additionalAttributes: new Dictionary<string, object> { ["aria-describedby"] = "external-description" }));
            }));
            builder.CloseComponent();
        });

        var label = cut.Find("[data-testid='label']");
        var description = cut.Find("[data-testid='description']");
        var group = cut.Find("[role='group']");
        var inputs = cut.FindAll("input[data-blazix-otp-input]");

        label.GetAttribute("for").ShouldBe(inputs[0].GetAttribute("id"));
        group.GetAttribute("aria-labelledby").ShouldBe(label.GetAttribute("id"));
        group.GetAttribute("aria-describedby").ShouldBe($"external-description {description.GetAttribute("id")}");
        inputs[0].GetAttribute("aria-labelledby").ShouldBe(label.GetAttribute("id"));
        inputs[0].HasAttribute("aria-label").ShouldBeFalse();
        for (var index = 1; index < inputs.Count; index++)
        {
            inputs[index].HasAttribute("aria-labelledby").ShouldBeFalse();
            inputs[index].GetAttribute("aria-label").ShouldBe($"Character {index + 1} of {DefaultLength}");
        }

        foreach (var input in inputs)
            input.HasAttribute("aria-describedby").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Root_OnInputChangeUpdatesValueAndRaisesReasons()
    {
        var changes = new List<OtpFieldValueChangeEventArgs>();
        var invalid = new List<OtpFieldValueInvalidEventArgs>();
        var complete = new List<OtpFieldValueCompleteEventArgs>();

        var cut = Render(CreateOtpField(
            onValueChange: EventCallback.Factory.Create<OtpFieldValueChangeEventArgs>(this, args => changes.Add(args)),
            onValueInvalid: EventCallback.Factory.Create<OtpFieldValueInvalidEventArgs>(this, args => invalid.Add(args)),
            onValueComplete: EventCallback.Factory.Create<OtpFieldValueCompleteEventArgs>(this, args => complete.Add(args))));

        cut.Find("input[data-blazix-otp-input]").Input(new ChangeEventArgs { Value = "12a3456" });

        GetSlotValues(cut).ShouldBe(["1", "2", "3", "4", "5", "6"]);
        changes.Count.ShouldBe(1);
        changes[0].Value.ShouldBe("123456");
        changes[0].Reason.ShouldBe(OtpFieldChangeReason.InputChange);
        invalid.Count.ShouldBe(1);
        invalid[0].Value.ShouldBe("12a3456");
        invalid[0].Reason.ShouldBe(OtpFieldInvalidReason.InputChange);
        complete.Count.ShouldBe(1);
        complete[0].Value.ShouldBe("123456");
        complete[0].Reason.ShouldBe(OtpFieldCompleteReason.InputChange);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Root_CancelledChangeDoesNotCommitOrMove()
    {
        var cut = Render(CreateOtpField(
            onValueChange: EventCallback.Factory.Create<OtpFieldValueChangeEventArgs>(this, args => args.Cancel())));

        GetSlotValues(cut).ShouldBe(["", "", "", "", "", ""]);

        return Task.CompletedTask;
    }

    private static string[] GetSlotValues(IRenderedComponent<ContainerFragment> cut) =>
        cut.FindAll("input[data-blazix-otp-input]")
            .Select(input => input.GetAttribute("value") ?? string.Empty)
            .ToArray();

    private static RenderFragment CreateOtpField(
        string? id = null,
        int length = DefaultLength,
        string? defaultValue = null,
        string? value = null,
        string? name = null,
        string? form = null,
        string? inputMode = null,
        OtpFieldValidationType validationType = OtpFieldValidationType.Numeric,
        Func<string, string>? normalizeValue = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        bool mask = false,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        EventCallback<OtpFieldValueChangeEventArgs>? onValueChange = null,
        EventCallback<OtpFieldValueInvalidEventArgs>? onValueInvalid = null,
        EventCallback<OtpFieldValueCompleteEventArgs>? onValueComplete = null)
    {
        return builder =>
        {
            builder.OpenComponent<OtpFieldRoot>(0);
            var attr = 1;
            if (id is not null)
                builder.AddAttribute(attr++, "Id", id);
            builder.AddAttribute(attr++, "Length", length);
            if (defaultValue is not null)
                builder.AddAttribute(attr++, "DefaultValue", defaultValue);
            if (value is not null)
                builder.AddAttribute(attr++, "Value", value);
            if (name is not null)
                builder.AddAttribute(attr++, "Name", name);
            if (form is not null)
                builder.AddAttribute(attr++, "Form", form);
            if (inputMode is not null)
                builder.AddAttribute(attr++, "InputMode", inputMode);
            if (validationType != OtpFieldValidationType.Numeric)
                builder.AddAttribute(attr++, "ValidationType", validationType);
            if (normalizeValue is not null)
                builder.AddAttribute(attr++, "NormalizeValue", normalizeValue);
            if (disabled)
                builder.AddAttribute(attr++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attr++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attr++, "Required", true);
            if (mask)
                builder.AddAttribute(attr++, "Mask", true);
            if (additionalAttributes is not null)
                builder.AddMultipleAttributes(attr++, additionalAttributes);
            if (onValueChange.HasValue)
                builder.AddAttribute(attr++, "OnValueChange", onValueChange.Value);
            if (onValueInvalid.HasValue)
                builder.AddAttribute(attr++, "OnValueInvalid", onValueInvalid.Value);
            if (onValueComplete.HasValue)
                builder.AddAttribute(attr++, "OnValueComplete", onValueComplete.Value);
            builder.AddAttribute(attr++, "ChildContent", (RenderFragment)(inputBuilder =>
            {
                for (var index = 0; index < length; index++)
                {
                    inputBuilder.OpenComponent<OtpFieldInput>(index);
                    inputBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                    {
                        ["aria-label"] = index == 0 ? string.Empty : $"Character {index + 1} of {length}"
                    });
                    inputBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }
}
