namespace Blazix.BaseUI.Tests.Contracts.Field;

public interface IFieldControlContract
{
    Task RendersAsInputByDefault();
    Task RendersWithCustomRender();
}
