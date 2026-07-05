using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Combobox;

internal sealed class ComboboxPositionerContext
{
    public event Action? StateChanged;

    public Side Side { get; set; } = Side.Bottom;
    public Align Align { get; set; } = Align.Center;
    public bool AnchorHidden { get; set; }
    public bool ArrowUncentered { get; set; }
    public Func<ElementReference?> GetArrowElement { get; set; } = () => null;
    public Action<ElementReference?> SetArrowElement { get; set; } = _ => { };

    public void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
