namespace Blazix.BaseUI.Tests.Contracts.Button;

public interface IButtonContract
{
    // Rendering
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Native button attributes
    Task NativeButton_HasTypeButton();
    Task NativeButton_HasDisabledAttributeWhenDisabled();
    Task NativeButton_DoesNotHaveAriaDisabledWhenDisabled();
    Task NativeButton_DoesNotHaveRoleButton();
    Task NativeButton_DoesNotHaveDisabledWhenNotDisabled();
    Task NativeButton_DisabledDoesNotHaveTabIndex();

    // Non-native button attributes
    Task NonNativeButton_HasRoleButton();
    Task NonNativeButton_DoesNotHaveTypeButton();
    Task NonNativeButton_HasAriaDisabledTrueWhenDisabled();
    Task NonNativeButton_HasTabIndexMinusOneWhenDisabled();
    Task NonNativeButton_DoesNotHaveAriaDisabledWhenNotDisabled();
    Task NonNativeButton_DoesNotHaveDisabledAttribute();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();

    // FocusableWhenDisabled - native
    Task NativeFocusableWhenDisabled_DoesNotHaveDisabledAttribute();
    Task NativeFocusableWhenDisabled_HasAriaDisabledTrue();
    Task NativeFocusableWhenDisabled_HasTabIndex();
    Task NativeFocusableWhenDisabled_HasDataDisabled();

    // FocusableWhenDisabled - non-native
    Task NonNativeFocusableWhenDisabled_HasAriaDisabledTrue();
    Task NonNativeFocusableWhenDisabled_HasTabIndex();
    Task NonNativeFocusableWhenDisabled_HasRoleButton();
    Task NonNativeFocusableWhenDisabled_HasDataDisabled();

    // TabIndex
    Task NativeButton_HasDefaultTabIndex();
    Task NonNativeButton_HasDefaultTabIndex();
    Task ForwardsExplicitTabIndex();
    Task NonNativeDisabled_HasTabIndexMinusOne();

    // State cascading
    Task CascadesButtonStateToClassValue();
    Task CascadesButtonStateDisabledTrue();
    Task CascadesButtonStateToStyleValue();

    // Element reference
    Task ExposesElementReference();

    // Disposal
    Task DisposeAsync_DoesNotThrow();
    Task DisposeAsync_SkipsJsWhenModuleNotCreated();
}
