namespace Blazix.BaseUI.Tests.Contracts.Drawer;

public interface IDrawerVirtualKeyboardProviderContract
{
    Task VirtualKeyboardProviderInitializesAndDisposesInterop();
    Task VirtualKeyboardProviderMarksViewportInitialization();
}
