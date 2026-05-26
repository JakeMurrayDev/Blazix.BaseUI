namespace BlazorBaseUI.Tests.Contracts.Autocomplete;

public interface IAutocompleteRootContract
{
    Task Input_ShouldExposeComboboxAttributes();

    Task ListMode_ShouldFilterItemsAndExposeEmptyState();

    Task ItemPress_ShouldFillInputClosePopupAndRaiseChangeDetails();

    Task BothMode_ShouldUseTypedQueryForFilteringAndInlineHighlightForDisplay();

    Task Trigger_ShouldExposePopupStateDisabledReadonlyRequiredAndListEmptyAttributes();
}
