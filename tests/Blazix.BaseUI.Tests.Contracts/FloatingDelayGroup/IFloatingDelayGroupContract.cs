namespace Blazix.BaseUI.Tests.Contracts.FloatingDelayGroup;

public interface IFloatingDelayGroupContract
{
    Task RendersChildContent();
    Task CreatesGroupOnFirstRender();
    Task ProvidesGroupContext();
    Task PassesOpenDelayMs();
    Task PassesCloseDelayMs();
    Task PassesTimeoutMs();
    Task DisposesGroupOnDispose();
    Task NotifiesMemberOpenedCallsJs();
    Task NotifiesMemberClosedCallsJs();
    Task RegistersMemberWithCallbacks();
    Task SetIsInstantPhaseUpdatesContext();
    Task ContextGetDelayReturnsInstantWhenInInstantPhase();
    Task ContextGetDelayReturnsNormalWhenNotInInstantPhase();
    Task ContextHasProviderIsTrue();
    Task UpdatesOptionsWhenParametersChange();
    Task UnregistersMemberCallsJs();
}
