namespace Blazix.BaseUI.Tests.Contracts;

/// <summary>
/// Defines the shared controlled lifecycle expected from roots that support multiple triggers.
/// </summary>
public interface IControlledTriggerLifecycleContract
{
    /// <summary>
    /// Verifies external open, trigger reassociation, close, and same-trigger reopen behavior,
    /// including payload and handle-state synchronization.
    /// </summary>
    Task ControlledTriggerLifecyclePreservesAssociationAndPayload();
}
