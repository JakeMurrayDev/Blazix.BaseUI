namespace Blazix.BaseUI.Tests.Contracts.FloatingTree;

public interface IFloatingNodeContract
{
    Task RendersChildContent();
    Task RegistersWithTreeOnInit();
    Task UnregistersOnDispose();
    Task HasNullParentIdWhenTopLevel();
    Task PicksUpParentIdFromOuterNode();
    Task ProvidesNodeContext();
    Task ExposesSetContextCallback();
    Task PassesTreeContextReferenceToNodeContext();
    Task DelegatesEventSubscriptionToTree();
    Task RegistersNodeWithJsOnFirstRender();
    Task RemovesNodeFromJsOnDispose();
    Task UsesConsumerProvidedId();
    Task FallsBackToGeneratedIdWhenIdNotProvided();
    Task ReportsOpenStateFromContext();
}
