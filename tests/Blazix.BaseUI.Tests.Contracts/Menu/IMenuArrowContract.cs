namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuArrowContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasAriaHidden();
    Task HasDataOpenWhenOpen();
    Task RequiresContext();
}
