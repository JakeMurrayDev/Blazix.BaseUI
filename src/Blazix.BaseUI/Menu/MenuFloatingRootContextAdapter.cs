using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Menu;

/// <summary>
/// Adapts a <see cref="MenuRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/> and <see cref="FloatingTree.FloatingTree"/>.
/// </summary>
internal sealed class MenuFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly MenuRootContext context;

    public MenuFloatingRootContextAdapter(MenuRootContext context) => this.context = context;

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
    public Task SetOpenAsync(bool open) => context.SetOpenAsync(open, MenuOpenChangeReason.FocusOut, null);
}
