namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectPortalContract
{
    Task RendersChildrenWhenMounted();
    Task DoesNotRenderChildrenWhenNotMounted();
    Task RendersChildrenWhenKeepMounted();
    Task RendersChildrenWhenForceMounted();
    Task CascadesPortalContext();
    Task RequiresContext();
    Task ForwardsAdditionalAttributes();
    Task RendersWithCustomRender();
    Task ExposesElementReference();
    Task DefaultContainerIsBody();
}
