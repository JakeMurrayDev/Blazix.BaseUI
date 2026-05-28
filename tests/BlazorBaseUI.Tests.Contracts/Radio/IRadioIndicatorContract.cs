namespace BlazorBaseUI.Tests.Contracts.Radio;

public interface IRadioIndicatorContract
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

    // KeepMounted
    Task KeepsIndicatorMountedWhenUnchecked();
    Task KeepsIndicatorMountedWhenChecked();

    // Style hooks
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUncheckedAndKeepMounted();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();
    Task InitiallyCheckedIndicatorDoesNotUseStartingStyle();
    Task IgnoresStaleExitTransitionWhenCheckedAgain();

    // Context
    Task ReceivesStateFromContext();
    Task ThrowsWithoutRadioRootContext();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

}
