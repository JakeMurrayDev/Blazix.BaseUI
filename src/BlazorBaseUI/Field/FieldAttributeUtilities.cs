namespace BlazorBaseUI.Field;

internal static class FieldAttributeUtilities
{
    public static void AddFieldStateAttributes(IDictionary<string, object> attributes, FieldRootState state)
    {
        if (state.Disabled)
            attributes["data-disabled"] = string.Empty;

        if (state.Valid == true)
            attributes["data-valid"] = string.Empty;
        else if (state.Valid == false)
            attributes["data-invalid"] = string.Empty;

        if (state.Touched)
            attributes["data-touched"] = string.Empty;

        if (state.Dirty)
            attributes["data-dirty"] = string.Empty;

        if (state.Filled)
            attributes["data-filled"] = string.Empty;

        if (state.Focused)
            attributes["data-focused"] = string.Empty;
    }

    public static FieldValidityData GetCombinedValidityData(FieldValidityData validityData, bool invalid)
    {
        return validityData with
        {
            State = validityData.State with
            {
                Valid = invalid ? false : validityData.State.Valid
            }
        };
    }
}
