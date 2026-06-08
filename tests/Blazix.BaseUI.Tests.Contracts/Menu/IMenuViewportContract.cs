namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuViewportContract
{
    Task RendersDiv();
    Task HasDataTransitioning();
    Task HasDataCurrent();
    Task HasDataInstantWhenSet();
    Task SetsHasViewportOnContext();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersChildContent();
    Task SetsInstantTypeTriggerChangeOnTransitionEnd();
    Task HasActivationDirectionAfterTransitionStart();
}
