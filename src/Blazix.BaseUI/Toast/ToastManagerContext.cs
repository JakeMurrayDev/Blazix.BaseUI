namespace Blazix.BaseUI.Toast;

/// <summary>
/// Provides the Blazor equivalent of React Base UI's <c>useToastManager</c> return value.
/// </summary>
public sealed class ToastManagerContext
{
    private readonly ToastStore store;

    internal ToastManagerContext(ToastStore store)
    {
        this.store = store;
    }

    /// <summary>The current toast list, newest first.</summary>
    public IReadOnlyList<ToastObject> Toasts => store.Toasts;

    /// <summary>Adds a toast and returns its identifier.</summary>
    public string Add(ToastManagerAddOptions options) => store.AddToast(options);

    /// <summary>Closes one toast or all toasts when <paramref name="toastId"/> is <see langword="null"/>.</summary>
    public void Close(string? toastId = null) => store.CloseToast(toastId);

    /// <summary>Updates an existing toast.</summary>
    public void Update(string toastId, ToastManagerUpdateOptions options) => store.UpdateToast(toastId, options);

    /// <summary>Adds a loading toast, then updates it when the task resolves or rejects.</summary>
    public Task<TValue> Promise<TValue>(Task<TValue> task, ToastManagerPromiseOptions<TValue> options) =>
        store.PromiseToast(task, options);
}
