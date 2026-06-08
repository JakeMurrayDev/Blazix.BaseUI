namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectListContract
{
    Task RendersAsDivByDefault();
    Task UsesRootIdDashListAsListId();
    Task WritesListIdToRootContext();
    Task OmitsTabIndex();
    Task AppliesRoleListbox();
    Task EmitsAriaMultiselectableWhenMultiple();
    Task OmitsAriaMultiselectableWhenSingleSelect();
    Task AppliesFunctionalStylesWhenAlignItemWithTriggerActive();
    Task OmitsFunctionalStylesWhenInactive();
    Task AppliesDisableScrollbarClassWhenScrollArrowsActiveAndNotTouch();
    Task OmitsDisableScrollbarClassWhenOpenMethodIsTouch();
    Task OmitsDisableScrollbarClassWhenNoScrollArrows();
    Task RegistersListElementOnFirstRender();
    Task InjectsScrollbarDisableStyleOnFirstRender();
    Task ClearsListElementOnDispose();
    Task ForwardsAdditionalAttributes();
    Task MergesUserClassWithDisableScrollbarClass();
    Task MergesUserStyleWithFunctionalStyles();
}
