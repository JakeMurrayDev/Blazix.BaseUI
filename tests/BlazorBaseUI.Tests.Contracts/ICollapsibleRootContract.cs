namespace BlazorBaseUI.Tests.Contracts;

public interface ICollapsibleRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task CascadesContextToChildren();
    Task ControlledModeRespectsOpenParameter();
    Task UncontrolledModeUsesDefaultOpen();
    Task InvokesOnOpenChange();
    Task InvokesOnOpenChangeWithCorrectReason();
    Task OnOpenChangeCancellationPreventsStateChange();
    Task ReceivesCorrectState();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasDataDisabledOnRootWhenDisabled();
    Task AllowPropagationOnEventArgs();
}
