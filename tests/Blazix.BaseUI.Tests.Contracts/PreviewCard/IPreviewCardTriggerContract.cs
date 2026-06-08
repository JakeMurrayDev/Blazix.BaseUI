namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardTriggerContract
{
    Task RendersAsAnchorByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasDataPopupOpenWhenOpen();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
