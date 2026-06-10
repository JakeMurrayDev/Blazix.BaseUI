using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Toast;

internal static class ToastRenderFragmentUtilities
{
    public static RenderFragment? ToRenderFragment(object? value)
    {
        return value switch
        {
            null => null,
            RenderFragment fragment => fragment,
            MarkupString markup => builder => builder.AddContent(0, markup),
            _ => builder => builder.AddContent(0, value)
        };
    }
}
