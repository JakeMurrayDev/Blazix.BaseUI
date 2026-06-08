namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectGroupContract
{
    Task ShouldRenderGroupWithLabel();
    Task ShouldAssociateLabelWithGroup();
}
