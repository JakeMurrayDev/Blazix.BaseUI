namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuCheckboxItemContract
{
    // Rendering
    Task HasRoleMenuitemcheckbox();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasDefaultId();
    Task UsesProvidedId();

    // ARIA
    Task HasAriaCheckedFalseWhenUnchecked();
    Task HasAriaCheckedTrueWhenChecked();
    Task HasAriaDisabledWhenDisabled();

    // Data attributes
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataDisabledWhenDisabled();
    Task RendersLabelAsDataAttribute();

    // State
    Task ControlledModeRespectsCheckedParameter();
    Task UncontrolledModeUsesDefaultChecked();
    Task TogglesOnClick();
    Task DisabledItemDoesNotToggle();

    // Events
    Task InvokesOnCheckedChange();
    Task SupportsCancelInOnCheckedChange();
    Task OnCheckedChangeIncludesReason();
}
