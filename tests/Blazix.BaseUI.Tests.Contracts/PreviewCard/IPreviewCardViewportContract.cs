namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task RendersChildrenInCurrentContainer();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
