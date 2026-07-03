using Blazix.BaseUI.Autocomplete;

namespace Blazix.BaseUI.Docs.Client.Components.Demos.Autocomplete;

internal sealed record AutocompleteDemoTag(string Id, string Value, string Group = "Component");

internal sealed record AutocompleteDemoMovie(string Id, string Title, int Year);

internal sealed record AutocompleteDemoDocItem(string Title, string Description);

internal sealed record AutocompleteDemoCommandItem(string Value, string Label, string Kind);

internal sealed record AutocompleteDemoVirtualItem(string Id, string Name);

internal static class AutocompleteDemoData
{
    public static readonly IReadOnlyList<AutocompleteDemoTag> Tags =
    [
        new("t1", "feature", "Type"),
        new("t2", "fix", "Type"),
        new("t3", "bug", "Type"),
        new("t4", "docs", "Type"),
        new("t5", "internal", "Type"),
        new("t6", "mobile", "Type"),
        new("c-accordion", "component: accordion"),
        new("c-alert-dialog", "component: alert dialog"),
        new("c-autocomplete", "component: autocomplete"),
        new("c-avatar", "component: avatar"),
        new("c-checkbox", "component: checkbox"),
        new("c-checkbox-group", "component: checkbox group"),
        new("c-collapsible", "component: collapsible"),
        new("c-combobox", "component: combobox"),
        new("c-context-menu", "component: context menu"),
        new("c-dialog", "component: dialog"),
        new("c-field", "component: field"),
        new("c-fieldset", "component: fieldset"),
        new("c-filterable-menu", "component: filterable menu"),
        new("c-form", "component: form"),
        new("c-input", "component: input"),
        new("c-menu", "component: menu"),
        new("c-menubar", "component: menubar"),
        new("c-meter", "component: meter"),
        new("c-navigation-menu", "component: navigation menu"),
        new("c-number-field", "component: number field"),
        new("c-popover", "component: popover"),
        new("c-preview-card", "component: preview card"),
        new("c-progress", "component: progress"),
        new("c-radio", "component: radio"),
        new("c-scroll-area", "component: scroll area"),
        new("c-select", "component: select"),
        new("c-separator", "component: separator"),
        new("c-slider", "component: slider"),
        new("c-switch", "component: switch"),
        new("c-tabs", "component: tabs"),
        new("c-toast", "component: toast"),
        new("c-toggle", "component: toggle"),
        new("c-toggle-group", "component: toggle group"),
        new("c-toolbar", "component: toolbar"),
        new("c-tooltip", "component: tooltip"),
    ];

    public static readonly IReadOnlyList<AutocompleteDemoTag> LimitTags =
    [
        new("t1", "feature", "Type"),
        new("t2", "fix", "Type"),
        new("t3", "bug", "Type"),
        new("t4", "docs", "Type"),
        new("t5", "internal", "Type"),
        new("t6", "mobile", "Type"),
        new("t7", "frontend", "Type"),
        new("t8", "backend", "Type"),
        new("t9", "performance", "Type"),
        new("t10", "accessibility", "Type"),
        new("t11", "design", "Type"),
        new("t12", "research", "Type"),
        new("t13", "testing", "Type"),
        new("t14", "infrastructure", "Type"),
        new("t15", "documentation", "Type"),
        .. Tags.Where(tag => tag.Group == "Component"),
    ];

    public static readonly IReadOnlyList<AutocompleteOptionGroup<AutocompleteDemoTag>> GroupedTags =
    [
        new(Tags.Where(tag => tag.Group == "Type").ToList(), "Type"),
        new(Tags.Where(tag => tag.Group == "Component").ToList(), "Component"),
    ];

    public static readonly IReadOnlyList<AutocompleteDemoDocItem> FuzzyItems =
    [
        new("Accordion", "A vertically stacked set of interactive headings that each reveal a section of content."),
        new("Alert Dialog", "A modal dialog that interrupts the user with important content and expects a response."),
        new("Autocomplete", "An input that suggests options as users type free-form text."),
        new("Combobox", "An input combined with a list of predefined items to select."),
        new("Context Menu", "A menu that appears at the pointer location after a secondary interaction."),
        new("Popover", "A popup anchored to another element with non-modal content."),
        new("Toolbar", "A container for grouping controls that perform document or application actions."),
    ];

