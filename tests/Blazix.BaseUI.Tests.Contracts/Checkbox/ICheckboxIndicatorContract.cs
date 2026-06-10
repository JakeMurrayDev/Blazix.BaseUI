namespace Blazix.BaseUI.Tests.Contracts.Checkbox;

public interface ICheckboxIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Visibility
    Task DoesNotRenderByDefault();
    Task RendersWhenChecked();
    Task RendersWhenIndeterminate();

    // keepMounted
    Task KeepsIndicatorMountedWhenUnchecked();
    Task KeepsIndicatorMountedWhenChecked();
    Task KeepsIndicatorMountedWhenIndeterminate();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUncheckedAndKeepMounted();
    Task HasDataIndeterminateWhenIndeterminate();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Context
    Task ReceivesStateFromContext();
    Task ThrowsWhenRenderedOutsideCheckboxRoot();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();
}
