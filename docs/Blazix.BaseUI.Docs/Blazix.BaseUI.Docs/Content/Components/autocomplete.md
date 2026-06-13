# Autocomplete

Autocomplete combines an input with a filtered suggestion list. It supports list, inline, and combined completion modes.

## Import

```razor
@using Blazix.BaseUI.Autocomplete
```

## Anatomy

```razor
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
```

## Notes

- Use `ItemToStringValue` for non-string items.
- Use `FilterDisabled` when filtering externally.
- Use `AutoHighlight` to control first-item highlight behavior.
