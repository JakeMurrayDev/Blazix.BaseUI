namespace Blazix.BaseUI.Tests.Contracts.Accordion;

public interface IAccordionPanelContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRoleRegion();
    Task HasAriaLabelledbyPointingToTrigger();
    Task HasIdMatchingTriggerAriaControls();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasDataIndexAttribute();
    Task HasDataOrientationAttribute();
    Task IsHiddenWhenClosed();
    Task IsHiddenWhenKeptMountedAndClosed();
    Task UsesHiddenUntilFoundWhenClosed();
    Task IsVisibleWhenOpen();
    Task KeepsMountedWhenKeepMountedTrue();
    Task OpensFromBeforeMatchWhenItemIsDisabled();
    Task HasIdleTransitionStateWhenInitiallyOpen();
    Task UpdatesTransitionStyleAttributesFromJsCallback();
    Task KeepsPanelAndTriggerIdsSynchronizedWhenPanelRendersBeforeTrigger();
}
