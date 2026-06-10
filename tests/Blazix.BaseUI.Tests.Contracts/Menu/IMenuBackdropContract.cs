namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuBackdropContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasDataOpenWhenOpen();
    Task HasPointerEventsNoneWhenHoverOpened();
    Task RequiresContext();
}
