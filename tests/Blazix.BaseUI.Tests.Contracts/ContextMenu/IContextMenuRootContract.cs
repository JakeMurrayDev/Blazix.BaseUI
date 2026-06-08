namespace Blazix.BaseUI.Tests.Contracts.ContextMenu;

public interface IContextMenuRootContract
{
    Task CascadesContextToTrigger();
    Task UncontrolledModeUsesDefaultOpen();
    Task ControlledModeRespectsOpenParameter();
    Task RendersWithOnOpenChangeCallback();
    Task DisabledStatePreventsInteraction();
    Task SupportsOrientations();
    Task SetsParentTypeToContextMenu();
    Task OmitsModalOpenOnHoverDelayCloseDelayProps();
    Task ForwardsDefaultTriggerIdToMenuRoot();
    Task ForwardsHandleToMenuRoot();
}
