namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardRootContract
{
    Task RendersChildren();
    Task ControlledOpenPropShowsPopup();
    Task OpensByDefaultWhenDefaultOpenTrue();
    Task RemainsClosedWhenDefaultOpenFalseControlled();
    Task RemainsOpenWhenDefaultOpenTrueAndOpenTrue();
    Task DefaultOpenRemainsUncontrolled();
    Task OpensOnTriggerHover();
    Task ClosesOnTriggerUnhover();
    Task OpensOnTriggerFocus();
    Task ClosesOnTriggerBlur();
    Task CallsOnOpenChangeWhenOpenStateChanges();
    Task DoesNotCallOnOpenChangeWhenStateUnchanged();
    Task OnOpenChangeCancelPreventsOpening();
    Task ActionsRefCloseMethodClosesPreviewCard();
    Task ActionsRefUnmountMethodUnmountsPreviewCard();
    Task CascadesContextToChildren();
}
