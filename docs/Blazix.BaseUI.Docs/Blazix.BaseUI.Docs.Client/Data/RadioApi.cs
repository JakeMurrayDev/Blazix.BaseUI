namespace Blazix.BaseUI.Docs.Client.Data;

public static class RadioApi
{
    private static readonly IReadOnlyList<ApiRow> FieldStateAttributes =
    [
        new("data-disabled", "", "", "Present when the component is disabled."),
        new("data-readonly", "", "", "Present when the component is read-only."),
        new("data-required", "", "", "Present when the component is required."),
        new("data-valid", "", "", "Present when the component is valid inside FieldRoot."),
        new("data-invalid", "", "", "Present when the component is invalid inside FieldRoot."),
        new("data-touched", "", "", "Present when the field has been touched inside FieldRoot."),
        new("data-dirty", "", "", "Present when the field value has changed inside FieldRoot."),
        new("data-filled", "", "", "Present when the field has a selected value inside FieldRoot."),
        new("data-focused", "", "", "Present when the field is focused inside FieldRoot."),
    ];

    private static readonly IReadOnlyList<ApiRow> RadioStateAttributes =
    [
        new("data-checked", "", "", "Present when the radio is selected."),
        new("data-unchecked", "", "", "Present when the radio is not selected."),
        new("data-disabled", "", "", "Present when the radio is disabled."),
        new("data-readonly", "", "", "Present when the radio is read-only."),
        new("data-required", "", "", "Present when the radio is required."),
        new("data-valid", "", "", "Present when the radio is valid inside FieldRoot."),
        new("data-invalid", "", "", "Present when the radio is invalid inside FieldRoot."),
        new("data-touched", "", "", "Present when the field has been touched inside FieldRoot."),
        new("data-dirty", "", "", "Present when the field value has changed inside FieldRoot."),
        new("data-filled", "", "", "Present when the field has a selected value inside FieldRoot."),
        new("data-focused", "", "", "Present when the field is focused inside FieldRoot."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("RadioGroup",
            "Provides shared single-selection state to a set of RadioRoot items. Renders a <div> element with radiogroup semantics by default.",
            [
                new ApiRow("Value", "TValue?", "null", "The controlled selected value. Supplying Value makes the group controlled, even without ValueChanged."),
                new ApiRow("DefaultValue", "TValue?", "null", "The initially selected value for uncontrolled usage."),
                new ApiRow("Disabled", "bool", "false", "Determines whether every radio in the group should ignore user interaction."),
                new ApiRow("ReadOnly", "bool", "false", "Determines whether the selected value cannot be changed by the user."),
                new ApiRow("Required", "bool", "false", "Determines whether a value must be selected before form submission."),
                new ApiRow("Name", "string?", "null", "The field name applied to child radio inputs. Falls back to FieldRoot.Name when present."),
                new ApiRow("Form", "string?", "null", "The id of the form that owns the child radio inputs when the group is rendered outside the form."),
                new ApiRow("ValueChanged", "EventCallback<TValue?>", "default", "Two-way binding callback invoked when the selected value changes."),
                new ApiRow("OnValueChange", "EventCallback<RadioGroupValueChangeEventArgs<TValue>>", "default", "Callback invoked before the value update is committed. Call Cancel() on the event args to prevent the update."),
                new ApiRow("Render", "RenderFragment<RenderProps<RadioGroupState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<RadioGroupState, string?>?", "null", "Returns a CSS class based on the group state."),
                new ApiRow("StyleValue", "Func<RadioGroupState, string?>?", "null", "Returns a CSS style based on the group state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The RadioRoot items to render."),
            ],
            FieldStateAttributes,
            []),
        new("Root",
            "Represents one radio button. Renders a <span> element by default and a hidden sibling <input type=\"radio\"> for native forms.",
            [
                new ApiRow("Value", "TValue", "required", "The unique identifying value of this radio in its group."),
                new ApiRow("Disabled", "bool", "false", "Determines whether this radio should ignore user interaction."),
                new ApiRow("ReadOnly", "bool", "false", "Determines whether this radio cannot be selected by the user."),
                new ApiRow("Required", "bool", "false", "Determines whether a value must be selected before form submission."),
                new ApiRow("NativeButton", "bool", "false", "Renders the visible radio as a native <button type=\"button\"> when using a custom native-button pattern."),
                new ApiRow("Name", "string?", "null", "The field name for the hidden input. Falls back to RadioGroup.Name or FieldRoot.Name."),
                new ApiRow("Render", "RenderFragment<RenderProps<RadioRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<RadioRootState, string?>?", "null", "Returns a CSS class based on the radio state."),
                new ApiRow("StyleValue", "Func<RadioRootState, string?>?", "null", "Returns a CSS style based on the radio state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The radio indicator or custom visual contents."),
            ],
            [
                .. RadioStateAttributes,
                new("data-radio-item", "", "", "Present when the radio is registered inside a RadioGroup."),
            ],
            []),
        new("Indicator",
            "Indicates whether the radio is selected. Renders a <span> element and unmounts when unchecked unless KeepMounted is true.",
            [
                new ApiRow("KeepMounted", "bool", "false", "Keeps the indicator element in the DOM when the radio is unchecked."),
                new ApiRow("Render", "RenderFragment<RenderProps<RadioIndicatorState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<RadioIndicatorState, string?>?", "null", "Returns a CSS class based on the indicator state."),
                new ApiRow("StyleValue", "Func<RadioIndicatorState, string?>?", "null", "Returns a CSS style based on the indicator state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The indicator visual contents."),
            ],
            [
                .. RadioStateAttributes,
                new("data-starting-style", "", "", "Present while the indicator is entering."),
                new("data-ending-style", "", "", "Present while the indicator is exiting."),
            ],
            []),
    ];
}
