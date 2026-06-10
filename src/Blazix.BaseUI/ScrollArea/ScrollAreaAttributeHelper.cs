namespace Blazix.BaseUI.ScrollArea;

internal static class ScrollAreaAttributeHelper
{
    public static void AddRootStateAttributes(Dictionary<string, object> attrs, ScrollAreaRootState state)
    {
        if (state.Scrolling)
        {
            attrs["data-scrolling"] = true;
        }

        if (state.HasOverflowX)
        {
            attrs["data-has-overflow-x"] = true;
        }

        if (state.HasOverflowY)
        {
            attrs["data-has-overflow-y"] = true;
        }

        if (state.OverflowXStart)
        {
            attrs["data-overflow-x-start"] = true;
        }

        if (state.OverflowXEnd)
        {
            attrs["data-overflow-x-end"] = true;
        }

        if (state.OverflowYStart)
        {
            attrs["data-overflow-y-start"] = true;
        }

        if (state.OverflowYEnd)
        {
            attrs["data-overflow-y-end"] = true;
        }
    }

    public static string CombineClassNames(string requiredClassName, string? userClassName)
    {
        if (string.IsNullOrWhiteSpace(userClassName))
        {
            return requiredClassName;
        }

        return $"{requiredClassName} {userClassName}";
    }
}
