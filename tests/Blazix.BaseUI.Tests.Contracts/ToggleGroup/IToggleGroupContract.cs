namespace Blazix.BaseUI.Tests.Contracts.ToggleGroup;

public interface IToggleGroupContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task HasRoleGroup();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledByDefault();
    Task HasDataMultipleWhenMultiple();
    Task DoesNotHaveDataMultipleByDefault();
    Task HasDataOrientationHorizontal();
    Task HasDataOrientationVertical();
    Task HasAriaOrientationHorizontal();
    Task HasAriaOrientationVertical();

    // Disabled
    Task DisabledGroup_PropagatesDataDisabledToToggles();
    Task DisabledGroup_BlocksValueChange();
    Task IndividualToggle_CanBeDisabled();

    // Value control
    Task ControlledValue_SetsPressedToggles();
    Task UncontrolledDefaultValue_SetsPressedToggles();
    Task Uncontrolled_TogglesOnClick();
    Task Uncontrolled_SingleMode_DeselectsPrevious();

    // Multiple mode
    Task Multiple_AllowsMultiplePressed();
    Task Single_OnlyOnePressed();

    // OnValueChange
    Task OnValueChange_ReceivesCorrectValue();
    Task OnValueChange_CanBeCanceled();

    // Context
    Task CascadesContextToChildren();
    Task ContextContainsDisabledState();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // TabIndex
    Task InitialRovingTabIndex_UsesFirstEnabledToggleNotPressedToggle();
    Task ToolbarDisabled_DisablesToggleGroupAndChildren();
}
