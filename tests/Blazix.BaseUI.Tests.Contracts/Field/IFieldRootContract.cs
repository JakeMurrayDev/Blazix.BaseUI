namespace Blazix.BaseUI.Tests.Contracts.Field;

public interface IFieldRootContract
{
    Task RendersAsDivByDefault();
    Task AddsDataDisabledToAllComponents();
    Task DoesNotRunValidationByDefaultOutsideForm();
    Task ValidationDebounceTimeDebounces();
    Task DefaultValueNotResetWhenProgrammaticallyChanged();
    Task DefaultValueNotResetToNonEmptyOnFocus();
    Task DirtyStateControlsDirtyState();
    Task TouchedStateControlsTouchedState();
    Task RendersWithCustomRender();
}
