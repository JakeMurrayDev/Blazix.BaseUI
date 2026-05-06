namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectPositionerContract
{
    Task RendersPresentationDivByDefault();
    Task EmitsDataSideAndDataAlign();
    Task EmitsDataOpenWhenOpen();
    Task EmitsDataClosedWhenClosed();
    Task HiddenAttributeWhenNotMounted();
    Task ForwardsAdditionalAttributes();
    Task RendersInternalBackdropWhenMountedAndModal();
    Task OmitsInternalBackdropWhenNotModal();
    Task EmitsSideNoneWhenAlignItemWithTriggerActive();
    Task DoesNotEmitSideNoneForTouchOpen();
    Task RerenderUpdatesPositionWhenAnchorTrackingDisabled();
    Task ResetsScrollArrowVisibilityOnUnmount();
    Task UpdatesComputedStateOnPositionCallback();
    Task PrunesSingleSelectValueWhenItemDisappears();
    Task FallsBackToInitialValueWhenPruned();
    Task FiltersMultiSelectArrayOnItemMapChange();
    Task ExposesScrollArrowSettersOnPositionerContext();
}
