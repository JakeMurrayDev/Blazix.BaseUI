using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Select;

/// <summary>
/// Adapts an <see cref="ISelectRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/>.
/// </summary>
internal sealed class SelectFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly ISelectRootContext rootContext;

    public SelectFloatingRootContextAdapter(ISelectRootContext context) => rootContext = context;

    /// <inheritdoc />
    public string FloatingId => rootContext.RootId;

    /// <inheritdoc />
    public bool GetOpen() => rootContext.GetOpen();

    /// <inheritdoc />
    public ElementReference? GetTriggerElement() => rootContext.GetTriggerElement();

    /// <inheritdoc />
    public ElementReference? GetPopupElement() => rootContext.GetPopupElement();

    /// <inheritdoc />
    public void SetPopupElement(ElementReference element) => rootContext.SetPopupElement(element);

    /// <inheritdoc />
    public Task SetOpenAsync(bool open) => rootContext.SetOpenAsync(open, SelectOpenChangeReason.FocusOut);

    /// <inheritdoc />
    public InteractionType CloseInteractionType => rootContext.CloseInteractionType;
}
