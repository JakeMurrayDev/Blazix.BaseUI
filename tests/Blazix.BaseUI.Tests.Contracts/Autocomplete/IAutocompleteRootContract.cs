namespace Blazix.BaseUI.Tests.Contracts.Autocomplete;

public interface IAutocompleteRootContract
{
    Task InputPress_ShouldReportInputPressOpenReason();

    Task InlineList_ShouldExposeExpandedAriaOnInput();

    Task DisabledRoot_ShouldDisableItems();

    Task CancelOpen_ShouldCloseThePopupAndReportCancelOpen();

    Task Input_ShouldExposeComboboxAttributes();

    Task ListMode_ShouldFilterItemsAndExposeEmptyState();

    Task ItemPress_ShouldFillInputClosePopupAndRaiseChangeDetails();

    Task BothMode_ShouldUseTypedQueryForFilteringAndInlineHighlightForDisplay();

    Task Trigger_ShouldExposePopupStateDisabledReadonlyRequiredAndListEmptyAttributes();
}
