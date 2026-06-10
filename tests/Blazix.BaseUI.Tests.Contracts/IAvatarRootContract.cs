namespace Blazix.BaseUI.Tests.Contracts;

public interface IAvatarRootContract
{
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task CascadesContextToChildren();
}
