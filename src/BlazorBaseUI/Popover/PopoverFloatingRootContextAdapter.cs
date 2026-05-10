using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Popover;

/// <summary>
/// Adapts a <see cref="PopoverRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/> and <see cref="FloatingTree.FloatingTree"/>.
/// </summary>
internal sealed class PopoverFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly PopoverRootContext _context;

    public PopoverFloatingRootContextAdapter(PopoverRootContext context) => _context = context;

    /// <inheritdoc />
    public string FloatingId => _context.RootId;

    /// <inheritdoc />
    public bool GetOpen() => _context.GetOpen();

    /// <inheritdoc />
    public ElementReference? GetTriggerElement() => _context.GetTriggerElement();

    /// <inheritdoc />
    public ElementReference? GetPopupElement() => _context.GetPopupElement?.Invoke();

    /// <inheritdoc />
    public void SetPopupElement(ElementReference element) => _context.SetPopupElement(element);

    /// <inheritdoc />
    public Task SetOpenAsync(bool open) => _context.SetOpenAsync(open, PopoverOpenChangeReason.FocusOut, null, null);
}
