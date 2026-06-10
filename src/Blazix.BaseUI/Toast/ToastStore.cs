namespace Blazix.BaseUI.Toast;

internal sealed class ToastStore : IDisposable
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, TimerInfo> timers = new(StringComparer.Ordinal);
    private List<ToastObject> toasts = [];
    private bool areTimersPaused;
    private int timeout;
    private int limit;
    private bool hovering;
    private bool focused;
    private bool isWindowFocused = true;
    private bool disposed;

    public event Action? Changed;

    public event Func<string?, Task>? FocusManagementRequested;

    public IReadOnlyList<ToastObject> Toasts
    {
        get
        {
            lock (syncRoot)
            {
                return toasts.ToArray();
            }
        }
    }

    public bool Hovering
    {
        get
        {
            lock (syncRoot)
            {
                return hovering;
            }
        }
    }

    public bool Focused
    {
        get
        {
            lock (syncRoot)
            {
                return focused;
            }
        }
    }

    public bool IsWindowFocused
    {
        get
        {
            lock (syncRoot)
            {
                return isWindowFocused;
            }
        }
    }

    public int Timeout
    {
        get
        {
            lock (syncRoot)
            {
                return timeout;
            }
        }
        set
        {
            lock (syncRoot)
            {
                timeout = value;
            }
        }
    }

    public int Limit
    {
        get
        {
            lock (syncRoot)
            {
                return limit;
            }
        }
        set
        {
            lock (syncRoot)
            {
                limit = value;
            }
        }
    }

    public bool Expanded
    {
        get
        {
            lock (syncRoot)
            {
                return hovering || focused;
            }
        }
    }

    public bool ExpandedOrOutOfFocus
    {
        get
        {
            lock (syncRoot)
            {
                return hovering || focused || !isWindowFocused;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (syncRoot)
            {
                return toasts.Count == 0;
            }
        }
    }

    public ToastStore(int timeout, int limit)
    {
        this.timeout = timeout;
        this.limit = limit;
    }

    public void SetFocused(bool focused)
    {
        lock (syncRoot)
        {
            if (this.focused == focused)
            {
                return;
            }

            this.focused = focused;
        }

        NotifyChanged();
    }

    public void SetHovering(bool hovering)
    {
        lock (syncRoot)
        {
            if (this.hovering == hovering)
            {
                return;
            }

            this.hovering = hovering;
        }

        NotifyChanged();
    }

    public void SetIsWindowFocused(bool isWindowFocused)
    {
        lock (syncRoot)
        {
            if (this.isWindowFocused == isWindowFocused)
            {
                return;
            }

            this.isWindowFocused = isWindowFocused;
        }

        NotifyChanged();
    }

    public string AddToast(ToastManagerAddOptions options)
    {
        lock (syncRoot)
        {
            ThrowIfDisposed();

            var id = string.IsNullOrWhiteSpace(options.Id)
                ? GenerateId()
                : options.Id!;

            if (!string.IsNullOrWhiteSpace(options.Id))
            {
                var existing = GetToast(id);
                if (existing is not null)
                {
                    if (existing.TransitionStatus == TransitionStatus.Ending)
                    {
                        RemoveToast(id, skipOnRemove: true);
                    }
                    else
                    {
                        UpdateToastInternal(id, ToUpdateOptions(options), resetTimer: true, markUpdated: true);
                        return id;
                    }
                }
            }

            var toast = ToToastObject(id, options);
            var updatedToasts = new List<ToastObject>(toasts.Count + 1) { toast };
            updatedToasts.AddRange(toasts);
            ApplyLimit(updatedToasts);
            SetToasts(updatedToasts);

            var duration = toast.Timeout ?? timeout;
            if (!string.Equals(toast.Type, "loading", StringComparison.Ordinal) && duration > 0)
            {
                ScheduleTimer(id, duration, () => CloseToast(id));
            }

            if (hovering || focused || !isWindowFocused)
            {
                PauseTimers();
            }

            return id;
        }
    }

    public void UpdateToast(string id, ToastManagerUpdateOptions updates)
    {
        lock (syncRoot)
        {
            UpdateToastInternal(id, updates, resetTimer: false, markUpdated: true);
        }
    }

    public void UpdateToastHeight(string id, double height)
    {
        lock (syncRoot)
        {
            UpdateToastInternal(
                id,
                new ToastManagerUpdateOptions(),
                resetTimer: false,
                markUpdated: false,
                height: height,
                clearStartingStatus: true);
        }
    }

    public void SetToastElement(string id, Microsoft.AspNetCore.Components.ElementReference? element)
    {
        lock (syncRoot)
        {
            var toast = GetToast(id);
            if (toast is null || Nullable.Equals(toast.Element, element))
            {
                return;
            }

            toast.Element = element;
        }
    }

    public void CloseToast(string? toastId = null)
    {
        lock (syncRoot)
        {
            ThrowIfDisposed();

            var closeAll = toastId is null;
            var toastsToClose = new List<ToastObject>();

            if (closeAll)
            {
                toastsToClose.AddRange(toasts);
                foreach (var timer in timers.Values)
                {
                    timer.CancellationTokenSource?.Cancel();
                    timer.CancellationTokenSource?.Dispose();
                    timer.CancellationTokenSource = null;
                }
                timers.Clear();
            }
            else
            {
                var toast = GetToast(toastId);
                if (toast is null)
                {
                    return;
                }

                toastsToClose.Add(toast);
                ClearTimer(toastId!);
            }

            var activeIndex = 0;
            var changed = false;
            var newToasts = new List<ToastObject>(toasts.Count);
            foreach (var item in toasts)
            {
                if (closeAll || item.Id == toastId)
                {
                    if (item.TransitionStatus != TransitionStatus.Ending || item.Height != 0)
                    {
                        var closed = item.Clone();
                        closed.TransitionStatus = TransitionStatus.Ending;
                        closed.Height = 0;
                        newToasts.Add(closed);
                        changed = true;
                    }
                    else
                    {
                        newToasts.Add(item);
                    }

                    continue;
                }

                if (item.TransitionStatus == TransitionStatus.Ending)
                {
                    newToasts.Add(item);
                    continue;
                }

                var limited = activeIndex >= limit;
                activeIndex++;
                if (item.Limited != limited)
                {
                    var next = item.Clone();
                    next.Limited = limited;
                    newToasts.Add(next);
                    changed = true;
                }
                else
                {
                    newToasts.Add(item);
                }
            }

            if (!changed)
            {
                return;
            }

            toasts = newToasts;
            if (closeAll || toasts.Count == 1)
            {
                hovering = false;
                focused = false;
            }

            NotifyChanged();

            foreach (var toast in toastsToClose)
            {
                if (toast.TransitionStatus != TransitionStatus.Ending)
                {
                    toast.OnClose?.Invoke();
                    InvokeEventCallback(toast.OnCloseCallback);
                }
            }

            _ = RequestFocusManagementAsync(toastId);
        }
    }

    public void RemoveToast(string toastId, bool skipOnRemove = false)
    {
        lock (syncRoot)
        {
            var index = toasts.FindIndex(toast => toast.Id == toastId);
            if (index < 0)
            {
                return;
            }

            var toast = toasts[index];
            if (!skipOnRemove)
            {
                toast.OnRemove?.Invoke();
                InvokeEventCallback(toast.OnRemoveCallback);
            }

            var newToasts = toasts.ToList();
            newToasts.RemoveAt(index);
            ApplyLimit(newToasts);
            SetToasts(newToasts);
        }
    }

    public Task<TValue> PromiseToast<TValue>(Task<TValue> task, ToastManagerPromiseOptions<TValue> options)
    {
        var loadingOptions = ToAddOptions(options.Loading.Resolve());
        loadingOptions.Type = "loading";
        var id = AddToast(loadingOptions);

        return HandlePromiseAsync(task, options, id);
    }

    public void PauseTimers()
    {
        lock (syncRoot)
        {
            if (areTimersPaused)
            {
                return;
            }

            areTimersPaused = true;
            foreach (var timer in timers.Values)
            {
                if (timer.CancellationTokenSource is null)
                {
                    continue;
                }

                timer.CancellationTokenSource.Cancel();
                timer.CancellationTokenSource.Dispose();
                timer.CancellationTokenSource = null;

                var elapsed = (int)Math.Max(0, (DateTimeOffset.UtcNow - timer.StartedAt).TotalMilliseconds);
                timer.Remaining = Math.Max(0, timer.Delay - elapsed);
            }
        }
    }

    public void ResumeTimers()
    {
        lock (syncRoot)
        {
            if (!areTimersPaused)
            {
                return;
            }

            areTimersPaused = false;
            foreach (var (id, timer) in timers.ToArray())
            {
                timer.Remaining = timer.Remaining > 0 ? timer.Remaining : timer.Delay;
                StartTimer(id, timer, timer.Remaining);
            }
        }
    }

    public int GetToastIndex(string id)
    {
        lock (syncRoot)
        {
            return toasts.FindIndex(toast => toast.Id == id);
        }
    }

    public int GetToastVisibleIndex(string id)
    {
        lock (syncRoot)
        {
            var visibleIndex = 0;
            foreach (var toast in toasts)
            {
                if (toast.Id == id)
                {
                    return toast.TransitionStatus == TransitionStatus.Ending ? -1 : visibleIndex;
                }

                if (toast.TransitionStatus != TransitionStatus.Ending)
                {
                    visibleIndex++;
                }
            }

            return -1;
        }
    }

    public double GetToastOffsetY(string id)
    {
        lock (syncRoot)
        {
            double offsetY = 0;
            foreach (var toast in toasts)
            {
                if (toast.Id == id)
                {
                    return offsetY;
                }

                offsetY += toast.Height;
            }

            return 0;
        }
    }

    public double GetFrontmostHeight()
    {
        lock (syncRoot)
        {
            return toasts.FirstOrDefault()?.Height ?? 0;
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (var timer in timers.Values)
            {
                timer.CancellationTokenSource?.Cancel();
                timer.CancellationTokenSource?.Dispose();
                timer.CancellationTokenSource = null;
            }
            timers.Clear();
        }
    }

    private void UpdateToastInternal(
        string id,
        ToastManagerUpdateOptions updates,
        bool resetTimer,
        bool markUpdated,
        double? height = null,
        bool clearStartingStatus = false)
    {
        var index = toasts.FindIndex(toast => toast.Id == id);
        if (index < 0)
        {
            return;
        }

        var prevToast = toasts[index];
        if (prevToast.TransitionStatus == TransitionStatus.Ending)
        {
            return;
        }

        var nextToast = prevToast.Clone();
        ApplyUpdates(nextToast, updates);

        if (height.HasValue)
        {
            nextToast.Height = height.Value;
        }

        if (clearStartingStatus && nextToast.TransitionStatus == TransitionStatus.Starting)
        {
            nextToast.TransitionStatus = TransitionStatus.Undefined;
        }

        if (markUpdated)
        {
            nextToast.UpdateKey = prevToast.UpdateKey + 1;
        }

        var newToasts = toasts.ToList();
        newToasts[index] = nextToast;
        toasts = newToasts;
        NotifyChanged();

        var nextTimeout = nextToast.Timeout ?? timeout;
        var prevTimeout = prevToast.Timeout ?? timeout;
        var timeoutUpdated = updates.HasTimeout;
        var shouldHaveTimer = nextToast.TransitionStatus != TransitionStatus.Ending
                              && !string.Equals(nextToast.Type, "loading", StringComparison.Ordinal)
                              && nextTimeout > 0;
        var hasTimer = timers.ContainsKey(id);
        var timeoutChanged = prevTimeout != nextTimeout;
        var wasLoading = string.Equals(prevToast.Type, "loading", StringComparison.Ordinal);

        if (!shouldHaveTimer && hasTimer)
        {
            ClearTimer(id);
            return;
        }

        if (shouldHaveTimer && (!hasTimer || timeoutChanged || timeoutUpdated || wasLoading || resetTimer))
        {
            ClearTimer(id);
            ScheduleTimer(id, nextTimeout, () => CloseToast(id));

            if (hovering || focused || !isWindowFocused)
            {
                PauseTimers();
            }
        }
    }

    private void SetToasts(List<ToastObject> newToasts)
    {
        toasts = newToasts;
        if (newToasts.Count == 0)
        {
            hovering = false;
            focused = false;
        }

        NotifyChanged();
    }

    private void ApplyLimit(List<ToastObject> updatedToasts)
    {
        var activeToasts = updatedToasts
            .Where(toast => toast.TransitionStatus != TransitionStatus.Ending)
            .ToList();

        if (activeToasts.Count > limit)
        {
            var excessCount = activeToasts.Count - limit;
            var limitedIds = activeToasts
                .TakeLast(excessCount)
                .Select(toast => toast.Id)
                .ToHashSet(StringComparer.Ordinal);

            for (var index = 0; index < updatedToasts.Count; index++)
            {
                var toast = updatedToasts[index];
                var limited = limitedIds.Contains(toast.Id);
                if (toast.Limited == limited)
                {
                    continue;
                }

                var next = toast.Clone();
                next.Limited = limited;
                updatedToasts[index] = next;
            }
        }
        else
        {
            for (var index = 0; index < updatedToasts.Count; index++)
            {
                var toast = updatedToasts[index];
                if (!toast.Limited)
                {
                    continue;
                }

                var next = toast.Clone();
                next.Limited = false;
                updatedToasts[index] = next;
            }
        }
    }

    private void ScheduleTimer(string id, int delay, Action callback)
    {
        var timer = new TimerInfo
        {
            Delay = delay,
            Remaining = delay,
            Callback = callback
        };

        timers[id] = timer;

        if (!hovering && !focused && isWindowFocused)
        {
            StartTimer(id, timer, delay);
        }
    }

    private void StartTimer(string id, TimerInfo timer, int delay)
    {
        timer.CancellationTokenSource?.Cancel();
        timer.CancellationTokenSource?.Dispose();

        var cts = new CancellationTokenSource();
        timer.CancellationTokenSource = cts;
        timer.StartedAt = DateTimeOffset.UtcNow;

        _ = Task.Run(async () =>
        {
            var shouldInvoke = false;
            try
            {
                await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                if (cts.Token.IsCancellationRequested)
                {
                    return;
                }

                lock (syncRoot)
                {
                    if (timers.Remove(id))
                    {
                        if (ReferenceEquals(timer.CancellationTokenSource, cts))
                        {
                            timer.CancellationTokenSource = null;
                        }

                        shouldInvoke = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cts.Dispose();
            }

            if (shouldInvoke)
            {
                InvokeTimerCallbackIfActive(timer.Callback);
            }
        });
    }

    private void ClearTimer(string id)
    {
        if (!timers.Remove(id, out var timer))
        {
            return;
        }

        timer.CancellationTokenSource?.Cancel();
        timer.CancellationTokenSource?.Dispose();
        timer.CancellationTokenSource = null;
    }

    private void InvokeTimerCallbackIfActive(Action callback)
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            callback();
        }
    }

    private async Task RequestFocusManagementAsync(string? toastId)
    {
        var handler = FocusManagementRequested;
        if (handler is null)
        {
            return;
        }

        await handler.Invoke(toastId);
    }

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
            UpdateToast(id, successOptions);
            return result;
        }
        catch (Exception ex)
        {
            var errorOptions = options.Error.Resolve(ex);
            errorOptions.Type ??= "error";
            errorOptions.HasType = true;
            UpdateToast(id, errorOptions);
            throw;
        }
    }

    private ToastObject? GetToast(string? id) =>
        id is null ? null : toasts.FirstOrDefault(toast => toast.Id == id);

    private void NotifyChanged()
    {
        Action? changed;
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            changed = Changed;
        }

        changed?.Invoke();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static ToastObject ToToastObject(string id, ToastManagerAddOptions options) =>
        new()
        {
            Id = id,
            Title = options.Title,
            Type = options.Type,
            Description = options.Description,
            Timeout = options.Timeout,
            Priority = options.Priority,
            TransitionStatus = TransitionStatus.Starting,
            OnClose = options.OnClose,
            OnCloseCallback = options.OnCloseCallback,
            OnRemove = options.OnRemove,
            OnRemoveCallback = options.OnRemoveCallback,
            ActionProps = options.ActionProps,
            PositionerProps = options.PositionerProps,
            Data = options.Data
        };

    private static ToastManagerUpdateOptions ToUpdateOptions(ToastManagerAddOptions options) =>
        new()
        {
            Title = options.Title,
            HasTitle = true,
            Type = options.Type,
            HasType = true,
            Description = options.Description,
            HasDescription = true,
            Timeout = options.Timeout,
            HasTimeout = true,
            Priority = options.Priority,
            OnClose = options.OnClose,
            HasOnClose = true,
            OnCloseCallback = options.OnCloseCallback,
            HasOnCloseCallback = options.OnCloseCallback.HasDelegate,
            OnRemove = options.OnRemove,
            HasOnRemove = true,
            OnRemoveCallback = options.OnRemoveCallback,
            HasOnRemoveCallback = options.OnRemoveCallback.HasDelegate,
            ActionProps = options.ActionProps,
            HasActionProps = true,
            PositionerProps = options.PositionerProps,
            HasPositionerProps = true,
            Data = options.Data,
            HasData = true
        };

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

    private static void ApplyUpdates(ToastObject toast, ToastManagerUpdateOptions updates)
    {
        if (updates.HasTitle)
        {
            toast.Title = updates.Title;
        }
        else if (updates.Title is not null)
        {
            toast.Title = updates.Title;
        }

        if (updates.HasType)
        {
            toast.Type = updates.Type;
        }
        else if (updates.Type is not null)
        {
            toast.Type = updates.Type;
        }

        if (updates.HasDescription)
        {
            toast.Description = updates.Description;
        }
        else if (updates.Description is not null)
        {
            toast.Description = updates.Description;
        }

        if (updates.HasTimeout)
        {
            toast.Timeout = updates.Timeout;
        }
        else if (updates.Timeout.HasValue)
        {
            toast.Timeout = updates.Timeout;
        }

        if (updates.Priority.HasValue)
        {
            toast.Priority = updates.Priority.Value;
        }

        if (updates.HasOnClose)
        {
            toast.OnClose = updates.OnClose;
        }

        if (updates.HasOnCloseCallback)
        {
            toast.OnCloseCallback = updates.OnCloseCallback;
        }

        if (updates.HasOnRemove)
        {
            toast.OnRemove = updates.OnRemove;
        }

        if (updates.HasOnRemoveCallback)
        {
            toast.OnRemoveCallback = updates.OnRemoveCallback;
        }

        if (updates.HasActionProps)
        {
            toast.ActionProps = updates.ActionProps;
        }
        else if (updates.ActionProps is not null)
        {
            toast.ActionProps = updates.ActionProps;
        }

        if (updates.HasPositionerProps)
        {
            toast.PositionerProps = updates.PositionerProps;
        }
        else if (updates.PositionerProps is not null)
        {
            toast.PositionerProps = updates.PositionerProps;
        }

        if (updates.HasData)
        {
            toast.Data = updates.Data;
        }
        else if (updates.Data is not null)
        {
            toast.Data = updates.Data;
        }
    }

    private static string GenerateId() => $"toast-{Guid.NewGuid().ToString("N")[..8]}";

    private static void InvokeEventCallback(Microsoft.AspNetCore.Components.EventCallback callback)
    {
        if (callback.HasDelegate)
        {
            _ = callback.InvokeAsync();
        }
    }

    private sealed class TimerInfo
    {
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public int Delay { get; set; }

        public int Remaining { get; set; }

        public Action Callback { get; set; } = null!;
    }
}
