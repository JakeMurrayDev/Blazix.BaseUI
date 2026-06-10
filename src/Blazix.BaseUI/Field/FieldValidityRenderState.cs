namespace Blazix.BaseUI.Field;

/// <summary>
/// Represents the state supplied to <see cref="FieldValidity"/>.
/// </summary>
public sealed record FieldValidityRenderState(
    FieldValidityState State,
    string[] Errors,
    string Error,
    object? Value,
    object? InitialValue,
    FieldTransitionStatus? TransitionStatus)
{
    /// <summary>
    /// Gets the validity state, matching the React Base UI field validity render contract.
    /// </summary>
    public FieldValidityState Validity => State;

    internal static FieldValidityRenderState FromValidityData(
        FieldValidityData data,
        FieldTransitionStatus? transitionStatus) => new(
            State: data.State,
            Errors: data.Errors,
            Error: data.Error,
            Value: data.Value,
            InitialValue: data.InitialValue,
            TransitionStatus: transitionStatus);
}
