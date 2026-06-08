using Microsoft.JSInterop;

namespace Blazix.BaseUI.Tests.Field;

internal sealed class TrackingJsObjectReference : IJSObjectReference
{
    public bool Disposed { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
