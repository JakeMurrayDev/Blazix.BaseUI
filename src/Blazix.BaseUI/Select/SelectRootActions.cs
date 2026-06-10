namespace Blazix.BaseUI.Select;

/// <summary>
/// Provides imperative actions for a <see cref="SelectRoot{TValue}"/> component.
/// </summary>
public sealed class SelectRootActions
{
    /// <summary>
    /// Gets the action that forces the select popup to unmount from the DOM.
    /// </summary>
    public Action? Unmount { get; internal set; }
}
