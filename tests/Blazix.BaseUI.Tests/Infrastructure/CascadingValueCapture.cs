namespace Blazix.BaseUI.Tests.Infrastructure;

/// <summary>
/// Helper component that captures a cascading value and exposes it via callback.
/// Used in tests to verify cascading value propagation.
/// </summary>
internal sealed class CascadingValueCapture<T> : ComponentBase
{
    [CascadingParameter]
    internal T? Value { get; set; }

    [Parameter]
    public EventCallback<T?> OnCaptured { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await OnCaptured.InvokeAsync(Value);
    }
}
