namespace Blazix.BaseUI.Tests.Contracts.Fieldset;

public interface IFieldsetLegendContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task SetsAriaLabelledByOnFieldsetAutomatically();
    Task SetsAriaLabelledByWithCustomId();
}
