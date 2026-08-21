namespace Blazix.BaseUI.Tests.Contracts.Combobox;

public interface IComboboxRootContract
{
    Task InputPress_ShouldReportInputPressOpenReason();

    Task InlineList_ShouldExposeExpandedAriaOnInput();

    Task DisabledRoot_ShouldDisableItems();

    Task DisabledItem_ShouldStillInvokeConsumerClickHandler();

    Task QueryClear_ShouldRestoreHighlightToSelectedItem();

    Task QueryClear_ShouldRestoreHighlightWithControlledInputValue();

    Task Input_ShouldExposeComboboxAttributesFromSelectedValue();

    Task ItemPress_ShouldSelectSingleValueAndSerializeHiddenInput();

    Task MultipleItemPress_ShouldToggleSelectedValuesAndRenderIndicators();

    Task MultipleToggle_ShouldNotClearInputWhenQueryIsEmpty();

    Task Clear_ShouldClearSelectedValueAndInputValue();

    Task Value_ShouldRenderSelectedLabelsAndPlaceholder();

    Task ObjectValues_ShouldUseLabelForInputAndValueForHiddenInput();

    Task ObjectValues_ShouldUseCustomEqualityForSelectedItems();

    Task GroupedFiltering_ShouldStopAfterGlobalLimit();

    Task HiddenInputChange_ShouldBeIgnoredWhenReadOnly();

    Task Label_ShouldKeepTriggerAssociationWhenLabelIsReplaced();
    Task GroupLabel_ShouldKeepGroupAssociationWhenSupersededLabelUnmounts();

    Task Item_ShouldIgnoreAStationaryPointerEnterOnWebKit();
}
