namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuItemContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA & Attributes
    Task HasRoleMenuitem();
    Task HasTabindexMinusOneByDefault();
    Task HasDataDisabledWhenDisabled();
    Task HasAriaDisabledWhenDisabled();
    Task HasDataHighlightedOnMouseEnter();
    Task SetsLabelAsDataAttribute();

    // Interaction
    Task InvokesOnClickHandler();
    Task ClosesMenuOnClickByDefault();
    Task DoesNotCloseWhenCloseOnClickFalse();
    Task DoesNotActivateWhenDisabled();
}
