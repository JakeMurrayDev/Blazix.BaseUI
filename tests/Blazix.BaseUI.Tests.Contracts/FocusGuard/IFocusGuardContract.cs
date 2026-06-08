namespace Blazix.BaseUI.Tests.Contracts.FocusGuard;

public interface IFocusGuardContract
{
    Task RendersAsSpan();
    Task HasTabIndexZero();
    Task HasAriaHiddenTrue();
    Task HasFocusGuardDataAttribute();
    Task HasVisuallyHiddenStyles();
    Task InvokesOnFocusCallback();
    Task HasRoleButtonOnSafari();
    Task DoesNotHaveAriaHiddenOnSafari();
    Task DoesNotHaveRoleWhenNotSafari();
}
