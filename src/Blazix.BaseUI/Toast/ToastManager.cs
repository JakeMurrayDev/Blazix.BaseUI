namespace Blazix.BaseUI.Toast;

/// <summary>
/// Provides an external manager for adding, updating, closing, and promise-binding toasts.
/// </summary>
public sealed class ToastManager
{
    private readonly HashSet<Action<ToastManagerEvent>> listeners = [];
    private readonly object listenersLock = new();

    /// <summary>Subscribes to manager events.</summary>
    internal Action Subscribe(Action<ToastManagerEvent> listener)
    {
        lock (listenersLock)
        {
            listeners.Add(listener);
        }

        return () =>
        {
            lock (listenersLock)
            {
                listeners.Remove(listener);
            }
        };
    }

    /// <summary>Adds a toast and returns its identifier.</summary>
    public string Add(ToastManagerAddOptions options)
    {
        var id = string.IsNullOrWhiteSpace(options.Id)
            ? GenerateId()
            : options.Id!;

        options.Id = id;
        Emit(new ToastManagerEvent(ToastManagerAction.Add, options));
        return id;
    }

    /// <summary>Closes one toast or all toasts when <paramref name="id"/> is <see langword="null"/>.</summary>
    public void Close(string? id = null)
    {
        Emit(new ToastManagerEvent(ToastManagerAction.Close, id));
    }

    /// <summary>Updates an existing toast.</summary>
    public void Update(string id, ToastManagerUpdateOptions updates)
    {
        Emit(new ToastManagerEvent(ToastManagerAction.Update, new ToastManagerUpdateRequest(id, updates)));
    }

    /// <summary>Adds a loading toast, then updates it when the task resolves or rejects.</summary>
    public Task<TValue> Promise<TValue>(Task<TValue> task, ToastManagerPromiseOptions<TValue> options)
    {
        var loadingOptions = ToAddOptions(options.Loading.Resolve());
        loadingOptions.Type = "loading";
        var id = Add(loadingOptions);

        return HandlePromiseAsync(task, options, id);
    }

    private void Emit(ToastManagerEvent evt)
    {
        Action<ToastManagerEvent>[] snapshot;
        lock (listenersLock)
        {
            snapshot = listeners.ToArray();
        }

        foreach (var listener in snapshot)
        {
            listener(evt);
        }
    }

    private static ToastManagerAddOptions ToAddOptions(ToastManagerUpdateOptions options) =>
        new()
        {
            Title = options.Title,
            Type = options.Type,
            Description = options.Description,
            Timeout = options.Timeout,
            Priority = options.Priority ?? ToastPriority.Low,
            OnClose = options.OnClose,
            OnCloseCallback = options.OnCloseCallback,
            OnRemove = options.OnRemove,
            OnRemoveCallback = options.OnRemoveCallback,
            ActionProps = options.ActionProps,
            PositionerProps = options.PositionerProps,
            Data = options.Data
        };

    private async Task<TValue> HandlePromiseAsync<TValue>(
        Task<TValue> task,
        ToastManagerPromiseOptions<TValue> options,
        string id)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            var successOptions = options.Success.Resolve(result);
            successOptions.Type ??= "success";
            successOptions.HasType = true;
            Update(id, successOptions);
            return result;
        }
        catch (Exception ex)
        {
            var errorOptions = options.Error.Resolve(ex);
            errorOptions.Type ??= "error";
            errorOptions.HasType = true;
            Update(id, errorOptions);
            throw;
        }
    }

    private static string GenerateId() => $"toast-{Guid.NewGuid().ToString("N")[..8]}";
}

internal enum ToastManagerAction
{
    Add,
    Close,
    Update
}

internal sealed record ToastManagerEvent(ToastManagerAction Action, object? Options);

internal sealed record ToastManagerUpdateRequest(string Id, ToastManagerUpdateOptions Updates);
