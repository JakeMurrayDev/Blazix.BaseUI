namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectValueContract
{
    Task ValueContent_AcceptsFunctionWithValueParameter();
    Task ChildContent_OverridesTextWhenProvided();
    Task DisplaysLabelFromItemToStringLabel();
    Task FallsBackToValueToString();
    Task Placeholder_DisplaysWhenNoValueSelected();
    Task Placeholder_DoesNotDisplayWhenValueSelected();
    Task Multiple_DisplaysCommaSeparatedLabels();
    Task Multiple_DisplaysEmptyWhenNoValuesSelected();
    Task Multiple_DisplaysSingleValueWhenOneSelected();

    // Dynamic update
    Task Value_DisplaysCorrectTextForDifferentValues();

    // Placeholder precedence
    Task Placeholder_ChildContentTakesPrecedenceOverPlaceholder();
    Task Placeholder_ValueContentTakesPrecedenceOverPlaceholder();

    // Multiple + callback
    Task Multiple_ValuesContentReceivesArrayOfValues();
    Task Multiple_ChildContentTakesPrecedenceOverItems();
    Task Multiple_DefaultsToEmptyArrayWhenNoValueProvided();

    // Parity with React source
    Task Placeholder_ChildContentBeatsPlaceholderWhenMultiSelectEmpty();
    Task PlaceholderContent_RendersWhenProvidedAndNoValue();
    Task PlaceholderContent_TakesPrecedenceOverTextPlaceholder();
    Task NullItemLabel_SuppressesPlaceholderWhenNoValueSelected();
    Task GetLabel_ResolvesFromItemGroups();
    Task GetLabel_ResolvesFromISelectItemLabelOnValue();
    Task Value_RegistersSpanElementWithRootContext();
}
