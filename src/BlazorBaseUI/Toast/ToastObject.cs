using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Toast;

/// <summary>
/// Represents an individual toast notification.
/// </summary>
public class ToastObject
{
    /// <summary>The unique identifier for the toast.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The rendered title, string title, or arbitrary title value.</summary>
    public object? Title { get; set; }

    /// <summary>The type of the toast. Used for data attributes and styling.</summary>
    public string? Type { get; set; }

    /// <summary>The rendered description, string description, or arbitrary description value.</summary>
    public object? Description { get; set; }

    /// <summary>The amount of time in milliseconds before the toast is dismissed automatically.</summary>
    public int? Timeout { get; set; }

    /// <summary>The priority used for announcement behavior.</summary>
    public ToastPriority Priority { get; set; } = ToastPriority.Low;

    /// <summary>The current transition status.</summary>
    public TransitionStatus TransitionStatus { get; set; } = TransitionStatus.Undefined;

    /// <summary>A counter incremented whenever the toast is updated or upserted.</summary>
    public int UpdateKey { get; set; }

    /// <summary>Whether the toast is limited by the provider limit.</summary>
    public bool Limited { get; set; }

    /// <summary>The measured natural height of the toast.</summary>
    public double Height { get; set; }

    /// <summary>The root element for focus restoration.</summary>
    public ElementReference? Element { get; set; }

    /// <summary>Callback invoked when the toast is closed.</summary>
    public Action? OnClose { get; set; }

    /// <summary>Blazor callback invoked when the toast is closed.</summary>
    public EventCallback OnCloseCallback { get; set; }

    /// <summary>Callback invoked when the toast is removed after any exit transition completes.</summary>
    public Action? OnRemove { get; set; }

    /// <summary>Blazor callback invoked when the toast is removed after any exit transition completes.</summary>
    public EventCallback OnRemoveCallback { get; set; }

    /// <summary>Options forwarded to the toast action button.</summary>
    public ToastActionOptions? ActionProps { get; set; }

    /// <summary>Options forwarded to the toast positioner.</summary>
    public ToastPositionerOptions? PositionerProps { get; set; }

    /// <summary>Custom payload data associated with the toast.</summary>
    public object? Data { get; set; }

    internal ToastObject Clone() =>
        (ToastObject)MemberwiseClone();
}

/// <summary>
/// Options for adding a toast.
/// </summary>
public class ToastManagerAddOptions
{
    /// <summary>The unique identifier for the toast.</summary>
    public string? Id { get; set; }

    /// <summary>The rendered title, string title, or arbitrary title value.</summary>
    public object? Title { get; set; }

    /// <summary>The type of the toast. Used for data attributes and styling.</summary>
    public string? Type { get; set; }

    /// <summary>The rendered description, string description, or arbitrary description value.</summary>
    public object? Description { get; set; }

    /// <summary>The amount of time in milliseconds before the toast is dismissed automatically.</summary>
    public int? Timeout { get; set; }

    /// <summary>The priority used for announcement behavior.</summary>
    public ToastPriority Priority { get; set; } = ToastPriority.Low;

    /// <summary>Callback invoked when the toast is closed.</summary>
    public Action? OnClose { get; set; }

    /// <summary>Blazor callback invoked when the toast is closed.</summary>
    public EventCallback OnCloseCallback { get; set; }

    /// <summary>Callback invoked when the toast is removed after any exit transition completes.</summary>
    public Action? OnRemove { get; set; }

    /// <summary>Blazor callback invoked when the toast is removed after any exit transition completes.</summary>
    public EventCallback OnRemoveCallback { get; set; }

    /// <summary>Options forwarded to the toast action button.</summary>
    public ToastActionOptions? ActionProps { get; set; }

    /// <summary>Options forwarded to the toast positioner.</summary>
    public ToastPositionerOptions? PositionerProps { get; set; }

    /// <summary>Custom payload data associated with the toast.</summary>
    public object? Data { get; set; }
}

/// <summary>
/// Options for updating an existing toast.
/// </summary>
public class ToastManagerUpdateOptions
{
    /// <summary>The rendered title, string title, or arbitrary title value.</summary>
    public object? Title { get; set; }

    /// <summary>Whether <see cref="Title"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasTitle { get; set; }

    /// <summary>The type of the toast. Used for data attributes and styling.</summary>
    public string? Type { get; set; }

    /// <summary>Whether <see cref="Type"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasType { get; set; }

    /// <summary>The rendered description, string description, or arbitrary description value.</summary>
    public object? Description { get; set; }

    /// <summary>Whether <see cref="Description"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasDescription { get; set; }

    /// <summary>The amount of time in milliseconds before the toast is dismissed automatically.</summary>
    public int? Timeout { get; set; }

    /// <summary>Whether <see cref="Timeout"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasTimeout { get; set; }

    /// <summary>The priority used for announcement behavior.</summary>
    public ToastPriority? Priority { get; set; }

    /// <summary>Callback invoked when the toast is closed.</summary>
    public Action? OnClose { get; set; }

    /// <summary>Whether <see cref="OnClose"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasOnClose { get; set; }

    /// <summary>Blazor callback invoked when the toast is closed.</summary>
    public EventCallback OnCloseCallback { get; set; }

    /// <summary>Whether <see cref="OnCloseCallback"/> should be applied when it has no delegate.</summary>
    public bool HasOnCloseCallback { get; set; }

    /// <summary>Callback invoked when the toast is removed after any exit transition completes.</summary>
    public Action? OnRemove { get; set; }

    /// <summary>Whether <see cref="OnRemove"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasOnRemove { get; set; }

    /// <summary>Blazor callback invoked when the toast is removed after any exit transition completes.</summary>
    public EventCallback OnRemoveCallback { get; set; }

