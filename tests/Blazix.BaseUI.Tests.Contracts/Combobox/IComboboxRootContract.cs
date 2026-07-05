namespace Blazix.BaseUI.Tests.Contracts.Combobox;

public interface IComboboxRootContract
{
    Task Input_ShouldExposeComboboxAttributesFromSelectedValue();

    Task ItemPress_ShouldSelectSingleValueAndSerializeHiddenInput();

    Task MultipleItemPress_ShouldToggleSelectedValuesAndRenderIndicators();

    Task Clear_ShouldClearSelectedValueAndInputValue();

    Task Value_ShouldRenderSelectedLabelsAndPlaceholder();

    Task ObjectValues_ShouldUseLabelForInputAndValueForHiddenInput();

    Task ObjectValues_ShouldUseCustomEqualityForSelectedItems();

    Task GroupedFiltering_ShouldStopAfterGlobalLimit();

    Task HiddenInputChange_ShouldBeIgnoredWhenReadOnly();
}
