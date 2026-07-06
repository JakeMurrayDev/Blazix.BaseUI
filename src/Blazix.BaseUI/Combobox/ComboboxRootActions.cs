namespace Blazix.BaseUI.Combobox;

/// <summary>
/// Exposes imperative actions for <see cref="ComboboxRoot{TValue}"/>.
/// </summary>
public sealed class ComboboxRootActions
{
    /// <summary>
    /// Unmounts a force-mounted combobox popup after external animation completes.
    /// </summary>
    public Func<ValueTask>? Unmount { get; set; }
}
