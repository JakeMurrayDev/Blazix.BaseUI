using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Field;

/// <summary>
/// Used to display a custom message based on the field's validity.
/// Requires <see cref="ChildContent"/> to be a function that accepts field validity state as an argument.
/// </summary>
public sealed class FieldValidity : ComponentBase, IFieldStateSubscriber, IDisposable
{
    private bool wasInvalid;
    private FieldTransitionStatus? transitionStatus;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    /// <summary>
    /// Gets or sets a render fragment that accepts the field validity state as a context parameter.
    /// </summary>
    [Parameter]
    public RenderFragment<FieldValidityRenderState>? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        FieldContext?.Subscribe(this);
        wasInvalid = GetCombinedValidityData().State.Valid == false;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        UpdateTransitionStatus();
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (transitionStatus == FieldTransitionStatus.Starting)
        {
            transitionStatus = null;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        FieldContext?.Unsubscribe(this);
    }

    /// <inheritdoc />
    void IFieldStateSubscriber.NotifyStateChanged()
    {
        UpdateTransitionStatus();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var validityData = GetCombinedValidityData();
        var renderState = FieldValidityRenderState.FromValidityData(validityData, transitionStatus);
        builder.AddContent(0, ChildContent?.Invoke(renderState));
    }

    private FieldValidityData GetCombinedValidityData()
    {
        var validityData = FieldContext?.ValidityData ?? FieldValidityData.Default;
        return FieldAttributeUtilities.GetCombinedValidityData(validityData, FieldContext?.Invalid == true);
    }

    private void UpdateTransitionStatus()
    {
        var isInvalid = GetCombinedValidityData().State.Valid == false;
        if (isInvalid && !wasInvalid)
            transitionStatus = FieldTransitionStatus.Starting;
        else if (!isInvalid)
            transitionStatus = null;

        wasInvalid = isInvalid;
    }
}
