namespace Blazix.BaseUI.Tests.Contracts.Popover;

public interface IPopoverPopupContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task RendersChildren();
    Task HasRoleDialog();
    Task HasTabIndexMinusOne();
    Task DoesNotHaveAriaModalWhenModal();
    Task DoesNotHaveAriaModalWhenNonModal();
    Task HasDataOpenWhenOpen();
    Task HasAriaLabelledByWhenTitlePresent();
    Task DoesNotHaveAriaLabelledByWithoutTitle();
    Task HasAriaDescribedByWhenDescriptionPresent();
    Task DoesNotHaveAriaDescribedByWithoutDescription();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
}
