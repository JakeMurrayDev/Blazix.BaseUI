using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Popover;

/// <summary>
/// Adapts a <see cref="PopoverRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/> and <see cref="FloatingTree.FloatingTree"/>.
/// </summary>
internal sealed class PopoverFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly PopoverRootContext context;

    public PopoverFloatingRootContextAdapter(PopoverRootContext context) => this.context = context;

    /// <inheritdoc />
    public string FloatingId => context.RootId;

    /// <inheritdoc />
    public bool GetOpen() => context.GetOpen();

    /// <inheritdoc />
    public ElementReference? GetTriggerElement() => context.GetTriggerElement();

    /// <inheritdoc />
    public ElementReference? GetPopupElement() => context.GetPopupElement?.Invoke();

    /// <inheritdoc />
    public void SetPopupElement(ElementReference element) => context.SetPopupElement(element);

    /// <inheritdoc />
    public Task SetOpenAsync(bool open) => context.SetOpenAsync(open, PopoverOpenChangeReason.FocusOut, null, null, null, null);

    /// <inheritdoc />
    public InteractionType CloseInteractionType => context.CloseInteractionType;
}
