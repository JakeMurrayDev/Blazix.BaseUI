namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Provides shared delay configuration for child components of a <see cref="TooltipProvider"/>.
/// </summary>
internal sealed class TooltipProviderContext
{
    /// <summary>
    /// Gets or sets the delay in milliseconds before the tooltip opens.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds before the tooltip closes.
    /// </summary>
    public int? CloseDelay { get; set; }

    private Func<bool> isInInstantPhaseFunc = () => false;

    /// <summary>
    /// Sets the delegate that determines whether the provider is in the instant phase.
    /// </summary>
    /// <param name="provider">A function returning <see langword="true"/> when in the instant phase.</param>
    public void SetInstantPhaseProvider(Func<bool> provider) => isInInstantPhaseFunc = provider;

    /// <summary>
    /// Determines whether the provider is in the instant phase where tooltips open without delay.
    /// </summary>
    /// <returns><see langword="true"/> if within the instant phase timeout; otherwise, <see langword="false"/>.</returns>
    public bool IsInInstantPhase() => isInInstantPhaseFunc();
}
