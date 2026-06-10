using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Tests.Field;

public class FieldValidationTests
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyFormValues = new Dictionary<string, object?>();

    [Fact]
    public async Task CommitAsyncIgnoresStaleCommitAfterSetCustomValidityAsync()
    {
        var data = CreateValidityData();
        var setCustomValidityStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSetCustomValidity = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var validation = new FieldValidation(
            () => data,
            next => data = next,
            (value, _) => Task.FromResult<string[]?>(Equals(value, "first") ? ["first error"] : []),
            getInvalid: null,
            getMarkedDirty: () => true,
            getShouldValidateOnChange: () => false,
            getFormValues: () => EmptyFormValues,
            debounceTime: 0,
            requestStateChange: () => { });

        var firstCommit = validation.CommitAsync(
            "first",
            nativeValidity: FieldNativeValiditySnapshot.Default,
            setCustomValidityAsync: message =>
            {
                if (message == "first error")
                {
                    setCustomValidityStarted.SetResult();
                    return releaseSetCustomValidity.Task;
                }

                return Task.CompletedTask;
            });

        await setCustomValidityStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await validation.CommitAsync("second", nativeValidity: FieldNativeValiditySnapshot.Default);

        releaseSetCustomValidity.SetResult();
        await firstCommit;

        data.Value.ShouldBe("second");
        data.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommitAsyncIgnoresStaleCommitAfterGetNativeValidityAsync()
    {
        var data = CreateValidityData();
        var getNativeValidityStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseGetNativeValidity = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var getNativeValidityCalls = 0;

        var validation = new FieldValidation(
            () => data,
            next => data = next,
            (_, _) => Task.FromResult<string[]?>([]),
            getInvalid: null,
            getMarkedDirty: () => true,
            getShouldValidateOnChange: () => true,
            getFormValues: () => EmptyFormValues,
            debounceTime: 0,
            requestStateChange: () => { });

        var firstCommit = validation.CommitAsync(
            "first",
            nativeValidity: FieldNativeValiditySnapshot.Default,
            setCustomValidityAsync: _ => Task.CompletedTask,
            getNativeValidityAsync: async () =>
            {
                if (Interlocked.Increment(ref getNativeValidityCalls) == 1)
                {
                    getNativeValidityStarted.SetResult();
                    await releaseGetNativeValidity.Task;
                    return new FieldNativeValiditySnapshot(
                        new FieldValidityState(ValueMissing: true, Valid: false),
                        "native error");
                }

                return FieldNativeValiditySnapshot.Default;
            });

        await getNativeValidityStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await validation.CommitAsync(
            "second",
            nativeValidity: FieldNativeValiditySnapshot.Default,
            setCustomValidityAsync: _ => Task.CompletedTask,
            getNativeValidityAsync: () => Task.FromResult<FieldNativeValiditySnapshot?>(FieldNativeValiditySnapshot.Default));

        releaseGetNativeValidity.SetResult();
        await firstCommit;

        data.Value.ShouldBe("second");
        data.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task RevalidateOnlyIgnoresStaleCommitAfterClearingCustomValidityAsync()
    {
        var data = CreateValidityData(
            new FieldValidityState(CustomError: true, Valid: false),
            ["old error"],
            "old error",
            "initial");
        var setCustomValidityStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSetCustomValidity = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var validation = new FieldValidation(
            () => data,
            next => data = next,
            (_, _) => Task.FromResult<string[]?>([]),
            getInvalid: null,
            getMarkedDirty: () => true,
            getShouldValidateOnChange: () => false,
            getFormValues: () => EmptyFormValues,
            debounceTime: 0,
            requestStateChange: () => { });

        var firstCommit = validation.CommitAsync(
            "first",
            revalidateOnly: true,
            nativeValidity: FieldNativeValiditySnapshot.Default,
            setCustomValidityAsync: message =>
            {
                if (message == string.Empty)
                {
                    setCustomValidityStarted.SetResult();
                    return releaseSetCustomValidity.Task;
                }

                return Task.CompletedTask;
            });

        await setCustomValidityStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await validation.CommitAsync("second", nativeValidity: FieldNativeValiditySnapshot.Default);

        releaseSetCustomValidity.SetResult();
        await firstCommit;

        data.Value.ShouldBe("second");
        data.Errors.ShouldBeEmpty();
    }

    private static FieldValidityData CreateValidityData(
        FieldValidityState? state = null,
        string[]? errors = null,
        string? error = null,
        object? value = null)
    {
        return new FieldValidityData(
            state ?? new FieldValidityState(),
            errors ?? [],
            error ?? string.Empty,
            value,
            InitialValue: null);
    }
}
