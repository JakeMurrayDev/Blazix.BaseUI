namespace Blazix.BaseUI.Autocomplete;

/// <summary>
/// Exposes imperative actions for <see cref="AutocompleteRoot{TValue}"/>.
/// </summary>
public sealed class AutocompleteRootActions
{
    /// <summary>
    /// Unmounts a force-mounted autocomplete popup after external animation completes.
    /// </summary>
    public Func<ValueTask>? Unmount { get; set; }
}
