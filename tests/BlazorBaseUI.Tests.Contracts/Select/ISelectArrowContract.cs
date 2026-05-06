namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectArrowContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasAriaHiddenTrue();
    Task EmitsDataSide();
    Task EmitsDataAlign();
    Task EmitsDataOpenWhenOpen();
    Task EmitsDataUncenteredWhenArrowUncentered();
    Task DoesNotEmitDataAnchorHidden();
    Task DoesNotRenderWhenAlignItemWithTriggerActive();
    Task RegistersArrowElementWithPositioner();
}
