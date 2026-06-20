namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record DocsNavLink(string Name, string Slug, string Href, string Summary, bool IsDocumented = false);

public sealed record DocsNavSection(string Title, string Area, IReadOnlyList<DocsNavLink> Links);

public static class DocsNav
{
    public static IReadOnlyList<DocsNavSection> Sections { get; } =
    [
        new("Overview", "overview",
        [
            // Quick start renders at the site root; nav consumers must match it with NavLinkMatch.All.
            new("Quick start", "quick-start", "/", "A quick guide to getting started with Blazix.BaseUI.", IsDocumented: true),
            new("About", "about", "/overview/about", "An overview of the project and its goals."),
            new("Accessibility", "accessibility", "/overview/accessibility", "How Blazix.BaseUI approaches accessibility."),
        ]),
        new("Handbook", "handbook",
        [
            new("Animation", "animation", "/handbook/animation", "A guide to animating Blazix.BaseUI components."),
            new("Composition", "composition", "/handbook/composition", "A guide to composing Blazix.BaseUI components with your own Blazor components."),
            new("Customization", "customization", "/handbook/customization", "A guide to customizing component behavior."),
            new("Forms", "forms", "/handbook/forms", "A guide to using Blazix.BaseUI components in forms."),
            new("Styling", "styling", "/handbook/styling", "A guide to styling Blazix.BaseUI components with any styling solution."),
        ]),
        new("Components", "components",
        [
            new("Accordion", "accordion", "/components/accordion", "A set of collapsible panels with headings.", IsDocumented: true),
            new("Alert Dialog", "alert-dialog", "/components/alert-dialog", "A dialog that requires a response from the user.", IsDocumented: true),
            new("Autocomplete", "autocomplete", "/components/autocomplete", "An input that suggests options as you type.", IsDocumented: true),
            new("Avatar", "avatar", "/components/avatar", "An image with a textual fallback.", IsDocumented: true),
            new("Button", "button", "/components/button", "An accessible button with full styling freedom.", IsDocumented: true),
            new("Checkbox", "checkbox", "/components/checkbox", "A control for toggling between checked states.", IsDocumented: true),
            new("Checkbox Group", "checkbox-group", "/components/checkbox-group", "Manages shared state for a series of checkboxes.", IsDocumented: true),
            new("Collapsible", "collapsible", "/components/collapsible", "A collapsible panel controlled by a button.", IsDocumented: true),
            new("Context Menu", "context-menu", "/components/context-menu", "A menu opened by right-clicking an area.", IsDocumented: true),
            new("Dialog", "dialog", "/components/dialog", "A popup that opens on top of the page.", IsDocumented: true),
            new("Drawer", "drawer", "/components/drawer", "A dialog that slides in from the edge of the screen.", IsDocumented: true),
            new("Field", "field", "/components/field", "Labelling and validation for form controls.", IsDocumented: true),
            new("Fieldset", "fieldset", "/components/fieldset", "A grouped set of fields with a legend.", IsDocumented: true),
            new("Form", "form", "/components/form", "A native form with consolidated error handling.", IsDocumented: true),
            new("Input", "input", "/components/input", "A native input with managed state.", IsDocumented: true),
            new("Menu", "menu", "/components/menu", "A list of actions in a dropdown.", IsDocumented: true),
            new("Menubar", "menubar", "/components/menubar", "A horizontal collection of menus.", IsDocumented: true),
            new("Meter", "meter", "/components/meter", "A graphical display of a numeric value.", IsDocumented: true),
            new("Navigation Menu", "navigation-menu", "/components/navigation-menu", "A collection of links and menus for site navigation.", IsDocumented: true),
            new("Number Field", "number-field", "/components/number-field", "A numeric input with increment and decrement buttons.", IsDocumented: true),
            new("Popover", "popover", "/components/popover", "An accessible popup anchored to a trigger.", IsDocumented: true),
            new("Preview Card", "preview-card", "/components/preview-card", "A popup that appears when hovering a link.", IsDocumented: true),
            new("Progress", "progress", "/components/progress", "Displays the status of a task over time.", IsDocumented: true),
            new("Radio", "radio", "/components/radio", "A control for selecting one option from a set.", IsDocumented: true),
            new("Scroll Area", "scroll-area", "/components/scroll-area", "A scrollable container with custom scrollbars.", IsDocumented: true),
            new("Select", "select", "/components/select", "A form component for choosing a value from a list of options.", IsDocumented: true),
            new("Separator", "separator", "/components/separator", "A visual divider between sections.", IsDocumented: true),
            new("Slider", "slider", "/components/slider", "A control for selecting a value from a range.", IsDocumented: true),
            new("Switch", "switch", "/components/switch", "A control for toggling between on and off.", IsDocumented: true),
            new("Tabs", "tabs", "/components/tabs", "Organizes content into panels with tabbed navigation.", IsDocumented: true),
            new("Toast", "toast", "/components/toast", "Briefly displays notifications.", IsDocumented: true),
            new("Toggle", "toggle", "/components/toggle", "A two-state button that can be on or off.", IsDocumented: true),
            new("Toggle Group", "toggle-group", "/components/toggle-group", "Manages shared state for a series of toggle buttons.", IsDocumented: true),
            new("Toolbar", "toolbar", "/components/toolbar", "A container for grouping buttons and controls.", IsDocumented: true),
            new("Tooltip", "tooltip", "/components/tooltip", "A popup that labels or describes an element on hover or focus.", IsDocumented: true),
        ]),
        new("Utils", "utils",
        [
            new("CSP Provider", "csp-provider", "/utils/csp-provider", "Support for strict Content Security Policies."),
            new("Direction Provider", "direction-provider", "/utils/direction-provider", "Enables right-to-left behavior for components."),
            new("Portal", "portal", "/utils/portal", "Renders content in a different part of the DOM."),
            new("Render Element", "render-element", "/utils/render-element", "The element-rendering primitive behind every component part."),
        ]),
    ];

    public static DocsNavLink? Find(string area, string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var section = Sections.FirstOrDefault(s => string.Equals(s.Area, area, StringComparison.OrdinalIgnoreCase));
        return section?.Links.FirstOrDefault(l => string.Equals(l.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
