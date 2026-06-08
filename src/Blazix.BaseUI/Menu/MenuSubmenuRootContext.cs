namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides shared state for a <see cref="MenuSubmenuRoot"/> and its descendant components.
/// </summary>
internal sealed class MenuSubmenuRootContext
{
    /// <summary>
    /// Gets the parent menu's root context.
    /// </summary>
    public MenuRootContext? ParentMenu { get; init; }
}
