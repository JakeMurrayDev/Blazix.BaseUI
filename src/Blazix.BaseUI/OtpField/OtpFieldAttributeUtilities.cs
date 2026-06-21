namespace Blazix.BaseUI.OtpField;

internal static class OtpFieldAttributeUtilities
{
    public static void AddRootStateAttributes(IDictionary<string, object> attributes, OtpFieldRootState state)
    {
        if (state.Complete)
            attributes["data-complete"] = true;

        if (state.Disabled)
            attributes["data-disabled"] = true;

        if (state.ReadOnly)
            attributes["data-readonly"] = true;

        if (state.Required)
            attributes["data-required"] = true;

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

    public static void AddInputStateAttributes(IDictionary<string, object> attributes, OtpFieldInputState state)
    {
        if (state.Complete)
            attributes["data-complete"] = true;

        if (state.Filled)
            attributes["data-filled"] = true;

        if (state.Disabled)
            attributes["data-disabled"] = true;

        if (state.ReadOnly)
            attributes["data-readonly"] = true;

        if (state.Required)
            attributes["data-required"] = true;

        if (state.Valid == true)
            attributes["data-valid"] = true;
        else if (state.Valid == false)
            attributes["data-invalid"] = true;

        if (state.Touched)
            attributes["data-touched"] = true;

        if (state.Dirty)
            attributes["data-dirty"] = true;

        if (state.Focused)
            attributes["data-focused"] = true;
    }
}
