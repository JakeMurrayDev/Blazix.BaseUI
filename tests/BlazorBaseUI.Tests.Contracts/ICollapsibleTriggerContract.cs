namespace BlazorBaseUI.Tests.Contracts;

public interface ICollapsibleTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasAriaControlsWhenOpen();
    Task HasNoAriaControlsWhenClosed();
    Task HasDataPanelOpenWhenOpen();
    Task HasNoDataPanelOpenWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasAriaDisabledWhenDisabled();
    Task TogglesOnClick();
    Task DoesNotToggleWhenDisabled();
    Task ReceivesCorrectState();
    Task ReferencesCustomPanelIdInAriaControls();
    Task RequiresContext();
}
