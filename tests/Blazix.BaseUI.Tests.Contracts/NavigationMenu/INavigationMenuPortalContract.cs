namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuPortalContract
{
    Task RendersWhenMounted();
    Task DoesNotRenderWhenNotMounted();
    Task RendersWhenKeepMounted();
    Task CascadesPortalContext();
    Task RequiresRootContext();
}
