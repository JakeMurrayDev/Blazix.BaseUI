namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuRootContract
{
    Task RendersNavByDefault();
    Task RendersDivWhenNested();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CascadesContext();
    Task UncontrolledDefaultValue();
    Task ControlledValue();
    Task InvokesOnValueChange();
    Task HasAriaOrientation();
}
