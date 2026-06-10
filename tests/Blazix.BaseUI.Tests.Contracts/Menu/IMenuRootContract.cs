namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuRootContract
{
    Task CascadesContextToChildren();
    Task ControlledModeRespectsOpenParameter();
    Task UncontrolledModeUsesDefaultOpen();
    Task InvokesOnOpenChangeWithReason();
    Task InvokesOnOpenChangeComplete();
    Task DisabledStatePreventsTriggerInteraction();
    Task SupportsModalModes();
    Task SupportsOrientations();
    Task ActionsRefProvidesCloseMethod();
    Task ChildContentReceivesPayloadContext();
}
