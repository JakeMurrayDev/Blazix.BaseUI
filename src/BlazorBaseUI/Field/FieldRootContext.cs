using BlazorBaseUI.Form;

namespace BlazorBaseUI.Field;

/// <summary>
/// Notifies a subscriber when the field state changes.
/// </summary>
internal interface IFieldStateSubscriber
{
    /// <summary>
    /// Called when the field state has changed.
    /// </summary>
    void NotifyStateChanged();
}

/// <summary>
/// Defines the context contract for the <see cref="FieldRoot"/> component.
/// </summary>
internal interface IFieldRootContext
{
    /// <summary>Gets whether the field is marked as invalid by an external source.</summary>
    bool? Invalid { get; }

    /// <summary>Gets the name that identifies the field when a form is submitted.</summary>
    string? Name { get; }

    /// <summary>Gets the current validity data for the field.</summary>
    FieldValidityData ValidityData { get; }

    /// <summary>Gets whether the field is disabled.</summary>
    bool Disabled { get; }

    /// <summary>Gets whether the field has been touched.</summary>
    bool Touched { get; }

    /// <summary>Gets whether the field value has changed from its initial value.</summary>
    bool Dirty { get; }

    /// <summary>Gets whether the field has a value.</summary>
    bool Filled { get; }

    /// <summary>Gets whether the field control is focused.</summary>
    bool Focused { get; }

    /// <summary>Gets when the field should be validated.</summary>
    ValidationMode ValidationMode { get; }

    /// <summary>Gets the debounce time in milliseconds for onChange validation.</summary>
    int ValidationDebounceTime { get; }

    /// <summary>Gets the current state of the field.</summary>
    FieldRootState State { get; }

    /// <summary>Gets the validation logic for the field.</summary>
    FieldValidation Validation { get; }

    /// <summary>Registers a form control with the field.</summary>
    void RegisterFieldControl(string source, FieldControlRegistration? registration);

    /// <summary>Sets the validity data for the field.</summary>
    void SetValidityData(FieldValidityData data);

    /// <summary>Sets the touched state of the field.</summary>
    void SetTouched(bool value);

    /// <summary>Sets the dirty state of the field.</summary>
    void SetDirty(bool value);

    /// <summary>Sets the filled state of the field.</summary>
    void SetFilled(bool value);

    /// <summary>Sets the focused state of the field.</summary>
    void SetFocused(bool value);

    /// <summary>Returns whether the field should validate on value changes.</summary>
    bool ShouldValidateOnChange();

    /// <summary>Registers a handler to focus the field control.</summary>
    void RegisterFocusHandler(Func<ValueTask> handler);

    /// <summary>Subscribes to field state change notifications.</summary>
    void Subscribe(IFieldStateSubscriber subscriber);

    /// <summary>Unsubscribes from field state change notifications.</summary>
    void Unsubscribe(IFieldStateSubscriber subscriber);
}

/// <summary>
/// Provides the cascading context for the <see cref="FieldRoot"/> component.
/// </summary>
internal sealed class FieldRootContext : IFieldRootContext
{
    private Action<FieldValidityData>? setValidityDataCallback;
    private Action<bool>? setTouchedCallback;
    private Action<bool>? setDirtyCallback;
    private Action<bool>? setFilledCallback;
    private Action<bool>? setFocusedCallback;
    private Func<bool>? shouldValidateOnChangeCallback;
    private Action<string, FieldControlRegistration?>? registerFieldControlCallback;
    private Action<Func<ValueTask>>? registerFocusHandlerCallback;
    private Action<IFieldStateSubscriber>? subscribeCallback;
    private Action<IFieldStateSubscriber>? unsubscribeCallback;

    public bool? Invalid { get; private set; }
    public string? Name { get; private set; }
    public FieldValidityData ValidityData { get; private set; } = FieldValidityData.Default;
    public bool Disabled { get; private set; }
    public bool Touched { get; private set; }
    public bool Dirty { get; private set; }
    public bool Filled { get; private set; }
    public bool Focused { get; private set; }
    public ValidationMode ValidationMode { get; private set; } = ValidationMode.OnSubmit;
    public int ValidationDebounceTime { get; private set; }
    public FieldRootState State { get; private set; } = FieldRootState.Default;
    public FieldValidation Validation { get; private set; } = null!;

    private FieldRootContext() { }

    public FieldRootContext(
        Action<FieldValidityData> setValidityData,
        Action<bool> setTouched,
        Action<bool> setDirty,
        Action<bool> setFilled,
        Action<bool> setFocused,
        Func<bool> shouldValidateOnChange,
        Action<string, FieldControlRegistration?> registerFieldControl,
        Action<Func<ValueTask>> registerFocusHandler,
        Action<IFieldStateSubscriber> subscribe,
        Action<IFieldStateSubscriber> unsubscribe,
        FieldValidation validation)
    {
        setValidityDataCallback = setValidityData;
        setTouchedCallback = setTouched;
        setDirtyCallback = setDirty;
        setFilledCallback = setFilled;
        setFocusedCallback = setFocused;
        shouldValidateOnChangeCallback = shouldValidateOnChange;
        registerFieldControlCallback = registerFieldControl;
        registerFocusHandlerCallback = registerFocusHandler;
        subscribeCallback = subscribe;
        unsubscribeCallback = unsubscribe;
        Validation = validation;
    }

    internal void Update(
        bool? invalid,
        string? name,
        FieldValidityData validityData,
        bool disabled,
        bool touched,
        bool dirty,
        bool filled,
        bool focused,
        ValidationMode validationMode,
        int validationDebounceTime,
        FieldRootState state)
    {
        Invalid = invalid;
        Name = name;
        ValidityData = validityData;
        Disabled = disabled;
        Touched = touched;
        Dirty = dirty;
        Filled = filled;
        Focused = focused;
        ValidationMode = validationMode;
        ValidationDebounceTime = validationDebounceTime;
        State = state;
    }

    public void RegisterFieldControl(string source, FieldControlRegistration? registration) =>
        registerFieldControlCallback?.Invoke(source, registration);

    public void SetValidityData(FieldValidityData data) => setValidityDataCallback?.Invoke(data);
    public void SetTouched(bool value) => setTouchedCallback?.Invoke(value);
    public void SetDirty(bool value) => setDirtyCallback?.Invoke(value);
    public void SetFilled(bool value) => setFilledCallback?.Invoke(value);
    public void SetFocused(bool value) => setFocusedCallback?.Invoke(value);
    public bool ShouldValidateOnChange() => shouldValidateOnChangeCallback?.Invoke() ?? false;
    public void RegisterFocusHandler(Func<ValueTask> handler) => registerFocusHandlerCallback?.Invoke(handler);
    public void Subscribe(IFieldStateSubscriber subscriber) => subscribeCallback?.Invoke(subscriber);
    public void Unsubscribe(IFieldStateSubscriber subscriber) => unsubscribeCallback?.Invoke(subscriber);

    internal Func<bool> ShouldValidateOnChangeFunc => ShouldValidateOnChange;
    internal Action<Func<ValueTask>> RegisterFocusHandlerFunc => handler => RegisterFocusHandler(handler);
    internal Action<IFieldStateSubscriber> SubscribeFunc => Subscribe;
    internal Action<IFieldStateSubscriber> UnsubscribeFunc => Unsubscribe;
}
