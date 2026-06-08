namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Internal extension methods for the navigation menu component.
/// </summary>
internal static class Extensions
{
    extension(NavigationMenuOrientation orientation)
    {
        /// <summary>
        /// Converts a <see cref="NavigationMenuOrientation"/> value to its corresponding data attribute string.
        /// </summary>
        public string ToDataAttributeString() => orientation switch
        {
            NavigationMenuOrientation.Horizontal => "horizontal",
            NavigationMenuOrientation.Vertical => "vertical",
            _ => "horizontal"
        };
    }
}
