namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task RendersChildrenInCurrentContainer();
    Task HasDataCurrentAttribute();
    Task SetsInstantTypeTriggerChangeOnTransitionEnd();
    Task HasDataInstantWhenSet();
    Task HasActivationDirectionAfterTransitionStart();
    Task HasDataCurrentOnInnerWrapper();
}
