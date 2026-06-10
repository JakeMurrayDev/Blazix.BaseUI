namespace Blazix.BaseUI.Tests.Contracts.Toolbar;

public interface IToolbarButtonContract
{
    // Rendering
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRenderFragment();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Native button attributes
    Task NativeButton_HasTypeButton();
    Task NativeButton_HasDisabledAttributeWhenDisabled();
    Task NativeButton_HasAriaDisabledWhenDisabledAndFocusable();
    Task NativeButton_DoesNotHaveRoleButton();

    // Non-native button attributes
    Task NonNativeButton_RendersAsDivElement();
    Task NonNativeButton_HasRoleButton();
    Task NonNativeButton_DoesNotHaveTypeButton();
    Task NonNativeButton_HasAriaDisabledWhenDisabled();

    // Data attributes
    Task HasDataOrientationFromRoot();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();
    Task HasDataFocusableByDefault();
    Task DoesNotHaveDataFocusableWhenFocusableWhenDisabledFalse();

    // Disabled cascading
    Task InheritsDisabledFromRoot();
    Task InheritsDisabledFromGroup();
    Task OwnDisabledTakesPrecedence();

    // State cascading
    Task ClassValueReceivesToolbarButtonState();

    // Validation
    Task ThrowsWhenNotInsideToolbarRoot();
}
