namespace Blazix.BaseUI.Tests.Contracts.Popover;

public interface IPopoverPortalContract
{
    Task RendersPortalContainer();
    Task RendersChildrenWhenMounted();
    Task DoesNotRenderChildrenWhenNotMounted();
    Task RendersWithKeepMounted();
}
