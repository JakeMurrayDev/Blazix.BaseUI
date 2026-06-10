namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuRadioItemContract
{
    Task HasRoleMenuitemradio();
    Task RendersWithCustomRender();
    Task HasAriaCheckedWhenSelected();
    Task HasDataCheckedWhenSelected();
    Task SelectsOnClick();
    Task InheritsDisabledFromGroup();
    Task RendersLabelAsDataAttribute();
}