    /// <summary>Whether <see cref="OnRemoveCallback"/> should be applied when it has no delegate.</summary>
    public bool HasOnRemoveCallback { get; set; }

    /// <summary>Options forwarded to the toast action button.</summary>
    public ToastActionOptions? ActionProps { get; set; }

    /// <summary>Whether <see cref="ActionProps"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasActionProps { get; set; }

    /// <summary>Options forwarded to the toast positioner.</summary>
    public ToastPositionerOptions? PositionerProps { get; set; }

    /// <summary>Whether <see cref="PositionerProps"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasPositionerProps { get; set; }

    /// <summary>Custom payload data associated with the toast.</summary>
    public object? Data { get; set; }

    /// <summary>Whether <see cref="Data"/> should be applied when it is <see langword="null"/>.</summary>
    public bool HasData { get; set; }
}

/// <summary>
/// Options for promise-backed toasts.
/// </summary>
/// <typeparam name="TValue">The promise result type.</typeparam>
public sealed class ToastManagerPromiseOptions<TValue>
{
    /// <summary>Options used while the promise is pending.</summary>
    public ToastPromiseOption<TValue> Loading { get; set; } = string.Empty;

    /// <summary>Options used when the promise resolves.</summary>
    public ToastPromiseOption<TValue> Success { get; set; } = string.Empty;

    /// <summary>Options used when the promise rejects.</summary>
    public ToastPromiseOption<Exception> Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents one promise toast branch.
/// </summary>
/// <typeparam name="TValue">The value used to resolve this branch.</typeparam>
public sealed class ToastPromiseOption<TValue>
{
    private readonly Func<TValue?, ToastManagerUpdateOptions> resolver;

    private ToastPromiseOption(Func<TValue?, ToastManagerUpdateOptions> resolver)
    {
        this.resolver = resolver;
    }

    /// <summary>Converts string content into a description update.</summary>
    public static implicit operator ToastPromiseOption<TValue>(string value) =>
        new(_ => new ToastManagerUpdateOptions
        {
            Description = value,
            HasDescription = true
        });

    /// <summary>Converts update options into a promise branch.</summary>
    public static implicit operator ToastPromiseOption<TValue>(ToastManagerUpdateOptions value) =>
        new(_ => value);

    /// <summary>Converts a value-to-string resolver into a description update.</summary>
    public static implicit operator ToastPromiseOption<TValue>(Func<TValue, string> value) =>
        new(result => new ToastManagerUpdateOptions
        {
            Description = value(result!),
            HasDescription = true
        });

    /// <summary>Converts a value-to-options resolver into a promise branch.</summary>
    public static implicit operator ToastPromiseOption<TValue>(Func<TValue, ToastManagerUpdateOptions> value) =>
        new(result => value(result!));

    /// <summary>Creates a description branch from a string.</summary>
    public static ToastPromiseOption<TValue> From(string value) => value;

    /// <summary>Creates a branch from update options.</summary>
    public static ToastPromiseOption<TValue> From(ToastManagerUpdateOptions value) => value;

    /// <summary>Creates a branch from a value-to-string resolver.</summary>
    public static ToastPromiseOption<TValue> From(Func<TValue, string> value) => value;

    /// <summary>Creates a branch from a value-to-options resolver.</summary>
    public static ToastPromiseOption<TValue> From(Func<TValue, ToastManagerUpdateOptions> value) => value;

    internal ToastManagerUpdateOptions Resolve(TValue? value = default) => resolver(value);
}

/// <summary>
/// Options forwarded to <see cref="ToastAction"/>.
/// </summary>
public sealed class ToastActionOptions
{
    /// <summary>Whether the action is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>Whether the action renders native button attributes.</summary>
    public bool NativeButton { get; set; } = true;

    /// <summary>The action child content.</summary>
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Callback invoked when the action is clicked.</summary>
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Additional action element attributes.</summary>
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}

/// <summary>
/// Options forwarded to <see cref="ToastPositioner"/>.
/// </summary>
public sealed class ToastPositionerOptions
{
    /// <summary>An element to position the toast against.</summary>
    public ElementReference? Anchor { get; set; }

    /// <summary>Which side of the anchor element to align the toast against.</summary>
    public Side Side { get; set; } = Side.Top;

    /// <summary>The alignment of the toast relative to the specified side.</summary>
    public Align Align { get; set; } = Align.Center;

    /// <summary>The offset in pixels from the side of the anchor element.</summary>
    public double SideOffset { get; set; }

    /// <summary>The offset in pixels from the alignment edge of the anchor element.</summary>
    public double AlignOffset { get; set; }

    /// <summary>The collision boundary used to detect repositioning.</summary>
    public CollisionBoundary CollisionBoundary { get; set; } = CollisionBoundary.ClippingAncestors;

    /// <summary>The padding in pixels between the toast and the collision boundary edge.</summary>
    public double CollisionPadding { get; set; } = 5;

    /// <summary>Per-side collision padding.</summary>
    public SidePadding? CollisionPaddingPerSide { get; set; }

    /// <summary>The minimum padding in pixels between the arrow and the popup edge.</summary>
    public double ArrowPadding { get; set; } = 5;

    /// <summary>Whether the toast stays attached to the anchor while overflowing.</summary>
    public bool Sticky { get; set; }

    /// <summary>Whether anchor tracking updates are disabled.</summary>
    public bool DisableAnchorTracking { get; set; }

    /// <summary>The CSS positioning method.</summary>
    public PositionMethod PositionMethod { get; set; } = PositionMethod.Absolute;

    /// <summary>How collisions are handled when positioning the toast.</summary>
    public CollisionAvoidance? CollisionAvoidance { get; set; }
}
