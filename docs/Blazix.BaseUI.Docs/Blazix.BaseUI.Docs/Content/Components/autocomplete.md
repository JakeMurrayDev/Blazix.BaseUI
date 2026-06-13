# Autocomplete

An input that suggests options as you type.

Rendered docs: `/components/autocomplete`

The autocomplete filters a list of items against the text in its input and shows the matches in a popup. The generic parts (`Root`, `Item`, `Collection`) take a `TValue` type parameter.

## Anatomy

```razor
@using Blazix.BaseUI.Autocomplete

<AutocompleteRoot TValue="string">
    <AutocompleteInputGroup>
        <AutocompleteInput />
        <AutocompleteTrigger />
        <AutocompleteIcon />
        <AutocompleteClear />
        <AutocompleteValue />
    </AutocompleteInputGroup>

    <AutocompletePortal>
        <AutocompleteBackdrop />
        <AutocompletePositioner>
            <AutocompletePopup>
                <AutocompleteArrow />

                <AutocompleteStatus />
                <AutocompleteEmpty />

                <AutocompleteList>
                    <AutocompleteRow>
                        <AutocompleteItem TValue="string" />
                    </AutocompleteRow>

                    <AutocompleteSeparator />

                    <AutocompleteGroup>
                        <AutocompleteGroupLabel />
                    </AutocompleteGroup>

                    <AutocompleteCollection TValue="string" />
                </AutocompleteList>
            </AutocompletePopup>
        </AutocompletePositioner>
    </AutocompletePortal>
</AutocompleteRoot>
```

A minimal autocomplete only needs the root, an input, and a popup containing the filtered items:

```razor
<AutocompleteRoot TValue="string" Items="@tags">
    <label>
        Search tags
        <AutocompleteInput placeholder="e.g. feature" />
    </label>
    <AutocompletePortal>
        <AutocompletePositioner SideOffset="6">
            <AutocompletePopup>
                <AutocompleteEmpty>No tags found.</AutocompleteEmpty>
                <AutocompleteList>
                    <AutocompleteCollection TValue="string" Context="entry">
                        <AutocompleteItem TValue="string" Value="@entry.Item" Index="@entry.Index">
                            @entry.Item
                        </AutocompleteItem>
                    </AutocompleteCollection>
                </AutocompleteList>
            </AutocompletePopup>
        </AutocompletePositioner>
    </AutocompletePortal>
</AutocompleteRoot>

@code {
    private static readonly IReadOnlyList<string> tags = ["feature", "fix", "bug", "docs"];
}
```

## Examples

### Add a trigger and clear button

Wrap the input in an `AutocompleteInputGroup` to place adornments alongside it. `AutocompleteClear` appears once there is a value to clear, and `AutocompleteTrigger` opens the popup.

```razor
<AutocompleteInputGroup>
    <AutocompleteInput placeholder="e.g. feature" />
    <AutocompleteClear aria-label="Clear">×</AutocompleteClear>
    <AutocompleteTrigger aria-label="Open">▾</AutocompleteTrigger>
</AutocompleteInputGroup>
```

## API reference

### Root

