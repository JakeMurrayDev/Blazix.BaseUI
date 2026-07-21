using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazix.BaseUI;

/// <summary>
/// Provides shared JS interop lifecycle management for Positioner components.
/// Encapsulates the common <c>initializePositioner</c>, <c>updatePosition</c>, and
/// <c>disposePositioner</c> patterns shared by all six positioner components.
/// <para>
/// Maps to the shared portions of React's <c>useAnchorPositioning</c> hook
/// that handle Floating UI middleware configuration and position updates.
/// </para>
/// </summary>
internal sealed class PositionerInterop : IAsyncDisposable
{
    private readonly Func<Task<IJSObjectReference>> getModule;
    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;

    /// <summary>
    /// Initializes a new instance of <see cref="PositionerInterop"/>.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime for module import.</param>
    /// <param name="modulePath">The component JS module path (e.g., "./_content/Blazix.BaseUI/blazix-baseui-tooltip.min.js").</param>
    public PositionerInterop(IJSRuntime jsRuntime, string modulePath)
    {
        getModule = () => jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath).AsTask();
    }

    /// <summary>
    /// Gets the positioner ID assigned by JavaScript after initialization.
    /// <see langword="null"/> when not yet initialized.
    /// </summary>
    public string? PositionerId { get; private set; }

    /// <summary>
    /// Gets whether the positioner has been initialized (has a valid ID).
    /// </summary>
    public bool IsInitialized => !string.IsNullOrEmpty(PositionerId);

    /// <summary>
    /// Gets whether the first render has occurred.
    /// </summary>
    public bool HasRendered => hasRendered;

    /// <summary>
    /// Gets the lazily-initialized JS module reference.
    /// </summary>
    private Task<IJSObjectReference> ModuleTask => (moduleTask ??= new Lazy<Task<IJSObjectReference>>(getModule)).Value;

    /// <summary>
    /// Gets the JS module reference for components that need custom argument assembly.
    /// Components like PopoverPositioner and MenuPositioner have non-standard collision padding
    /// or additional callback args that don't fit the standard <see cref="InitializeAsync"/> signature.
    /// </summary>
    internal Task<IJSObjectReference> GetModuleAsync() => ModuleTask;

    /// <summary>
    /// Sets the positioner ID directly. Used by components that call <c>initializePositioner</c>
    /// via <see cref="GetModuleAsync"/> instead of <see cref="InitializeAsync"/>.
    /// </summary>
    internal void SetPositionerId(string? id) => PositionerId = id;

    internal async Task ResetAsync()
    {
        var id = PositionerId;
        PositionerId = null;
        if (moduleTask?.IsValueCreated != true || string.IsNullOrEmpty(id))
        {
            return;
        }

        try
        {
            var module = await ModuleTask;
            await module.InvokeVoidAsync("disposePositioner", id);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
        {
            _ = ex;
        }
    }

    /// <summary>
    /// Marks the first render as complete. Call from <c>OnAfterRenderAsync</c> when <c>firstRender</c> is <see langword="true"/>.
    /// </summary>
    public void MarkRendered()
    {
        hasRendered = true;
    }

    /// <summary>
    /// Initializes the positioner via JS interop with the standard argument set.
    /// The caller provides the popup/anchor elements and may append additional arguments.
    /// </summary>
    /// <param name="popupElement">The popup element reference.</param>
    /// <param name="anchorElement">The anchor element reference.</param>
    /// <param name="config">The shared positioning configuration.</param>
    /// <param name="arrowElement">The optional arrow element reference.</param>
    /// <param name="extraArgs">Additional component-specific arguments to append.</param>
    /// <returns>The positioner ID.</returns>
    public async Task InitializeAsync(
        ElementReference popupElement,
        ElementReference anchorElement,
        PositionerConfig config,
        ElementReference? arrowElement,
        params object?[] extraArgs)
    {
        try
        {
            var module = await ModuleTask;

            var args = new List<object?>
            {
                popupElement,
                anchorElement,
                config.Side.ToDataAttributeString(),
                config.Align.ToDataAttributeString(),
                config.SideOffset,
                config.AlignOffset,
                config.CollisionPaddingArgument,
                config.CollisionBoundaryArgument,
                config.ArrowPadding,
                arrowElement,
                config.Sticky,
                config.PositionMethodString,
                config.DisableAnchorTracking
            };

            config.AppendCollisionAvoidanceArgs(args);
            args.AddRange(extraArgs);

            PositionerId = await module.InvokeAsync<string>("initializePositioner", args.ToArray());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            _ = ex; // Expected during circuit disconnect - safe to ignore.
        }
    }

    /// <summary>
    /// Updates the position via JS interop with the standard argument set.
    /// </summary>
    /// <param name="anchorElement">The anchor element reference.</param>
    /// <param name="config">The shared positioning configuration.</param>
    /// <param name="arrowElement">The optional arrow element reference.</param>
    /// <param name="extraArgs">Additional component-specific arguments to append.</param>
    public async Task UpdateAsync(
        ElementReference anchorElement,
        PositionerConfig config,
        ElementReference? arrowElement,
        params object?[] extraArgs)
    {
        if (string.IsNullOrEmpty(PositionerId))
        {
            return;
        }

        try
        {
            var module = await ModuleTask;

            var args = new List<object?>
            {
                PositionerId,
                anchorElement,
                config.Side.ToDataAttributeString(),
                config.Align.ToDataAttributeString(),
                config.SideOffset,
                config.AlignOffset,
                config.CollisionPaddingArgument,
                config.CollisionBoundaryArgument,
                config.ArrowPadding,
                arrowElement,
                config.Sticky,
                config.PositionMethodString
            };

            config.AppendCollisionAvoidanceArgs(args);
            args.AddRange(extraArgs);

            await module.InvokeVoidAsync("updatePosition", args.ToArray());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            _ = ex; // Expected during circuit disconnect - safe to ignore.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (moduleTask?.IsValueCreated == true)
        {
            IJSObjectReference? module = null;
            try
            {
                module = await ModuleTask;
                if (hasRendered && !string.IsNullOrEmpty(PositionerId))
                {
                    await module.InvokeVoidAsync("disposePositioner", PositionerId);
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
            {
                _ = ex; // Expected during circuit disconnect - safe to ignore.
            }
            finally
            {
                if (module is not null)
                {
                    try
                    {
                        await module.DisposeAsync();
                    }
                    catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
                    {
                        _ = ex;
                    }
                }
            }
        }

        PositionerId = null;
    }
}
