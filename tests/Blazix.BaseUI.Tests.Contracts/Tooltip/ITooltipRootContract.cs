using Blazix.BaseUI.Tests.Contracts;

namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipRootContract : IControlledTriggerLifecycleContract
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
    Task MapsTriggerPressCloseToInstantDismiss();
    Task DoesNotMapOutsidePressCloseToInstantDismiss();
    Task ClosesWhenActiveTriggerUnmounts();
    Task DisabledTriggerDoesNotCloseActiveTooltip();
    Task ActionsRefCloseMethodClosesTooltip();
    Task ActionsRefUnmountMethodUnmountsTooltip();
    Task CascadesContextToChildren();
}
