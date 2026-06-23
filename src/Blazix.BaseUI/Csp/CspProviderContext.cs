namespace Blazix.BaseUI.Csp;

/// <summary>
/// Provides CSP options to components that need to inject runtime style elements.
/// </summary>
internal sealed class CspProviderContext
{
    /// <summary>
    /// Gets or sets the nonce applied to runtime style elements.
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// Gets or sets whether components should skip injecting runtime style elements.
    /// </summary>
    public bool DisableStyleElements { get; set; }
}
