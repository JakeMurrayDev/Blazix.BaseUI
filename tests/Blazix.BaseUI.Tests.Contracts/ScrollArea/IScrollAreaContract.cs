namespace Blazix.BaseUI.Tests.Contracts.ScrollArea;

public interface IScrollAreaContract
{
    Task RootRendersAsDivByDefault();
    Task RootRendersWithCustomRender();
    Task RootForwardsAdditionalAttributes();
    Task RootAppliesClassValue();
    Task RootAppliesStyleValue();
    Task RootAppliesOverflowStateAttributes();
    Task ViewportRendersPresentationRegion();
    Task ViewportCombinesDisableScrollbarClass();
    Task ViewportUsesFocusableTabIndexWhenScrollable();
    Task ContentRendersPresentationWrapper();
    Task ScrollbarDefaultsToVerticalOrientation();
    Task ScrollbarSupportsHorizontalOrientation();
    Task ScrollbarHonorsKeepMounted();
    Task ScrollbarRendersWhenOverflowAppears();
    Task ScrollbarAppliesAxisScrollingAttribute();
    Task ThumbReceivesOrientationAndMeasuredStyles();
    Task CornerRendersOnlyWhenBothScrollbarsAreVisible();
    Task DescendantsRequireScrollAreaContext();
}
