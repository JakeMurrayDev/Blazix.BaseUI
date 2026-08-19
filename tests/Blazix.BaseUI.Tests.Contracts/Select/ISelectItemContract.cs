namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectItemContract
{
    Task ShouldSelectItemAndClosePopupWhenClicked();
    Task ShouldNotSelectDisabledItem();
    Task ShouldApplyDataSelectedWhenSelected();
    Task ShouldApplyDataHighlightedWhenHighlighted();
    Task ShouldRenderWithOptionRole();
    Task ShouldSetAriaSelectedTrue();
    Task DisabledItem_HasAriaDisabled();

    // Focus + Disabled
    Task DisabledItem_ShouldNotHighlightOnMouseEnter();

    // Focus on open
    Task ShouldFocusSelectedItemUponOpeningPopup();

    // Disabled item click guard
    Task DisabledItem_ShouldNotSelectOnClickAndKeepOpen();

    // Root disabled inheritance
    Task Item_ShouldInheritRootDisabledState();

    // React parity additions
    Task ShouldNotEmitDataLabel();
    Task ShouldEmitDataBlazixBaseUiLabelWhenLabelSet();
    Task ShouldRejectMouseClickOnUnhighlightedItem();
    Task NativeButton_ShouldRenderAsButtonElementWithTypeButton();
    Task NonNativeButton_ShouldRenderAsDivWithRoleOption();
    Task DisabledItem_ShouldRemainFocusableWhenHighlighted();
}
