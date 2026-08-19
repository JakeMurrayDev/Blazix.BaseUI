namespace Blazix.BaseUI.Fieldset;

/// <summary>
/// Provides contextual information for components within a <see cref="FieldsetRoot"/>.
/// </summary>
internal interface IFieldsetRootContext
{
    /// <summary>
    /// Gets the ID of the associated <see cref="FieldsetLegend"/>.
    /// </summary>
    string? LegendId { get; }

    /// <summary>
    /// Gets whether the fieldset is disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Sets the legend element's ID for ARIA labelling.
    /// </summary>
    /// <param name="owner">The legend instance registering or clearing the ID.</param>
    /// <param name="id">The legend element ID, or <see langword="null"/> to clear.</param>
    /// <remarks>
    /// A clear is ignored unless <paramref name="owner"/> is still the registered legend.
    /// </remarks>
    void SetLegendId(object owner, string? id);
}

/// <summary>
/// Default implementation of <see cref="IFieldsetRootContext"/>.
/// </summary>
internal sealed class FieldsetRootContext : IFieldsetRootContext
{
    /// <inheritdoc />
    public string? LegendId { get; set; }

    /// <summary>
    /// Gets or sets the delegate used to set the legend ID.
    /// </summary>
    public Action<object, string?> SetLegendId { get; set; } = null!;

    /// <inheritdoc />
    public bool Disabled { get; set; }

    /// <inheritdoc />
    void IFieldsetRootContext.SetLegendId(object owner, string? id) => SetLegendId(owner, id);
}
