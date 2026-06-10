namespace Blazix.BaseUI.Tests.Contracts.FloatingTree;

public interface IFloatingTreeContract
{
    Task RendersChildContent();
    Task ProvidesTreeContext();
    Task GeneratesUniqueTreeId();
    Task InitializesJsTreeOnFirstRender();
    Task DisposesJsTreeOnDispose();
    Task EmitsEventToRegisteredHandlers();
    Task RemovesHandlerOnOff();
    Task EmitDoesNothingWithNoHandlers();
    Task UsesExternalTreeWhenProvided();
    Task GetNodeChildrenReturnsRecursiveOpenChildren();
    Task GetNodeChildrenReturnsAllChildrenWhenOnlyOpenChildrenIsFalse();
    Task GetDeepestNodeReturnsDeepestOpenDescendant();
    Task GetDeepestNodeReturnsSelfWhenNoChildren();
    Task AcceptsTypedExternalTree();
    Task GetNodeAncestorsReturnsParentToRoot();
    Task GetNodeAncestorsReturnsEmptyForRootNode();
    Task GetNodeAncestorsStopsAtMissingParent();
}
