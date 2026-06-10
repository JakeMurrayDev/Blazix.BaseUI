namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipRootContract
{
    Task RendersChildren();
    Task OpensByDefaultWhenDefaultOpenTrue();
    Task RemainsClosedWhenDefaultOpenFalseControlled();
    Task RemainsOpenWhenDefaultOpenTrueAndOpenTrue();
    Task DefaultOpenRemainsUncontrolled();
    Task CallsOnOpenChangeWhenOpenStateChanges();
    Task DoesNotCallOnOpenChangeWhenStateUnchanged();
    Task OnOpenChangeCancelPreventsOpening();
    Task ShouldNotOpenWhenDisabled();
    Task DisabledPreventsSubsequentOpens();
    Task DisabledDoesNotPreventInitialDefaultOpen();
    Task ActionsRefCloseMethodClosesTooltip();
    Task ActionsRefUnmountMethodUnmountsTooltip();
    Task CascadesContextToChildren();
}
