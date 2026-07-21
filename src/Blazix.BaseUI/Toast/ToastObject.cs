using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Toast;

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
    private string? id;
    private object? title;
    private string? type;
    private object? description;
    private int? timeout;
    private ToastPriority priority = ToastPriority.Low;
    private Action? onClose;
    private EventCallback onCloseCallback;
    private Action? onRemove;
    private EventCallback onRemoveCallback;
    private ToastActionOptions? actionProps;
    private ToastPositionerOptions? positionerProps;
    private object? data;

    /// <summary>The unique identifier for the toast.</summary>
    public string? Id
    {
        get => id;
        set
        {
            id = value;
            HasId = true;
        }
    }

    /// <summary>The rendered title, string title, or arbitrary title value.</summary>
    public object? Title
    {
        get => title;
        set
        {
            title = value;
            HasTitle = true;
        }
    }

    /// <summary>The type of the toast. Used for data attributes and styling.</summary>
    public string? Type
    {
        get => type;
        set
        {
            type = value;
            HasType = true;
        }
    }

    /// <summary>The rendered description, string description, or arbitrary description value.</summary>
    public object? Description
    {
        get => description;
        set
        {
            description = value;
            HasDescription = true;
        }
    }

    /// <summary>The amount of time in milliseconds before the toast is dismissed automatically.</summary>
    public int? Timeout
    {
        get => timeout;
        set
        {
            timeout = value;
            HasTimeout = true;
        }
    }

    /// <summary>The priority used for announcement behavior.</summary>
    public ToastPriority Priority
    {
        get => priority;
        set
        {
            priority = value;
            HasPriority = true;
        }
    }

    /// <summary>Callback invoked when the toast is closed.</summary>
    public Action? OnClose
    {
        get => onClose;
        set
        {
            onClose = value;
            HasOnClose = true;
        }
    }

    /// <summary>Blazor callback invoked when the toast is closed.</summary>
    public EventCallback OnCloseCallback
    {
        get => onCloseCallback;
        set
        {
            onCloseCallback = value;
            HasOnCloseCallback = true;
        }
    }

    /// <summary>Callback invoked when the toast is removed after any exit transition completes.</summary>
    public Action? OnRemove
    {
        get => onRemove;
        set
        {
            onRemove = value;
            HasOnRemove = true;
        }
    }

    /// <summary>Blazor callback invoked when the toast is removed after any exit transition completes.</summary>
    public EventCallback OnRemoveCallback
    {
        get => onRemoveCallback;
        set
        {
            onRemoveCallback = value;
            HasOnRemoveCallback = true;
        }
    }

    /// <summary>Options forwarded to the toast action button.</summary>
    public ToastActionOptions? ActionProps
    {
        get => actionProps;
        set
        {
            actionProps = value;
            HasActionProps = true;
        }
    }

    /// <summary>Options forwarded to the toast positioner.</summary>
    public ToastPositionerOptions? PositionerProps
    {
        get => positionerProps;
        set
        {
            positionerProps = value;
            HasPositionerProps = true;
        }
    }

    /// <summary>Custom payload data associated with the toast.</summary>
    public object? Data
    {
        get => data;
        set
        {
            data = value;
            HasData = true;
        }
    }

    internal bool HasId { get; private set; }

    internal bool HasTitle { get; private set; }

    internal bool HasType { get; private set; }

    internal bool HasDescription { get; private set; }

    internal bool HasTimeout { get; private set; }

    internal bool HasPriority { get; private set; }

    internal bool HasOnClose { get; private set; }

    internal bool HasOnCloseCallback { get; private set; }

    internal bool HasOnRemove { get; private set; }

    internal bool HasOnRemoveCallback { get; private set; }

    internal bool HasActionProps { get; private set; }

    internal bool HasPositionerProps { get; private set; }

    internal bool HasData { get; private set; }

    internal ToastManagerAddOptions CloneWithId(string resolvedId)
    {
        var clone = (ToastManagerAddOptions)MemberwiseClone();
        clone.id = resolvedId;
        clone.HasId = true;
        return clone;
    }
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
    private ElementReference? anchor;
    private Side side = Side.Top;
    private Align align = Align.Center;
    private double sideOffset;
    private double alignOffset;
    private CollisionBoundary collisionBoundary = CollisionBoundary.ClippingAncestors;
    private double collisionPadding = 5;
    private SidePadding? collisionPaddingPerSide;
    private double arrowPadding = 5;
    private bool sticky;
    private bool disableAnchorTracking;
    private PositionMethod positionMethod = PositionMethod.Absolute;
    private CollisionAvoidance? collisionAvoidance;
    private readonly HashSet<string> suppliedProperties = new(StringComparer.Ordinal);

    /// <summary>An element to position the toast against.</summary>
    public ElementReference? Anchor { get => anchor; set => Set(ref anchor, value, nameof(Anchor)); }

    /// <summary>Which side of the anchor element to align the toast against.</summary>
    public Side Side { get => side; set => Set(ref side, value, nameof(Side)); }

    /// <summary>The alignment of the toast relative to the specified side.</summary>
    public Align Align { get => align; set => Set(ref align, value, nameof(Align)); }

    /// <summary>The offset in pixels from the side of the anchor element.</summary>
    public double SideOffset { get => sideOffset; set => Set(ref sideOffset, value, nameof(SideOffset)); }

    /// <summary>The offset in pixels from the alignment edge of the anchor element.</summary>
    public double AlignOffset { get => alignOffset; set => Set(ref alignOffset, value, nameof(AlignOffset)); }

    /// <summary>The collision boundary used to detect repositioning.</summary>
    public CollisionBoundary CollisionBoundary { get => collisionBoundary; set => Set(ref collisionBoundary, value, nameof(CollisionBoundary)); }

    /// <summary>The padding in pixels between the toast and the collision boundary edge.</summary>
    public double CollisionPadding { get => collisionPadding; set => Set(ref collisionPadding, value, nameof(CollisionPadding)); }

    /// <summary>Per-side collision padding.</summary>
    public SidePadding? CollisionPaddingPerSide { get => collisionPaddingPerSide; set => Set(ref collisionPaddingPerSide, value, nameof(CollisionPaddingPerSide)); }

    /// <summary>The minimum padding in pixels between the arrow and the popup edge.</summary>
    public double ArrowPadding { get => arrowPadding; set => Set(ref arrowPadding, value, nameof(ArrowPadding)); }

    /// <summary>Whether the toast stays attached to the anchor while overflowing.</summary>
    public bool Sticky { get => sticky; set => Set(ref sticky, value, nameof(Sticky)); }

    /// <summary>Whether anchor tracking updates are disabled.</summary>
    public bool DisableAnchorTracking { get => disableAnchorTracking; set => Set(ref disableAnchorTracking, value, nameof(DisableAnchorTracking)); }

    /// <summary>The CSS positioning method.</summary>
    public PositionMethod PositionMethod { get => positionMethod; set => Set(ref positionMethod, value, nameof(PositionMethod)); }

    /// <summary>How collisions are handled when positioning the toast.</summary>
    public CollisionAvoidance? CollisionAvoidance { get => collisionAvoidance; set => Set(ref collisionAvoidance, value, nameof(CollisionAvoidance)); }

    internal bool Has(string propertyName) => suppliedProperties.Contains(propertyName);

    private void Set<T>(ref T field, T value, string propertyName)
    {
        field = value;
        suppliedProperties.Add(propertyName);
    }
}
