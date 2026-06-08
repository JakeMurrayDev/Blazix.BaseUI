namespace Blazix.BaseUI.Tests.Contracts.Accordion;

public interface IAccordionHeaderContract
{
    Task RendersAsH3ByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasDataDisabledWhenParentDisabled();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataIndexAttribute();
    Task HasDataOrientationAttribute();
}
