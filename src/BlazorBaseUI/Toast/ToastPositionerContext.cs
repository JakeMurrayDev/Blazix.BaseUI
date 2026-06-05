using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Toast;

internal sealed class ToastPositionerContext
{
    public Side Side { get; set; } = Side.Top;

    public Align Align { get; set; } = Align.Center;

    public bool AnchorHidden { get; set; }

    public bool ArrowUncentered { get; set; }

    public ElementReference? ArrowElement { get; set; }

    public Action<ElementReference?> SetArrowElement { get; set; } = _ => { };
}
