namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardPopupContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task RendersChildren();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
