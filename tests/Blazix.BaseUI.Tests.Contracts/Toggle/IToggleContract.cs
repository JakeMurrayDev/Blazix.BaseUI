namespace Blazix.BaseUI.Tests.Contracts.Toggle;

public interface IToggleContract
{
    // Rendering
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA
    Task HasAriaPressedFalseByDefault();
    Task HasAriaPressedTrueWhenPressed();

    // Native button attributes
    Task NativeButton_HasTypeButton();
    Task NativeButton_OverridesUserTypeAndOmitsForm();
    Task NativeButton_HasDisabledWhenDisabled();
    Task NativeButton_DoesNotHaveAriaDisabled();
    Task NativeButton_DoesNotHaveRoleButton();

    // Non-native button attributes
    Task NonNativeButton_HasRoleButton();
    Task NonNativeButton_DoesNotHaveType();
    Task NonNativeButton_HasAriaDisabledWhenDisabled();
    Task NonNativeButton_HasTabIndexMinusOneWhenDisabled();

    // Data attributes
    Task HasDataPressedWhenPressed();
    Task DoesNotHaveDataPressedWhenNotPressed();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();

    // TabIndex
    Task NativeButton_HasDefaultTabIndex();
    Task NonNativeButton_HasDefaultTabIndex();

    // State cascading
    Task ClassValueReceivesToggleState();
    Task ClassValueReceivesPressedTrue();
    Task ClassValueReceivesDisabledTrue();

    // Uncontrolled state
    Task Uncontrolled_DefaultPressedTrue();
    Task Uncontrolled_TogglesOnClick();

    // Controlled state
    Task Controlled_ReflectsParentState();

    // OnPressedChange
    Task OnPressedChange_FiresOnClick();
    Task GroupedToggle_OnPressedChangeFiresOnClick();
    Task GroupedNativeToggle_ExposesAriaDisabledState();
    Task GroupedEmptyStringValues_AreResolvedToUniqueGeneratedValues();
    Task Disabled_OnPressedChangeDoesNotFire();

    // Element reference
    Task ExposesElementReference();
}
