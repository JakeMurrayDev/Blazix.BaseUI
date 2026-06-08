namespace Blazix.BaseUI.Tests.Contracts.Field;

public interface IFieldDescriptionContract
{
    Task RendersAsParagraphByDefault();
    Task RendersWithCustomRender();
    Task SetsAriaDescribedByOnControlAutomatically();
}
