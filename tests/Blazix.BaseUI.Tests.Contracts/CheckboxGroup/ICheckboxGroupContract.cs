namespace Blazix.BaseUI.Tests.Contracts.CheckboxGroup;

public interface ICheckboxGroupContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task HasRoleGroup();

    // Value control
    Task ControlledValue_SetsCheckedCheckboxes();
    Task UncontrolledValue_UsesDefaultValue();
    Task UncontrolledValue_UpdatesOnCheckboxClick();

    // DefaultValue
    Task DefaultValue_InitializesCorrectCheckboxes();
    Task DefaultValue_AllowsToggling();

    // OnValueChange
    Task OnValueChange_CalledWhenCheckboxClicked();
    Task OnValueChange_ReceivesUpdatedValueArray();
    Task OnValueChange_CanBeCanceled();

    // Disabled
    Task Disabled_PropagesToAllCheckboxes();
    Task Disabled_PreventsValueChange();
    Task NotDisabled_AllowsCheckboxInteraction();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataValidWhenValid();
    Task HasDataInvalidWhenInvalid();
    Task HasDataTouchedWhenTouched();
    Task HasDataDirtyWhenDirty();
    Task HasDataFilledWhenFilled();

    // Context
    Task CascadesContextToChildren();
    Task ContextContainsCorrectValue();
    Task ContextContainsDisabledState();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // Parent checkbox
    Task ParentCheckbox_CheckedWhenAllChildrenChecked();
    Task ParentCheckbox_IndeterminateWhenSomeChildrenChecked();
    Task ParentCheckbox_UncheckedWhenNoChildrenChecked();
}
