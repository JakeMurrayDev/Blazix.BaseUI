namespace Blazix.BaseUI.Tests.Contracts.PreviewCard;

public interface IPreviewCardPositionerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasHiddenWhenNotMounted();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task CascadesPositionerContext();
    Task RequiresContext();
}
