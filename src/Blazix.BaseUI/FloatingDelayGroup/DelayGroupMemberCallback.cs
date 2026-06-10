using Microsoft.JSInterop;

namespace Blazix.BaseUI.FloatingDelayGroup;

/// <summary>
/// Wraps close and instant-phase callbacks for a delay group member, invokable from JavaScript.
/// </summary>
public sealed class DelayGroupMemberCallback(Func<string?, Task> closeAsync, Action<bool>? setIsInstantPhase = null)
{

    /// <summary>
    /// Gets a value indicating whether this member is in the instant phase
    /// (another member was recently open, so opens/closes should be immediate).
    /// </summary>
    public bool IsInstantPhase { get; private set; }

    /// <summary>
    /// Invoked by JS to close this member when another member opens.
    /// Optionally includes a reason string describing why the member was closed.
    /// </summary>
    [JSInvokable]
    public Task CloseMember(string? reason = null) => closeAsync(reason);

    /// <summary>
    /// Invoked by JS to set this member's instant-phase state.
    /// </summary>
    [JSInvokable]
    public void SetMemberInstantPhase(bool value)
    {
        IsInstantPhase = value;
        setIsInstantPhase?.Invoke(value);
    }
}
