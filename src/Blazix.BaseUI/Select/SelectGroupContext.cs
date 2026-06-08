namespace Blazix.BaseUI.Select;

/// <summary>
/// Defines the contract for a group context that manages the association between a group and its label.
/// </summary>
internal interface ISelectGroupContext
{
    /// <summary>
    /// Sets the id of the label element associated with the group.
    /// </summary>
    void SetLabelId(string? id);
}

/// <summary>
/// Provides shared state for a <see cref="SelectGroup"/> and its descendant <see cref="SelectGroupLabel"/>.
/// </summary>
internal sealed class SelectGroupContext : ISelectGroupContext
{
    /// <summary>
    /// Gets or sets the delegate that sets the label id on the parent group.
    /// </summary>
    public Action<string?> SetLabelIdAction { get; init; } = null!;

    /// <inheritdoc />
    public void SetLabelId(string? id) => SetLabelIdAction(id);
}
