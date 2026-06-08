namespace Blazix.BaseUI.Tests.Contracts.MenuBar;

public interface IMenuBarRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task HasRoleMenubar();
    Task HasAriaOrientationHorizontalByDefault();
    Task HasAriaOrientationVerticalWhenSet();
    Task HasDataOrientationAttribute();
    Task DoesNotSetDataDisabledOnRootWhenDisabled();
    Task ForwardsAdditionalAttributes();
    Task ForwardsProvidedId();
    Task HasGeneratedId();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CascadesContextToChildren();
    Task HasDataModalWhenModal();
    Task DoesNotSetDataModalWhenModalFalse();
    Task TracksHasSubmenuOpenState();
}
