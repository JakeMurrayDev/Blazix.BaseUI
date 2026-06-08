namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipProviderContract
{
    Task RendersChildren();
    Task CascadesProviderContext();
    Task DelayParameterIsPassedToContext();
    Task CloseDelayParameterIsPassedToContext();
    Task TimeoutParameterIsPassedToContext();
}
