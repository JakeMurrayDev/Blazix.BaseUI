namespace Blazix.BaseUI.Field;

internal static class FieldAttributeUtilities
{
    public static void AddFieldStateAttributes(IDictionary<string, object> attributes, FieldRootState state)
    {
        if (state.Disabled)
            attributes["data-disabled"] = true;

        if (state.Valid == true)
            attributes["data-valid"] = true;
        else if (state.Valid == false)
            attributes["data-invalid"] = true;

        if (state.Touched)
            attributes["data-touched"] = true;

        if (state.Dirty)
            attributes["data-dirty"] = true;

        if (state.Filled)
            attributes["data-filled"] = true;

        if (state.Focused)
            attributes["data-focused"] = true;
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
