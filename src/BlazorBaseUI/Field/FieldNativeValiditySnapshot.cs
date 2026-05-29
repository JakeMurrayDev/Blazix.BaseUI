namespace BlazorBaseUI.Field;

public sealed record FieldNativeValiditySnapshot(
    FieldValidityState State,
    string ValidationMessage)
{
    public static FieldNativeValiditySnapshot Default { get; } = new(
        State: FieldValidityState.Default,
        ValidationMessage: string.Empty);
}
