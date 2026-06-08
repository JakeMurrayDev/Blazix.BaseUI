namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardBackdropContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataOpenWhenOpen();
    Task HasPointerEventsNone();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
