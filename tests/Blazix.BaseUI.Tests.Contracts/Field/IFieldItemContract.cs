namespace Blazix.BaseUI.Tests.Contracts.Field;

public interface IFieldItemContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
}
