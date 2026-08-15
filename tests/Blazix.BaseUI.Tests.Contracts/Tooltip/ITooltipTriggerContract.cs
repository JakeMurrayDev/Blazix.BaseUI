namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasAriaDescribedByWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDisabledAttributeWhenDisabled();
    Task DoesNotOpenWhenDisabled();
    Task ClosesOnPointerDownWhenOpen();
    Task ReinitializesJsHoverWhenDisableHoverablePopupChanges();
    Task InitializesJsHoverForHandleBackedTrigger();
    Task DoesNotAttachMouseHandlersWhenConsumerSuppliesNone();
    Task AttachesMouseLeaveHandlerWhileFocusOpenIsBlocked();
    Task ForwardsConsumerMouseEnterHandler();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
