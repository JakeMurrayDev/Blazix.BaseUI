namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectItemIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasAriaHiddenTrue();

    // Default children
    Task RendersDefaultCheckmarkWhenNoChildContent();
    Task RendersCustomChildContentWhenProvided();

    // Visibility
    Task DoesNotRenderWhenNotSelected();
    Task RendersWhenSelected();
    Task NonKeepMountedIndicatorUnmountsImmediatelyWhenSelectionChanges();

    // KeepMounted
    Task KeepsIndicatorMountedWhenNotSelected();
    Task KeepsIndicatorMountedWhenSelected();

    // Data attributes (transition style hooks)
    Task DoesNotHaveStartingStyleByDefault();
    Task DoesNotHaveEndingStyleByDefault();

    // State callback
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();
}
