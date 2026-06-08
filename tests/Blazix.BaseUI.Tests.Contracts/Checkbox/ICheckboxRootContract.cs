namespace Blazix.BaseUI.Tests.Contracts.Checkbox;

public interface ICheckboxRootContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task OverridesBuiltInAttributes();

    // ARIA and role
    Task HasRoleCheckbox();
    Task HasAriaCheckedFalseByDefault();
    Task HasAriaCheckedTrueWhenChecked();
    Task HasAriaCheckedMixedWhenIndeterminate();
    Task HasAriaRequiredWhenRequired();
    Task DoesNotHaveAriaRequiredByDefault();
    Task HasTabindexZeroByDefault();
    Task HasTabindexMinusOneWhenDisabled();

    // Disabled
    Task HasAriaDisabledWhenDisabled();
    Task DoesNotHaveAriaDisabledByDefault();
    Task DoesNotChangeStateWhenClickedDisabled();

    // ReadOnly
    Task HasAriaReadonlyWhenReadOnly();
    Task DoesNotHaveAriaReadonlyByDefault();
    Task DoesNotChangeStateWhenClickedReadOnly();

    // Indeterminate
    Task IndeterminateDoesNotChangeStateWhenClicked();
    Task IndeterminateOverridesChecked();

    // Name and Value on hidden input
    Task SetsNameOnInputOnly();
    Task DoesNotSetValueByDefault();
    Task SetsValueOnInputOnly();

    // Hidden checkbox input
    Task RendersHiddenCheckboxInput();
    Task HiddenInputHasCorrectAttributes();
    Task InputHasId();

    // UncheckedValue
    Task RendersUncheckedValueHiddenInput();
    Task DoesNotRenderUncheckedValueWhenChecked();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataIndeterminateWhenIndeterminate();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();
    Task PlacesStyleHooksOnRootAndIndicator();

    // Controlled/Uncontrolled
    Task UncontrolledModeUsesDefaultChecked();
    Task ControlledModeRespectsCheckedParameter();
    Task UpdatesStateWhenControlledValueChanges();

    // Event callbacks
    Task InvokesOnCheckedChangeOnInputChange();
    Task InvokesCheckedChangedOnInputChange();
    Task DoesNotInvokeCallbacksWhenDisabled();
    Task DoesNotInvokeCallbacksWhenReadOnly();
    Task OnCheckedChangeCancellationPreventsStateChange();
    Task UpdatesStateWhenInputToggled();

    // Context cascading
    Task CascadesContextToChildren();

    // Element reference
    Task ExposesElementReference();

    // ClassValue/StyleValue state
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();
}
