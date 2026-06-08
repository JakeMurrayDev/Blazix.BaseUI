namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuCheckboxItemIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasAriaHidden();

    // Visibility
    Task DoesNotRenderWhenUnchecked();
    Task RendersWhenChecked();

    // keepMounted
    Task KeepsIndicatorMountedWhenUnchecked();
    Task KeepsIndicatorMountedWhenChecked();

    // Data attributes
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUncheckedAndKeepMounted();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotRenderDataHighlighted();

    // Context
    Task ReceivesStateFromCheckboxItemContext();
    Task HandlesNullContext();

    // State
    Task ClassValueReceivesCorrectState();
}
