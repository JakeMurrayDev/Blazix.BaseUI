namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardTriggerContract
{
    Task RendersAsAnchorByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasDataPopupOpenWhenOpen();
    Task DoesNotOpenOnFocusAfterPointerDown();
    Task DoesNotOpenOnFocusAfterEscapeDismissal();
    Task DoesNotAttachMouseHandlersWhenConsumerSuppliesNone();
    Task AttachesMouseLeaveHandlerWhileFocusOpenIsBlocked();
    Task ForwardsConsumerMouseEnterHandler();
    Task DoesNotOpenOnFocusAfterEscapeDismissalWithHandleBackedTrigger();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
