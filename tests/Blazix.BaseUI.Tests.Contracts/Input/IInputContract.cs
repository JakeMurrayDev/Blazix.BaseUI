namespace Blazix.BaseUI.Tests.Contracts.Input;

public interface IInputContract
{
    Task RendersAsInputByDefault();
    Task ForwardsAdditionalAttributes();
    Task ForwardsValueToFieldControl();
    Task ForwardsDisabledToFieldControl();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersWithCustomRender();
}
