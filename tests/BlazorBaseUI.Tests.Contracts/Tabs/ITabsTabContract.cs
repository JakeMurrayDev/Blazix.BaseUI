namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsTabContract
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
    Task HasRoleTab();
    Task HasTypeButton();
    Task HasAriaSelectedTrueWhenActive();
    Task HasAriaSelectedFalseWhenInactive();
    Task HasTabIndex0WhenActive();
    Task HasTabIndexMinus1WhenInactive();
    Task HasAriaControlsPointingToPanel();

    // Data attributes
    Task HasDataOrientationHorizontal();
    Task HasDataActiveWhenActive();
    Task DoesNotHaveDataActiveWhenInactive();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();

    // Disabled behavior
    Task HasAriaDisabledWhenDisabled();
    Task DisabledActiveTabDoesNotEnterTabOrder();
    Task RightClickDoesNotActivateTab();
    Task InvokesClickHandlerWhenTabIsAlreadyActive();
    Task InvokesFocusHandlerWhenTabIsDisabled();

    // State
    Task ClassValueReceivesTabsTabState();
    Task ClassValueReceivesActiveTrue();
    Task ClassValueReceivesDisabledTrue();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task ThrowsWhenNotInTabsRoot();
}
