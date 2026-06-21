using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.OtpField;

internal sealed class OtpFieldRootContext
{
    private readonly Func<OtpFieldInput, int> registerInput;
    private readonly Action<OtpFieldInput> unregisterInput;
    private readonly Action<int, ElementReference?> setInputElement;
    private readonly Func<int, string?, Task<OtpFieldCommitResult>> commitInput;

    public OtpFieldRootContext(
        Func<OtpFieldInput, int> registerInput,
        Action<OtpFieldInput> unregisterInput,
        Action<int, ElementReference?> setInputElement,
        Func<int, string?, Task<OtpFieldCommitResult>> commitInput)
    {
        this.registerInput = registerInput;
        this.unregisterInput = unregisterInput;
        this.setInputElement = setInputElement;
        this.commitInput = commitInput;
    }

    public int ActiveIndex { get; private set; }
    public string? AutoComplete { get; private set; }
    public bool Disabled { get; private set; }
    public string? Form { get; private set; }
    public string? InputMode { get; private set; }
    public string? InputAriaLabelledBy { get; private set; }
    public bool Invalid { get; private set; }
    public int Length { get; private set; }
    public bool Mask { get; private set; }
    public string? Pattern { get; private set; }
    public bool ReadOnly { get; private set; }
    public bool Required { get; private set; }
    public OtpFieldRootState State { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public Func<int, string?> GetInputId { get; private set; } = _ => null;

    public void Update(
        int activeIndex,
        string? autoComplete,
        bool disabled,
        string? form,
        string? inputMode,
        string? inputAriaLabelledBy,
        bool invalid,
        int length,
        bool mask,
        string? pattern,
        bool readOnly,
        bool required,
        OtpFieldRootState state,
        string value,
        Func<int, string?> getInputId)
    {
        ActiveIndex = activeIndex;
        AutoComplete = autoComplete;
        Disabled = disabled;
        Form = form;
        InputMode = inputMode;
        InputAriaLabelledBy = inputAriaLabelledBy;
        Invalid = invalid;
        Length = length;
        Mask = mask;
        Pattern = pattern;
        ReadOnly = readOnly;
        Required = required;
        State = state;
        Value = value;
        GetInputId = getInputId;
    }

    public int RegisterInput(OtpFieldInput input) => registerInput(input);
    public void UnregisterInput(OtpFieldInput input) => unregisterInput(input);
    public void SetInputElement(int index, ElementReference? element) => setInputElement(index, element);
    public Task<OtpFieldCommitResult> CommitInputAsync(int index, string? value) => commitInput(index, value);
}
