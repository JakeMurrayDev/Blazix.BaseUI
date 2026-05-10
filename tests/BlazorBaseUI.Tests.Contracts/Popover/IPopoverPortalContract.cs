namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverPortalContract
{
    Task RendersPortalContainer();
    Task RendersChildrenWhenMounted();
    Task DoesNotRenderChildrenWhenNotMounted();
    Task RendersWithKeepMounted();
}