Groups all parts of the autocomplete and manages its state. Generic over the item type (`TValue`). Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `string?` | `null` | The current input value. Use when controlled. |
| `DefaultValue` | `string?` | `null` | The uncontrolled initial input value. |
| `ValueChanged` | `EventCallback<string>` | — | Callback for two-way binding of the input value. |
| `OnValueChange` | `EventCallback<AutocompleteValueChangeEventArgs>` | — | Invoked when the input value changes, with the reason. |
| `Open` | `bool?` | `null` | Whether the popup is open. Use when controlled. |
| `DefaultOpen` | `bool` | `false` | Whether the popup is initially open. |
| `OpenChanged` | `EventCallback<bool>` | — | Callback for two-way binding of the open state. |
| `OnOpenChange` | `EventCallback<AutocompleteOpenChangeEventArgs>` | — | Invoked when the popup opens or closes, with the reason. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Invoked after open or close animations complete. |
| `OnItemHighlighted` | `EventCallback<AutocompleteHighlightEventArgs<TValue>>` | — | Invoked when the highlighted item changes. |
| `Mode` | `AutocompleteMode` | `List` | List filtering and inline completion behavior (`List`, `Both`, `Inline`, `None`). |
| `AutoHighlight` | `AutocompleteAutoHighlight` | `False` | Automatic highlighting of the first item (`False`, `True`, `Always`). |
| `KeepHighlight` | `bool` | `false` | Retains the highlight when the pointer leaves the list. |
| `HighlightItemOnHover` | `bool` | `true` | Whether pointer movement highlights items. |
| `LoopFocus` | `bool` | `true` | Whether keyboard navigation loops through the list. |
| `OpenOnInputClick` | `bool` | `false` | Whether clicking the input opens the popup. |
| `Disabled` | `bool` | `false` | Ignore user interaction. |
| `ReadOnly` | `bool` | `false` | Ignore user edits. |
| `Required` | `bool` | `false` | A value is required for form submission. |
| `Name` | `string?` | `null` | Identifies the field when a form is submitted. |
| `Form` | `string?` | `null` | The id of the form the hidden input belongs to. |
| `AutoComplete` | `string?` | `null` | The browser autocomplete hint for the hidden input. |
| `Id` | `string?` | `null` | The id of the component root. |
| `Grid` | `bool` | `false` | Presents list items as a grid. |
| `Virtualized` | `bool` | `false` | Indicates the list items are externally virtualized. |
| `Inline` | `bool` | `false` | Renders the list inline instead of in a popup. |
| `Modal` | `bool` | `false` | Whether the popup is modal. |
| `Items` | `IReadOnlyList<TValue>?` | `null` | The items to display and filter. |
| `ItemGroups` | `IReadOnlyList<AutocompleteOptionGroup<TValue>>?` | `null` | Grouped items to display and filter. |
| `FilteredItems` | `IReadOnlyList<TValue>?` | `null` | Externally filtered items, bypassing the built-in filter. |
| `Limit` | `int` | `-1` | The maximum number of items to display. `-1` shows all. |
| `Filter` | `Func<TValue, string, Func<TValue, string?>?, bool>?` | `null` | A custom function that matches items against the query. |
| `FilterDisabled` | `bool` | `false` | Disables the built-in filtering. |
| `ItemToStringValue` | `Func<TValue?, string?>?` | `null` | Converts an item value to its display and form text. |
| `IsItemEqualToValue` | `Func<TValue, TValue, bool>?` | `null` | Custom item equality logic. |
| `SubmitOnItemClick` | `bool` | `false` | Whether pressing an item submits the owning form. |
| `ActionsRef` | `AutocompleteRootActions?` | `null` | A reference to imperative root actions. |
| `InputRef` | `Action<ElementReference?>?` | `null` | Receives the hidden form input reference. |
| `ChildContent` | `RenderFragment?` | `null` | The parts of the autocomplete. |

### InputGroup

A wrapper for the input and its controls. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteInputGroupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteInputGroupState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteInputGroupState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-popup-side`, `data-list-empty`, `data-disabled`, `data-readonly`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`.

### Input

A text input used to search for items. Renders an `<input>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Ignore user interaction. |
| `Id` | `string?` | `null` | The id of the input element. |
| `Render` | `RenderFragment<RenderProps<AutocompleteInputState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteInputState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteInputState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-popup-side`, `data-list-empty`, `data-disabled`, `data-readonly`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`.

### Trigger

A button that opens the popup. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. |
| `Id` | `string?` | `null` | The id of the trigger element. |
| `Render` | `RenderFragment<RenderProps<AutocompleteTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteTriggerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteTriggerState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-pressed`, `data-popup-side`, `data-list-empty`, `data-required`, `data-readonly`, `data-disabled`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`.

### Icon

An icon that indicates the popup can be opened. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteIconState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteIconState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteIconState, string?>?` | `null` | Returns a CSS style based on state. |

### Clear

A button that clears the value. Renders a `<button>` element by default (a `<div>` when `NativeButton` is false). Renders only while a value is present unless `KeepMounted` is set.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Ignore user interaction. |
| `KeepMounted` | `bool` | `false` | Keeps the button mounted even when no value is present. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. |
| `Render` | `RenderFragment<RenderProps<AutocompleteClearState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteClearState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteClearState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-visible`.

### Value

Renders the current input value as text without adding an element of its own.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment<string>?` | `null` | Content rendered with the current input value supplied as context. |

### Portal

A portal boundary that renders the popup content into a different part of the DOM. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment?` | `null` | The popup content to portal. |

