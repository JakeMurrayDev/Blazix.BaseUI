namespace BlazorBaseUI.Collapsible;

/// <summary>
/// Provides cascading state from <see cref="CollapsibleRoot"/> to child components.
/// </summary>
internal sealed class CollapsibleRootContext
{
    /// <summary>Whether the collapsible is currently open.</summary>
    public bool Open { get; set; }

    /// <summary>Whether the collapsible is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>The current transition status of the collapsible.</summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>The element identifier of the panel.</summary>
    public string PanelId { get; set; } = string.Empty;

    /// <summary>The callback to invoke when the trigger is activated.</summary>
    public Func<Task> HandleTrigger { get; set; } = null!;

    /// <summary>The callback to invoke when a <c>beforematch</c> event occurs.</summary>
    public Func<Task> HandleBeforeMatch { get; set; } = null!;

    /// <summary>The callback to register the panel element identifier.</summary>
    public Action<string> SetPanelId { get; set; } = null!;

    /// <summary>The callback to update the transition status.</summary>
    public Action<TransitionStatus> SetTransitionStatus { get; set; } = null!;
}
