using Microsoft.JSInterop;

namespace BlazorBaseUI.FloatingDelayGroup;

/// <summary>
/// Provides delay group state for coordinated hover delays across grouped floating elements.
/// Cascaded by <see cref="FloatingDelayGroup"/> to all descendant components.
/// </summary>
public sealed class FloatingDelayGroupContext
{
    /// <summary>
    /// Occurs when delay group state changes and descendants should re-evaluate derived delay state.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Gets the unique identifier of the delay group.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this context was provided by a <see cref="FloatingDelayGroup"/>.
    /// </summary>
    public bool HasProvider { get; init; }

    /// <summary>
    /// Gets a value indicating whether the group is in the instant phase
    /// (a member was recently open, so subsequent opens should be immediate).
    /// </summary>
    public bool IsInstantPhase { get; set; }

    /// <summary>
    /// Gets or sets the delegate that returns the current delay values.
    /// Returns (openDelay, closeDelay) in milliseconds.
    /// </summary>
    public Func<(int OpenDelayMs, int CloseDelayMs)> GetDelay { get; init; } = null!;

    /// <summary>
    /// Gets or sets the delegate that registers a member with the delay group via JS interop.
    /// Parameters: interactionId, closeMemberRef.
    /// </summary>
    public Func<string, DotNetObjectReference<DelayGroupMemberCallback>, Task> RegisterMemberAsync { get; init; } = null!;

    /// <summary>
    /// Gets or sets the delegate that unregisters a member from the delay group via JS interop.
    /// </summary>
    public Func<string, Task> UnregisterMemberAsync { get; init; } = null!;

    /// <summary>
    /// Gets or sets the delegate that notifies the group when a member opens.
    /// </summary>
    public Func<string, Task> NotifyMemberOpenedAsync { get; init; } = null!;

    /// <summary>
    /// Gets or sets the delegate that notifies the group when a member closes.
    /// </summary>
    public Func<string, Task> NotifyMemberClosedAsync { get; init; } = null!;

    /// <summary>
    /// Notifies subscribers that delay group state changed.
    /// </summary>
    public void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
