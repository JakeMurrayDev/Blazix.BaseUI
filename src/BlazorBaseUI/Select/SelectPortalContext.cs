namespace BlazorBaseUI.Select;

/// <summary>
/// Provides cascading state from the <see cref="SelectPortal"/> to child select components.
/// Mirrors the React <c>SelectPortalContext</c> used by <c>useSelectPortalContext</c> to
/// assert a portal ancestor is present.
/// </summary>
internal sealed class SelectPortalContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the portal content remains mounted when the select is closed.
    /// </summary>
    public bool KeepMounted { get; set; }
}
