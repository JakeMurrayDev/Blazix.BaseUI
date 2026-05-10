using BlazorBaseUI.Field;
using BlazorBaseUI.Checkbox;

namespace BlazorBaseUI.CheckboxGroup;

/// <summary>
/// Provides cascading state from <see cref="CheckboxGroup"/> to its child checkboxes.
/// </summary>
internal sealed class CheckboxGroupContext
{
    /// <summary>
    /// Gets or sets the names of the checkboxes in the group that are currently ticked.
    /// </summary>
    public string[]? Value { get; set; }

    /// <summary>
    /// Gets or sets the names of the checkboxes in the group that should be initially ticked.
    /// </summary>
    public string[]? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the names of all checkboxes in the group.
    /// </summary>
    public string[]? AllValues { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox group is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the parent checkbox state when using the parent checkbox pattern.
    /// </summary>
    public CheckboxGroupParent? Parent { get; set; }

    /// <summary>
    /// Gets or sets the field validation state.
    /// </summary>
    public FieldValidation? Validation { get; set; }

    /// <summary>
    /// Gets or sets the delegate used to update the group's value.
    /// </summary>
    public Func<string[], Task> SetValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate used to register a child checkbox control.
    /// </summary>
    public Action<CheckboxRoot> RegisterControlAction { get; set; } = null!;

    /// <summary>
    /// Updates the group's value to the specified array.
    /// </summary>
    /// <param name="value">The new group value.</param>
    public void SetValue(string[] value) => SetValueFunc(value);

    /// <summary>
    /// Registers a child <see cref="CheckboxRoot"/> control with the group.
    /// </summary>
    /// <param name="checkbox">The checkbox to register.</param>
    public void RegisterControlRef(CheckboxRoot checkbox) => RegisterControlAction(checkbox);
}

/// <summary>
/// Manages the state and behavior of a parent checkbox that controls a group of child checkboxes.
/// </summary>
internal sealed class CheckboxGroupParent
{
    /// <summary>
    /// Gets or sets the identifier used to generate <c>aria-controls</c> references.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the names of all child checkboxes in the group.
    /// </summary>
    public string[] AllValues { get; set; } = [];

    /// <summary>
    /// Gets or sets the default checked values for the group.
    /// </summary>
    public string[]? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the delegate that returns the current group value.
    /// </summary>
    public Func<string[]?> GetValue { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate used to update the group's value.
    /// </summary>
    public Func<string[], Task> SetValue { get; set; } = null!;

    private readonly Dictionary<string, bool> disabledStates = [];
    private string[]? uncontrolledState;
    private bool uncontrolledStateInitialized;

    private ParentCheckboxStatus status = ParentCheckboxStatus.Mixed;

    private string[] UncontrolledState
    {
        get
        {
            if (!uncontrolledStateInitialized)
            {
                uncontrolledState = DefaultValue ?? [];
                uncontrolledStateInitialized = true;
            }
            return uncontrolledState!;
        }
    }

    /// <summary>
    /// Gets a value indicating whether all child checkboxes are checked.
    /// </summary>
    public bool Checked
    {
        get
        {
            var value = GetValue() ?? [];
            return value.Length == AllValues.Length && AllValues.Length > 0;
        }
    }

    /// <summary>
    /// Gets a value indicating whether some, but not all, child checkboxes are checked.
    /// </summary>
    public bool Indeterminate
    {
        get
        {
            var value = GetValue() ?? [];
            return value.Length > 0 && value.Length < AllValues.Length;
        }
    }

    /// <summary>
    /// Gets the space-separated list of child checkbox input IDs for the <c>aria-controls</c> attribute.
    /// </summary>
    public string? AriaControls =>
        AllValues.Length > 0
            ? string.Join(" ", AllValues.Select(v => $"{Id}-{v}"))
            : null;

    /// <summary>
    /// Handles a checked change on the parent checkbox, cycling through all-on, all-off, and mixed states.
    /// </summary>
    /// <param name="nextChecked">The new checked state requested by the user.</param>
    public void OnCheckedChange(bool nextChecked)
    {
        var currentValue = GetValue() ?? [];

        var none = AllValues
            .Where(v => disabledStates.TryGetValue(v, out var disabled) && disabled && currentValue.Contains(v))
            .ToArray();

        var all = AllValues
            .Where(v => !disabledStates.TryGetValue(v, out var disabled) || !disabled || currentValue.Contains(v))
            .ToArray();

        var allOnOrOff = currentValue.Length == all.Length || currentValue.Length == 0;

        if (allOnOrOff)
        {
            if (currentValue.Length == all.Length)
            {
                SetValue(none);
            }
            else
            {
                SetValue(all);
            }
            return;
        }

        if (status == ParentCheckboxStatus.Mixed)
        {
            SetValue(all);
            status = ParentCheckboxStatus.On;
        }
        else if (status == ParentCheckboxStatus.On)
        {
            SetValue(none);
            status = ParentCheckboxStatus.Off;
        }
        else if (status == ParentCheckboxStatus.Off)
        {
            SetValue(UncontrolledState);
            status = ParentCheckboxStatus.Mixed;
        }
    }

    /// <summary>
    /// Handles a checked change on a child checkbox within the group.
    /// </summary>
    /// <param name="childValue">The value of the child checkbox that changed.</param>
    /// <param name="nextChecked">The new checked state of the child checkbox.</param>
    public void OnChildCheckedChange(string childValue, bool nextChecked)
    {
        var currentValue = GetValue() ?? [];
        string[] newValue;

        if (nextChecked)
        {
            newValue = [.. currentValue, childValue];
        }
        else
        {
            newValue = currentValue.Where(v => v != childValue).ToArray();
        }

        SetValue(newValue);
        status = ParentCheckboxStatus.Mixed;
    }

    /// <summary>
    /// Records the disabled state of a child checkbox for use in parent toggle calculations.
    /// </summary>
    /// <param name="value">The value of the child checkbox.</param>
    /// <param name="disabled">Whether the child checkbox is disabled.</param>
    public void SetDisabledState(string value, bool disabled)
    {
        disabledStates[value] = disabled;
    }

    /// <summary>
    /// Removes the tracked disabled state for a child checkbox that has been disposed.
    /// </summary>
    /// <param name="value">The value of the child checkbox to remove.</param>
    public void RemoveDisabledState(string value)
    {
        disabledStates.Remove(value);
    }
}