    public static readonly IReadOnlyList<AutocompleteDemoMovie> Movies =
    [
        new("1", "The Shawshank Redemption", 1994),
        new("2", "The Godfather", 1972),
        new("3", "The Dark Knight", 2008),
        new("4", "The Godfather Part II", 1974),
        new("5", "12 Angry Men", 1957),
        new("6", "The Lord of the Rings: The Return of the King", 2003),
        new("7", "Schindler's List", 1993),
        new("8", "Pulp Fiction", 1994),
        new("9", "The Lord of the Rings: The Fellowship of the Ring", 2001),
        new("10", "The Good, the Bad and the Ugly", 1966),
        new("11", "Forrest Gump", 1994),
        new("12", "The Lord of the Rings: The Two Towers", 2002),
        new("13", "Fight Club", 1999),
        new("14", "Inception", 2010),
        new("15", "The Matrix", 1999),
        new("16", "Goodfellas", 1990),
        new("17", "Interstellar", 2014),
        new("18", "Se7en", 1995),
        new("19", "Saving Private Ryan", 1998),
        new("20", "Parasite", 2019),
    ];

    public static readonly IReadOnlyList<AutocompleteDemoCommandItem> CommandItems =
    [
        new("linear", "Linear", "Application"),
        new("figma", "Figma", "Application"),
        new("slack", "Slack", "Application"),
        new("youtube", "YouTube", "Application"),
        new("raycast", "Raycast", "Application"),
        new("notion", "Notion", "Application"),
        new("github", "GitHub", "Application"),
        new("jira", "Jira", "Application"),
        new("calendar", "Google Calendar", "Application"),
        new("clipboard-history", "Clipboard History", "Command"),
        new("create-snippet", "Create Snippet", "Command"),
        new("toggle-dark-mode", "Toggle Dark Mode", "Command"),
        new("new-window", "New Window", "Command"),
        new("search-docs", "Search Documentation", "Command"),
        new("capture-screen", "Capture Screenshot", "Command"),
    ];

    public static readonly IReadOnlyList<AutocompleteOptionGroup<AutocompleteDemoCommandItem>> GroupedCommandItems =
    [
        new(CommandItems.Where(item => item.Kind == "Application").ToList(), "Suggestions"),
        new(CommandItems.Where(item => item.Kind == "Command").ToList(), "Commands"),
    ];

    public static readonly IReadOnlyList<AutocompleteDemoVirtualItem> VirtualizedItems =
        Enumerable.Range(1, 10000)
            .Select(index => new AutocompleteDemoVirtualItem(index.ToString(), $"Item {index:0000}"))
            .ToList();

    public static string GetTagValue(AutocompleteDemoTag? tag) => tag?.Value ?? string.Empty;

    public static string GetMovieValue(AutocompleteDemoMovie? movie) => movie?.Title ?? string.Empty;

    public static string GetDocItemValue(AutocompleteDemoDocItem? item) => item?.Title ?? string.Empty;

    public static string GetCommandValue(AutocompleteDemoCommandItem? item) => item?.Label ?? string.Empty;

    public static string GetVirtualItemValue(AutocompleteDemoVirtualItem? item) => item?.Name ?? string.Empty;

    public static bool Contains(string text, string query) =>
        text.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    public static bool FuzzyMatch(string text, string query)
    {
        var normalized = query.Trim();
        if (normalized.Length == 0)
        {
            return true;
        }

        var index = 0;
        foreach (var character in text)
        {
            if (char.ToUpperInvariant(character) != char.ToUpperInvariant(normalized[index]))
            {
                continue;
            }

            index++;
            if (index == normalized.Length)
            {
                return true;
            }
        }

        return false;
    }
}
