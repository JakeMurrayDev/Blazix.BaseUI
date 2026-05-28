namespace BlazorBaseUI.Tests.Contracts.RadioGroup;

public interface IRadioGroupContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AdditionalAttributesCanOverrideBuiltInRole();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task HasRoleRadiogroup();

    // Value control
    Task ControlledValue_SetsCheckedRadio();
    Task ValueParameterControlsSelectionWithoutValueChanged();
    Task UncontrolledValue_UsesDefaultValue();
    Task UncontrolledValue_UpdatesOnRadioClick();

    // OnValueChange
    Task OnValueChange_CalledWhenRadioClicked();
    Task OnValueChange_ReceivesSelectedValue();
    Task OnValueChange_CanBeCanceled();

    // Disabled
    Task HasAriaDisabledWhenDisabled();
    Task DoesNotHaveAriaDisabledByDefault();
    Task Disabled_PropagesToAllRadios();
    Task Disabled_PreventsValueChange();

    // ReadOnly
    Task HasAriaReadonlyWhenReadOnly();
    Task DoesNotHaveAriaReadonlyByDefault();
    Task ReadOnly_PreventsValueChange();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();
    Task HasDataValidWhenValid();
    Task HasDataInvalidWhenInvalid();
    Task HasDataTouchedWhenTouched();
    Task HasDataDirtyWhenDirty();
    Task HasDataFilledWhenFilled();

    // Style hooks on children
    Task PlacesStyleHooksOnGroupRadioAndIndicator();

    // Value prop
    Task DoesNotForwardValuePropToElement();
    Task SetsTabindexZeroOnSelectedRadioOnly();

    // Hidden input and form ownership
    Task DoesNotRenderGroupLevelHiddenRadioInput();
    Task FormPropPassesToEachRadioInput();

    // Context
    Task CascadesContextToChildren();
    Task ContextContainsCorrectValue();
    Task ContextContainsDisabledState();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

}
