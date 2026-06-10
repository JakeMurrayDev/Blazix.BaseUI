namespace Blazix.BaseUI.Tests.Contracts.Popover;

public interface IPopoverTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHaspopupDialog();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDisabledAttributeWhenDisabled();
    Task DoesNotToggleWhenDisabled();
    Task TogglesOnClick();
    Task HasFocusHandlersWhenOpenOnHover();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasAriaDisabledWhenDisabledCustomElement();
    Task HasDataPressedWhenOpenViaClick();
    Task DoesNotHaveDataPressedWhenClosed();
    Task DoesNotHaveDataPressedWhenHoverOpened();
    Task HasDataClickTriggerAttribute();
    Task InitializesHoverForHandleTrigger();
    Task RendersFocusGuardsWhenOpenAndNonModal();
    Task DoesNotRenderFocusGuardsWhenClosed();
    Task DoesNotRenderFocusGuardsWhenModal();
}