### Backdrop

A backdrop displayed behind the popup. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteBackdropState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteBackdropState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteBackdropState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`.

### Positioner

Positions the popup relative to the input or trigger. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Side` | `Side` | `Bottom` | Which side of the anchor to place the popup against. |
| `Align` | `Align` | `Center` | How to align the popup relative to the anchor. |
| `SideOffset` | `int` | `0` | The distance between the anchor and the popup, in pixels. |
| `AlignOffset` | `int` | `0` | The offset along the alignment axis, in pixels. |
| `CollisionPadding` | `int` | `5` | The padding between the popup and the collision boundary. |
| `CollisionBoundary` | `CollisionBoundary` | `ClippingAncestors` | The boundary the popup is kept within. |
| `ArrowPadding` | `int` | `5` | The padding between the arrow and the popup edges. |
| `Sticky` | `bool` | `false` | Keeps the popup in view while the anchor scrolls. |
| `DisableAnchorTracking` | `bool` | `false` | Disables tracking of the anchor position. |
| `PositionMethod` | `PositionMethod` | `Absolute` | The CSS position method used for the popup. |
| `CollisionAvoidance` | `CollisionAvoidance?` | `null` | Customizes the collision-avoidance behavior. |
| `Anchor` | `ElementReference?` | `null` | An explicit anchor element to position against. |
| `Render` | `RenderFragment<RenderProps<AutocompletePositionerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompletePositionerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompletePositionerState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-side`, `data-align`, `data-open`, `data-closed`.

CSS variables: `--anchor-width`, `--anchor-height`, `--available-width`, `--available-height`, `--transform-origin`.

### Popup

A container for the list and related content. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompletePopupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompletePopupState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompletePopupState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-side`, `data-align`, `data-open`, `data-closed`, `data-empty`, `data-starting-style`, `data-ending-style`.

### Arrow

An arrow that points toward the anchor. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteArrowState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteArrowState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteArrowState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-side`, `data-align`, `data-open`, `data-closed`, `data-uncentered`.

### Status

Displays a status message announced politely to screen readers. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteStatusState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteStatusState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteStatusState, string?>?` | `null` | Returns a CSS style based on state. |

### Empty

Renders and announces content shown when the filtered list is empty. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteEmptyState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteEmptyState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteEmptyState, string?>?` | `null` | Returns a CSS style based on state. |

### List

A scrollable container for the items. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteListState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteListState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteListState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-empty`.

### Row

Groups items into a row when the list is rendered as a grid. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteRowState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteRowState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteRowState, string?>?` | `null` | Returns a CSS style based on state. |

### Item

An individual selectable item. Generic over the item type (`TValue`). Renders a `<div>` element by default (a `<button>` when `NativeButton` is true).

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue?` | `null` | The value of this item. |
| `Disabled` | `bool` | `false` | Whether this item is disabled. |
| `Label` | `string?` | `null` | Text used for filtering and typeahead matching. |
| `Index` | `int?` | `null` | An explicit index for this item. |
| `NativeButton` | `bool` | `false` | Whether the component renders a native `<button>` element. |
| `Render` | `RenderFragment<RenderProps<AutocompleteItemState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteItemState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteItemState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-highlighted`, `data-disabled`.

### Separator

A visual divider between items or groups. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Orientation` | `Orientation` | `Horizontal` | The orientation of the separator. |
| `Render` | `RenderFragment<RenderProps<AutocompleteSeparatorState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteSeparatorState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteSeparatorState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-orientation`.

### Group

Groups related items with a corresponding label. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AutocompleteGroupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteGroupState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteGroupState, string?>?` | `null` | Returns a CSS style based on state. |

### GroupLabel

An accessible label for a group. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | `null` | The id of the label, used for `aria-labelledby`. Generated automatically when omitted. |
| `Render` | `RenderFragment<RenderProps<AutocompleteGroupLabelState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AutocompleteGroupLabelState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AutocompleteGroupLabelState, string?>?` | `null` | Returns a CSS style based on state. |

### Collection

Renders the root's filtered items. Generic over the item type (`TValue`). Does not render its own element; the child content receives each item and its index.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment<AutocompleteCollectionItem<TValue>>` | required | Renders a single filtered item. The context exposes `Item` and `Index`. |
