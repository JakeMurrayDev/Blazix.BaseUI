namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuRadioGroupContract
{
    Task HasRoleGroup();
    Task RendersWithCustomRender();
    Task CascadesContextToRadioItems();
    Task ControlledModeRespectsValueParameter();
    Task UncontrolledModeUsesDefaultValue();
    Task InvokesOnValueChange();
    Task SupportsCancelInOnValueChange();
    Task GroupLabelWiresAriaLabelledby();
}
