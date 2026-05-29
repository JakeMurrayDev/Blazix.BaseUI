namespace BlazorBaseUI.Field;

/// <summary>
/// Manages validation logic for a <see cref="FieldRoot"/> component, including
/// custom validation callbacks, native constraint validation, debounced change
/// validation, and initial value tracking.
/// </summary>
public sealed class FieldValidation : IDisposable
{
    private readonly Func<FieldValidityData> getValidityData;
    private readonly Action<FieldValidityData> setValidityData;
    private readonly Func<object?, IReadOnlyDictionary<string, object?>, Task<string[]?>> validate;
    private readonly Func<bool> getInvalid;
    private readonly Func<bool> getMarkedDirty;
    private readonly Func<bool> getShouldValidateOnChange;
    private readonly Func<IReadOnlyDictionary<string, object?>> getFormValues;
    private readonly Action requestStateChange;
    private readonly Action<Exception, string>? logError;
    private readonly Func<object?, Task> cachedCommitCallback;

    private Timer? debounceTimer;
    private object? pendingValue;
    private Func<object?, Task>? pendingCommitCallback;
    private bool initialValueSet;
    private bool isDisposed;
    private int validationCommitId;
    private readonly Lock timerLock = new();

    public FieldValidation(
        Func<FieldValidityData> getValidityData,
        Action<FieldValidityData> setValidityData,
        Func<object?, IReadOnlyDictionary<string, object?>, Task<string[]?>> validate,
        Func<bool>? getInvalid,
        Func<bool>? getMarkedDirty,
        Func<bool> getShouldValidateOnChange,
        Func<IReadOnlyDictionary<string, object?>> getFormValues,
        int debounceTime,
        Action requestStateChange,
        Action<Exception, string>? logError = null)
    {
        this.getValidityData = getValidityData;
        this.setValidityData = setValidityData;
        this.validate = validate;
        this.getInvalid = getInvalid ?? (() => false);
        this.getMarkedDirty = getMarkedDirty ?? (() => false);
        this.getShouldValidateOnChange = getShouldValidateOnChange;
        this.getFormValues = getFormValues;
        DebounceTime = debounceTime;
        this.requestStateChange = requestStateChange;
        this.logError = logError;

        cachedCommitCallback = async value =>
        {
            try
            {
                await CommitAsync(value);
            }
            catch (Exception ex)
            {
                logError?.Invoke(ex, "Error committing validation");
            }
        };
    }

    /// <summary>
    /// Gets or sets the debounce time in milliseconds.
    /// </summary>
    public int DebounceTime { get; set; }

    /// <summary>
    /// Validates the specified value and updates the field's validity data.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="revalidateOnly">When <see langword="true"/>, skips validation if the field is not already invalid.</param>
    /// <param name="nativeValidity">The current browser constraint-validation snapshot, if available.</param>
    /// <param name="setCustomValidityAsync">Callback used to synchronize custom validity on the native control.</param>
    /// <param name="getNativeValidityAsync">Callback used to re-read native validity after custom validity changes.</param>
    public async Task CommitAsync(
        object? value,
        bool revalidateOnly = false,
        FieldNativeValiditySnapshot? nativeValidity = null,
        Func<string, Task>? setCustomValidityAsync = null,
        Func<Task<FieldNativeValiditySnapshot?>>? getNativeValidityAsync = null)
    {
        var currentData = getValidityData();

        if (revalidateOnly && currentData.State.Valid != false)
        {
            if (!Equals(currentData.Value, value))
            {
                setValidityData(currentData with { Value = value });
            }
            return;
        }

        var currentCommitId = Interlocked.Increment(ref validationCommitId);
        var hasNativeValidity = nativeValidity is not null;
        var nextNativeValidity = nativeValidity ?? FieldNativeValiditySnapshot.Default;
        var nextState = nextNativeValidity.State;

        if (revalidateOnly && hasNativeValidity)
        {
            var revalidatedData = await TryResolveRevalidationAsync(
                value,
                currentData,
                nextNativeValidity,
                setCustomValidityAsync);

            if (!IsCurrentCommit(currentCommitId))
                return;

            if (revalidatedData is not null)
            {
                setValidityData(revalidatedData);
                requestStateChange();
                return;
            }

            if (!ShouldContinueRevalidation(nextState))
                return;
        }

        var isValidatingOnChange = getShouldValidateOnChange();
        var result = Array.Empty<string>();
        var validationErrors = new List<string>();
        string? defaultValidationMessage = null;

        if (!string.IsNullOrEmpty(nextNativeValidity.ValidationMessage) && !isValidatingOnChange)
        {
            defaultValidationMessage = nextNativeValidity.ValidationMessage;
            validationErrors.Add(nextNativeValidity.ValidationMessage);
        }
        else
        {
            var customErrors = await validate(value, getFormValues());

            if (currentCommitId != Volatile.Read(ref validationCommitId))
                return;

            if (customErrors is { Length: > 0 })
            {
                result = customErrors;
                validationErrors.AddRange(customErrors);
                nextState = nextState with
                {
                    Valid = false,
                    CustomError = true
                };

                if (setCustomValidityAsync is not null)
                {
                    await setCustomValidityAsync(string.Join('\n', customErrors));

                    if (!IsCurrentCommit(currentCommitId))
                        return;
                }
            }
            else if (isValidatingOnChange)
            {
                if (setCustomValidityAsync is not null)
                {
                    await setCustomValidityAsync(string.Empty);

                    if (!IsCurrentCommit(currentCommitId))
                        return;
                }

                nextState = nextState with { CustomError = false };

                if (getNativeValidityAsync is not null)
                {
                    var refreshedNativeValidity = await getNativeValidityAsync();

                    if (!IsCurrentCommit(currentCommitId))
                        return;

                    if (refreshedNativeValidity is not null)
                    {
                        nextState = refreshedNativeValidity.State;
                        if (!string.IsNullOrEmpty(refreshedNativeValidity.ValidationMessage))
                        {
                            defaultValidationMessage = refreshedNativeValidity.ValidationMessage;
                            validationErrors.Add(refreshedNativeValidity.ValidationMessage);
                        }
                    }
                }
                else if (nextState.Valid == false && !HasAnyValidityError(nextState))
                {
                    nextState = nextState with { Valid = true };
                }
            }
        }

        if (getInvalid())
        {
            nextState = nextState with { Valid = false };
        }
        else if (!hasNativeValidity && nextState.Valid is null && validationErrors.Count == 0)
        {
            nextState = nextState with { Valid = true };
        }

        if (nextState.ValueMissing && !getMarkedDirty() && IsOnlyValueMissing(nextState))
        {
            nextState = nextState with
            {
                Valid = true,
                ValueMissing = false
            };

            if (defaultValidationMessage is not null && validationErrors.Count == 1)
            {
                defaultValidationMessage = null;
                validationErrors.Clear();
            }
        }

        var nextData = currentData with
        {
            State = nextState,
            Errors = [.. validationErrors],
            Error = defaultValidationMessage ?? result.FirstOrDefault() ?? string.Empty,
            Value = value
        };

        if (!IsCurrentCommit(currentCommitId))
            return;

        setValidityData(nextData);
        requestStateChange();
    }

