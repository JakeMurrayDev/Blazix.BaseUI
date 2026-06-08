namespace Blazix.BaseUI.Tests.Contracts.Accordion;

public interface IAccordionRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task RendersCorrectAriaAttributes();
    Task ReferencesManualPanelIdInTriggerAriaControls();
    Task UncontrolledOpenState();
    Task UncontrolledDefaultValueWithCustomItemValue();
    Task ControlledOpenState();
    Task ControlledValueWithCustomItemValue();
    Task CanDisableWholeAccordion();
    Task CanDisableOneAccordionItem();
    Task MultipleItemsCanBeOpenWhenMultipleTrue();
    Task OnlyOneItemOpenWhenMultipleFalse();
    Task HasDataOrientationAttribute();
    Task OnValueChangeWithDefaultItemValue();
    Task OnValueChangeWithCustomItemValue();
    Task OnValueChangeWhenMultipleFalse();
    Task AsyncOnValueChangeCancellationPreventsStateChange();
    Task CascadesContextToChildren();
}
