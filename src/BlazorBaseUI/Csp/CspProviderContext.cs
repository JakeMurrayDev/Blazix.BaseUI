namespace BlazorBaseUI.Csp;

/// <summary>
/// Provides CSP options to components that need to inject runtime style elements.
/// </summary>
internal sealed record CspProviderContext(string? Nonce, bool DisableStyleElements);
