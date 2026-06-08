namespace Blazix.BaseUI.Tests.Contracts;

public interface ICollapsiblePanelContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task IsNotVisibleWhenClosed();
    Task IsVisibleWhenOpen();
    Task RemainsInDomWhenKeepMounted();
    Task IsRemovedFromDomWhenNotKeepMounted();
    Task HasHiddenUntilFoundAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task ReceivesCorrectState();
    Task RequiresContext();
}
