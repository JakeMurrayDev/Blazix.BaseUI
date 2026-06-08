using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Adapts a <see cref="DialogRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/>.
/// </summary>
internal sealed class DialogFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly DialogRootContext context;

    public DialogFloatingRootContextAdapter(DialogRootContext context) => this.context = context;

    /// <inheritdoc />
    public string FloatingId => context.RootId;

    /// <inheritdoc />
    public bool GetOpen() => context.GetOpen();

    /// <inheritdoc />
    public ElementReference? GetTriggerElement() => context.GetTriggerElement();

    /// <inheritdoc />
    public ElementReference? GetPopupElement() => context.GetPopupElement();

    /// <inheritdoc />
    public void SetPopupElement(ElementReference element) => context.SetPopupElement(element);

    /// <inheritdoc />
    public Task SetOpenAsync(bool open) => context.SetOpenAsync(open, DialogOpenChangeReason.FocusOut);
}
