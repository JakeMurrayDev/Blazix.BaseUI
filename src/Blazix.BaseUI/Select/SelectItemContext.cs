using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Select;

/// <summary>
/// Provides shared state for a <see cref="SelectItem{TValue}"/> and its descendant components.
/// Mirrors the React <c>SelectItemContext</c> (selected, indexRef, textRef, selectedByFocus, hasRegistered).
/// </summary>
internal sealed class SelectItemContext
{
    /// <summary>
    /// Gets or sets whether the parent item is selected.
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// Gets or sets the parent item's ordinal index within the registered value map.
    /// <c>-1</c> until the item has registered. Mirrors React <c>indexRef.current</c>.
    /// </summary>
    public int Index { get; set; } = -1;

    /// <summary>
    /// Gets or sets whether the parent item has completed registration with the root.
    /// Descendants (e.g., <see cref="SelectItemText"/>) should defer root-level
    /// side effects until this becomes <see langword="true"/>.
    /// Mirrors React <c>hasRegistered</c>.
    /// </summary>
    public bool HasRegistered { get; set; }

    /// <summary>
    /// Gets or sets whether the parent item is the focus-selected item (i.e., its
    /// index equals the root's <see cref="ISelectRootContext.SelectedIndex"/>).
    /// Mirrors React <c>selectedByFocus</c>.
    /// </summary>
    public bool SelectedByFocus { get; set; }

    /// <summary>
    /// Gets or sets the callback that <see cref="SelectItemText"/> invokes to report its
    /// captured <see cref="ElementReference"/> to the parent <see cref="SelectItem{TValue}"/>.
    /// The parent item uses the element's <c>textContent</c> as a lazy typeahead label fallback
    /// when <see cref="SelectItem{TValue}.Label"/> is not supplied. Pass <see langword="null"/>
    /// on dispose to clear the registration. Mirrors the React <c>textRef</c> merged into
    /// <see cref="SelectItemText"/>'s ref array.
    /// </summary>
    public Action<ElementReference?>? SetTextElement { get; set; }
}
