namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectConformanceContract
{
    Task SelectArrow_RendersAsDiv();
    Task SelectBackdrop_RendersAsDiv();
    Task SelectGroup_RendersAsDiv();
    Task SelectGroupLabel_RendersAsDiv();
    Task SelectIcon_RendersAsSpan();
    Task SelectItem_RendersAsDivWithOptionRole();
    Task SelectItemIndicator_RendersAsSpan();
    Task SelectItemText_RendersAsDiv();
    Task SelectList_RendersAsDiv();
    Task SelectPopup_RendersAsDiv();
    Task SelectPortal_RendersAsDiv();
    Task SelectPositioner_RendersAsDiv();
    Task SelectScrollDownArrow_RendersAsDiv();
    Task SelectScrollUpArrow_RendersAsDiv();
    Task SelectValue_RendersAsSpan();
}
