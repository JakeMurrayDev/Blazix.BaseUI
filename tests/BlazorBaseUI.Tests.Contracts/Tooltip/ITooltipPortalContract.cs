namespace BlazorBaseUI.Tests.Contracts.Tooltip;

public interface ITooltipPortalContract
{
    Task RendersChildrenWhenMounted();
    Task DoesNotRenderChildrenWhenNotMounted();
    Task RendersChildrenWhenKeepMounted();
    Task CascadesPortalContext();
    Task RequiresContext();
    Task ForwardsAdditionalAttributes();
    Task RendersWithCustomRender();
    Task ExposesElementReference();
}
