namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectRootContract
{
    Task DefaultValue_ShouldSelectItemByDefault();
    Task Value_ShouldSelectSpecifiedItem();
    Task Value_ShouldUpdateWhenValuePropChanges();
    Task Value_ShouldNotUpdateInternalIfControlledValueDoesNotChange();
    Task ItemToStringValue_UsesForFormSubmission();
    Task ItemToStringLabel_UsesForTriggerText();
    Task OnValueChange_ShouldCallWhenItemSelected();
    Task DefaultOpen_ShouldOpenSelectByDefault();
    Task DefaultOpen_ShouldSelectItemAndCloseWhenClicked();
    Task OnOpenChange_ShouldCallWhenOpenedOrClosed();
    Task OnOpenChange_CancelPreventsOpening();
    Task Modal_ShouldRenderBackdropWhenTrue();
    Task Modal_ShouldNotRenderBackdropWhenFalse();

    // Value prop
    Task Value_UpdatesSelectValueLabelBeforePopupOpens();

    // Form
    Task ItemToStringValue_MultipleSelectionFormSubmission();

    // ItemToStringLabel
    Task ItemToStringLabel_UpdatesTriggerTextAfterSelectingItem();

    // Event guard
    Task OnValueChange_IsNotCalledTwiceOnSelect();

    // Disabled
    Task Disabled_SetsDisabledState();
    Task Disabled_UpdatesWhenDisabledPropChanges();

    // ReadOnly
    Task ReadOnly_SetsReadOnlyState();
    Task ReadOnly_ShouldNotOpenWhenClicked();
    Task ReadOnly_ShouldNotOpenWithKeyboard();

    // Id prop
    Task Id_SetsIdOnTrigger();

    // Null reset
    Task Value_ResetsSelectedIndexWhenSetToNull();

    // Multiple
    Task Multiple_ShouldAllowMultipleSelections();
    Task Multiple_ShouldDeselectItemsWhenClickedAgain();
    Task Multiple_ShouldHandleDefaultValueAsArray();
    Task Multiple_ShouldSerializeMultipleValuesForFormSubmission();
    Task Multiple_ShouldSerializeEmptyArrayAsEmptyString();
    Task Multiple_DoesNotMarkHiddenInputAsRequiredWhenSelectionExists();
    Task Multiple_KeepsHiddenInputRequiredWhenNoSelectionExists();
    Task Multiple_ShouldNotClosePopupWhenSelectingItems();
    Task Multiple_ShouldClosePopupInSingleSelectMode();
    Task Multiple_ShouldUpdateSelectedItemsWhenValuePropChanges();

    // Highlight on hover
    Task HighlightItemOnHover_HighlightsItemOnMouseMove();
    Task HighlightItemOnHover_DoesNotHighlightWhenDisabled();
    Task HighlightItemOnHover_DoesNotRemoveHighlightOnMouseLeaveWhenDisabled();
    Task HighlightItemOnHover_FalseDoesNotHighlightOnMouseMove();
    Task HighlightItemOnHover_FalseDoesNotRemoveHighlightOnMouseLeave();

    // OnOpenChangeComplete
    Task OnOpenChangeComplete_FiresWhenTransitionEnds();

    // Id prop (root level)
    Task Id_RootIdFlowsToTrigger();
    Task Id_RootIdDefaultsToGenerated();

    // IsItemEqualToValue
    Task IsItemEqualToValue_UsesCustomComparer();
    Task IsItemEqualToValue_DefaultsToDefaultComparer();

    // FieldRoot integration
    Task FieldRoot_ResolvedNameFromFieldContext();
    Task FieldRoot_ResolvedDisabledFromFieldContext();
    Task FieldRoot_SetsFilledOnValueChange();
    Task FieldRoot_SetsDirtyOnValueChange();
    Task FieldRoot_ClearsFormErrorsOnValueChange();
}
