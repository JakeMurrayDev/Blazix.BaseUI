namespace BlazorBaseUI.Tests.Contracts.Form;

public interface IFormContract
{
    Task RendersAsFormByDefault();
    Task RendersWithCustomRender();
    Task SetsNoValidateByDefault();
    Task NoValidateCanBeDisabled();
    Task MarksControlInvalidWhenErrorsProvided();
    Task DoesNotMarkControlInvalidWhenNoErrors();
}
