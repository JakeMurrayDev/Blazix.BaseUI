namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectLabelContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task UsesRootIdMinusLabelAsDefaultId();
    Task IgnoresConsumerSuppliedId();
    Task UpdatesTriggerAriaLabelledByWithoutFieldRoot();
    Task RegistersLabelIdInLabelableContextWhenInsideFieldRoot();
    Task EmitsFieldValidityDataAttributes();
    Task FocusesTriggerWhenClicked();
}