    /// <summary>
    /// Schedules a debounced validation for the specified value.
    /// </summary>
    /// <param name="value">The value to validate after the debounce period.</param>
    public void CommitDebounced(object? value, Func<object?, Task>? commitAsync = null)
    {
        lock (timerLock)
        {
            pendingValue = value;
            pendingCommitCallback = commitAsync;
            Interlocked.Increment(ref validationCommitId);

            if (debounceTimer is null)
            {
                debounceTimer = new Timer(OnDebounceElapsed, null, DebounceTime, Timeout.Infinite);
            }
            else
            {
                debounceTimer.Change(DebounceTime, Timeout.Infinite);
            }
        }
    }

    /// <summary>
    /// Records the initial value of the field if not already set.
    /// </summary>
    /// <param name="value">The initial value to store.</param>
    public void SetInitialValue(object? value)
    {
        if (initialValueSet)
            return;

        initialValueSet = true;
        var currentData = getValidityData();
        setValidityData(currentData with { InitialValue = value, Value = value });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (timerLock)
        {
            isDisposed = true;
            debounceTimer?.Dispose();
            debounceTimer = null;
        }
    }

    private async Task<FieldValidityData?> TryResolveRevalidationAsync(
        object? value,
        FieldValidityData currentData,
        FieldNativeValiditySnapshot nativeValidity,
        Func<string, Task>? setCustomValidityAsync)
    {
        if (currentData.State.Valid != false)
            return currentData.Value == value ? null : currentData with { Value = value };

        if (!nativeValidity.State.ValueMissing)
        {
            if (setCustomValidityAsync is not null)
                await setCustomValidityAsync(string.Empty);

            return currentData with
            {
                State = FieldValidityState.Default with { Valid = true },
                Error = string.Empty,
                Errors = [],
                Value = value
            };
        }

        return null;
    }

    private static bool ShouldContinueRevalidation(FieldValidityState state)
    {
        return state.Valid != false || IsOnlyValueMissing(state);
    }

    private static bool IsOnlyValueMissing(FieldValidityState state)
    {
        return state.ValueMissing &&
               !state.BadInput &&
               !state.CustomError &&
               !state.PatternMismatch &&
               !state.RangeOverflow &&
               !state.RangeUnderflow &&
               !state.StepMismatch &&
               !state.TooLong &&
               !state.TooShort &&
               !state.TypeMismatch;
    }

    private static bool HasAnyValidityError(FieldValidityState state)
    {
        return state.BadInput ||
               state.CustomError ||
               state.PatternMismatch ||
               state.RangeOverflow ||
               state.RangeUnderflow ||
               state.StepMismatch ||
               state.TooLong ||
               state.TooShort ||
               state.TypeMismatch ||
               state.ValueMissing;
    }

    private bool IsCurrentCommit(int commitId)
    {
        return commitId == Volatile.Read(ref validationCommitId);
    }

    private void OnDebounceElapsed(object? state)
    {
        object? valueToCommit;
        Func<object?, Task>? commitCallback;
        lock (timerLock)
        {
            if (isDisposed)
                return;

            valueToCommit = pendingValue;
            commitCallback = pendingCommitCallback;
        }

        _ = (commitCallback ?? cachedCommitCallback)(valueToCommit);
    }
}
