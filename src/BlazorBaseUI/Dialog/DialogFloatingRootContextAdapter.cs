using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Dialog;

/// <summary>
/// Adapts a <see cref="DialogRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/>.
/// </summary>
internal sealed class DialogFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly DialogRootContext _context;

    public DialogFloatingRootContextAdapter(DialogRootContext context) => _context = context;

    /// <inheritdoc />
    public string FloatingId => _context.RootId;

    /// <inheritdoc />
    public bool GetOpen() => _context.GetOpen();

    /// <inheritdoc />
    public ElementReference? GetTriggerElement() => _context.GetTriggerElement();

    /// <inheritdoc />
    public ElementReference? GetPopupElement() => _context.GetPopupElement();

    /// <inheritdoc />
    public void SetPopupElement(ElementReference element) => _context.SetPopupElement(element);

    /// <inheritdoc />
    public Task SetOpenAsync(bool open) => _context.SetOpenAsync(open, DialogOpenChangeReason.FocusOut);
}
