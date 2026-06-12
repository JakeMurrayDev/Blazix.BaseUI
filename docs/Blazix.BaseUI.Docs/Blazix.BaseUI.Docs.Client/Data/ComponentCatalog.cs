namespace Blazix.BaseUI.Docs.Client.Data;

public static class ComponentCatalog
{
    public static IReadOnlyList<ComponentDoc> All { get; } =
    [
        new(
            "accordion",
            "Accordion",
            "AC",
            "Disclosure",
            "A set of collapsible panels with headings.",
            "Use AccordionRoot with typed AccordionItem values. The root can be uncontrolled with DefaultValue or controlled with Value and ValueChanged, and panels expose measured motion hooks for Tailwind transitions.",
            """
            <AccordionRoot TValue="string"
                           DefaultValue="@(["install"])"
                           Multiple
                           ClassValue="@(_ => "grid gap-2")">
                <AccordionItem TValue="string" Value="install">
                    <AccordionHeader>
                        <AccordionTrigger>Install</AccordionTrigger>
                    </AccordionHeader>
                    <AccordionPanel ClassValue="@(_ => "h-[var(--accordion-panel-height)] overflow-hidden transition-[height] data-[ending-style]:h-0 data-[starting-style]:h-0")">
                        dotnet add package Blazix.BaseUI
                    </AccordionPanel>
                </AccordionItem>
            </AccordionRoot>
            """,
            ["AccordionRoot", "AccordionItem", "AccordionHeader", "AccordionTrigger", "AccordionPanel"],
            ["Tab moves through triggers in document order.", "Enter or Space toggles the focused trigger.", "Arrow keys move between triggers according to Orientation.", "Home and End move to the first and last trigger."],
            ["Use Multiple when more than one panel can stay open.", "Use DefaultValue for uncontrolled initial state.", "Use Value and ValueChanged when the app owns state.", "Use HiddenUntilFound when browser find-in-page should reveal closed content."]),
        new(
            "alert-dialog",
            "Alert Dialog",
            "AD",
            "Modal",
            "A modal dialog that requires an explicit user response before work continues.",
            "Use AlertDialogRoot with a trigger, portal, backdrop, popup, title, description, and close actions.",
            """
            <AlertDialogRoot>
                <AlertDialogTrigger>Delete file</AlertDialogTrigger>
                <AlertDialogPortal>
                    <AlertDialogBackdrop />
                    <AlertDialogPopup>
                        <AlertDialogTitle>Delete file?</AlertDialogTitle>
                        <AlertDialogDescription>This action cannot be undone.</AlertDialogDescription>
                        <AlertDialogClose>Cancel</AlertDialogClose>
                    </AlertDialogPopup>
                </AlertDialogPortal>
            </AlertDialogRoot>
            """,
            ["AlertDialogRoot", "AlertDialogTrigger", "AlertDialogPortal", "AlertDialogBackdrop", "AlertDialogPopup", "AlertDialogTitle", "AlertDialogDescription", "AlertDialogClose"],
            ["Tab is trapped inside the modal.", "Escape closes the dialog.", "Focus returns to the trigger after close."],
            ["Backdrop pointer dismissal is disabled.", "Use a clear title and description.", "Provide at least one explicit close action."]),
        new(
            "autocomplete",
            "Autocomplete",
            "AU",
            "Combobox",
            "A text input with filtered suggestions and active item management.",
            "Provide Items to AutocompleteRoot and render the collection inside AutocompleteList.",
            """
            <AutocompleteRoot TValue="string" Items="items">
                <AutocompleteInput placeholder="Search" />
                <AutocompleteTrigger>Open</AutocompleteTrigger>
                <AutocompletePositioner>
                    <AutocompletePopup>
                        <AutocompleteList>
                            <AutocompleteCollection TValue="string" Context="entry">
                                <AutocompleteItem TValue="string" Value="@entry.Item" Index="@entry.Index">
                                    @entry.Item
                                </AutocompleteItem>
                            </AutocompleteCollection>
                        </AutocompleteList>
                    </AutocompletePopup>
                </AutocompletePositioner>
            </AutocompleteRoot>
            """,
            ["AutocompleteRoot", "AutocompleteInput", "AutocompleteTrigger", "AutocompletePositioner", "AutocompletePopup", "AutocompleteList", "AutocompleteCollection", "AutocompleteItem"],
            ["Arrow keys move the highlight.", "Enter accepts the highlighted item.", "Escape closes the popup."],
            ["Use ItemToStringValue for non-string items.", "Set FilterDisabled when filtering externally.", "Use AutoHighlight for first-result highlight behavior."]),
        new(
            "avatar",
            "Avatar",
            "AV",
            "Media",
            "A resilient user image primitive with fallback content for loading or broken sources.",
            "Compose AvatarRoot with AvatarImage and AvatarFallback. Style the rendered parts with Tailwind classes.",
            """
            <AvatarRoot>
                <AvatarImage src="/favicon.png" alt="Blazix" />
                <AvatarFallback>BX</AvatarFallback>
            </AvatarRoot>
            """,
            ["AvatarRoot", "AvatarImage", "AvatarFallback"],
            ["Avatar has no keyboard interaction by default."],
            ["Fallback renders when the image fails.", "Use Delay on AvatarFallback to avoid flicker.", "Keep alt text meaningful when an image is used."]),
        new(
            "button",
            "Button",
            "BT",
            "Action",
            "A button primitive with native and custom element behavior for accessible activation.",
            "Use Button for ordinary actions. Switch NativeButton off only when rendering a non-button element intentionally.",
            """
            <Button Disabled="isSaving" FocusableWhenDisabled="true">
                Save changes
            </Button>
            """,
            ["Button"],
            ["Enter and Space activate the button.", "Disabled native buttons leave the tab order.", "FocusableWhenDisabled keeps focus while blocking activation."],
            ["Prefer native button rendering.", "Use FocusableWhenDisabled for loading states.", "ClassValue receives ButtonState for disabled styling."]),
        new(
            "checkbox",
            "Checkbox",
            "CB",
            "Input",
            "A binary or mixed-state input with hidden form integration.",
            "Use CheckboxRoot with CheckboxIndicator. The indicator reflects checked or indeterminate state.",
            """
            <CheckboxRoot DefaultChecked="true">
                <CheckboxIndicator>✓</CheckboxIndicator>
            </CheckboxRoot>
            """,
            ["CheckboxRoot", "CheckboxIndicator"],
            ["Space toggles the checkbox.", "Tab moves focus to the checkbox.", "ReadOnly prevents state changes."],
            ["Use Checked and CheckedChanged for controlled state.", "Use Indeterminate for mixed state.", "Name and Value participate in form submission."]),
        new(
            "checkbox-group",
            "Checkbox Group",
            "CG",
            "Input",
            "A grouped checkbox primitive that manages selected values and parent selection.",
            "Wrap related CheckboxRoot children in CheckboxGroup and give each child a Value.",
            """
            <CheckboxGroup DefaultValue="@(["docs"])">
                <CheckboxRoot Value="docs"><CheckboxIndicator>✓</CheckboxIndicator></CheckboxRoot>
                <CheckboxRoot Value="tests"><CheckboxIndicator>✓</CheckboxIndicator></CheckboxRoot>
            </CheckboxGroup>
            """,
            ["CheckboxGroup", "CheckboxRoot", "CheckboxIndicator"],
            ["Each checkbox follows normal checkbox keyboard behavior.", "A parent checkbox can toggle all child values."],
            ["Use AllValues when rendering a parent checkbox.", "Use ValueChanged to observe selected values.", "Group Disabled cascades to children."]),
        new(
            "collapsible",
            "Collapsible",
            "CO",
            "Disclosure",
            "A single trigger and panel pair for showing or hiding supporting content.",
            "Use CollapsibleRoot with CollapsibleTrigger and CollapsiblePanel. KeepMounted preserves DOM content while closed.",
            """
            <CollapsibleRoot DefaultOpen="true">
                <CollapsibleTrigger>Details</CollapsibleTrigger>
                <CollapsiblePanel>More information</CollapsiblePanel>
            </CollapsibleRoot>
            """,
            ["CollapsibleRoot", "CollapsibleTrigger", "CollapsiblePanel"],
            ["Enter or Space toggles the trigger.", "Tab reaches the trigger in normal page order."],
            ["Use Open and OpenChanged for controlled state.", "Use DefaultOpen for initial uncontrolled state.", "Use KeepMounted when hidden content must stay in the DOM."])
    ];

    public static ComponentDoc? Find(string? slug)
    {
        return All.FirstOrDefault(component => string.Equals(component.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
