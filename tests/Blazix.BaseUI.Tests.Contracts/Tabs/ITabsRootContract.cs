namespace Blazix.BaseUI.Tests.Contracts.Tabs;

public interface ITabsRootContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Data attributes
    Task HasDataActivationDirectionNoneByDefault();
    Task HasDataOrientationHorizontalByDefault();

    // State
    Task ClassValueReceivesTabsRootState();
    Task ClassValueReceivesOrientationVertical();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task AcceptsNullChildren();

    // Value management
    Task SetsDefaultValueOnInit();
    Task SupportsControlledValue();
    Task FiresValueChangedCallback();
    Task FiresOnValueChangeWithCancellation();
    Task FiresOnValueChangeWithInitialReasonForImplicitAutoSelection();
    Task AutomaticInitialSelectionCannotBeCanceled();
    Task ExplicitNullDefaultValueDoesNotAutoSelect();
    Task ValueParameterControlsSelectionWithoutValueChanged();
    Task SelectedTabDisabledAfterInitialValidityFallsBackWithDisabledReason();

    // Composite (Tab + Panel integration)
    Task SetsAriaControlsOnTabsToCorrespondingPanelId();
    Task SetsAriaLabelledbyOnPanelsToCorrespondingTabId();
    Task PutsSelectedChildInTabOrder();
    Task ShowsOnlyActivePanelContent();
    Task DisabledTabAutoSelectsFirstEnabled();
    Task DisabledTabAutoSelectsThirdWhenFirstTwoDisabled();
    Task HonorsExplicitDefaultValueOnDisabledTab();
    Task HonorsExplicitValueOnDisabledTab();
    Task NoTabSelectedWhenAllDisabled();
    Task SyncsAriaControlsWithKeepMountedFalse();
}
