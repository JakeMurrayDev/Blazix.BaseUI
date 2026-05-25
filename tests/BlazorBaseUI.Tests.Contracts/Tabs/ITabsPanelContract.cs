namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsPanelContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA
    Task HasRoleTabpanel();
    Task HasTabIndex0WhenVisible();
    Task HasTabIndexMinus1WhenHidden();
    Task HasAriaLabelledbyPointingToTab();

    // Visibility
    Task HasHiddenWhenNotActive();
    Task DoesNotHaveHiddenWhenActive();
    Task DoesNotRenderWhenNotActiveAndKeepMountedFalse();
    Task RendersHiddenWhenNotActiveAndKeepMountedTrue();

    // Data attributes
    Task HasDataOrientationHorizontal();
    Task HasDataActivationDirection();
    Task HasDataIndex();
    Task HasDataHiddenWhenNotActive();
    Task DoesNotHaveDataHiddenWhenActive();
    Task HasInertWhenHiddenAndKeptMounted();
    Task ClassValueReceivesTransitionStatus();
    Task AddsStartingStyleWhenPanelOpens();
    Task KeepsExitingPanelVisibleUntilTransitionCompletes();

    // State
    Task ClassValueReceivesTabsPanelState();
    Task ClassValueReceivesHiddenTrue();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task ThrowsWhenNotInTabsRoot();
}
