namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardPortalContract
{
    Task RendersChildrenWhenMounted();
    Task DoesNotRenderChildrenWhenNotMounted();
    Task RendersChildrenWhenKeepMounted();
    Task CascadesPortalContext();
    Task RequiresContext();
}
