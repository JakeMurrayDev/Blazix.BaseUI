using Microsoft.AspNetCore.Components;
using System.Collections;

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

    public static bool IsRenderableValue(object? value)
    {
        return value switch
        {
            null => false,
            bool => false,
            string text => text.Length > 0,
            MarkupString markup => markup.Value.Length > 0,
            IEnumerable values => values.Cast<object?>().Any(IsRenderableValue),
            _ => true
        };
    }
}
