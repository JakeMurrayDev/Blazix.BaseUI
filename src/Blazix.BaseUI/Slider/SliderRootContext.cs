using Blazix.BaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Slider;

/// <summary>
/// Provides cascading state and callbacks shared between slider sub-components.
/// </summary>
internal sealed class SliderRootContext
{
    public int ActiveThumbIndex { get; set; } = -1;
    public int LastUsedThumbIndex { get; set; } = -1;
    public ElementReference? ControlElement { get; set; }
    public bool Dragging { get; set; }
    public bool Disabled { get; set; }
    public bool ReadOnly { get; set; }
    public double LargeStep { get; set; } = 10;
    public double Max { get; set; } = 100;
    public double Min { get; set; }
    public double MinStepsBetweenValues { get; set; }
    public string? Form { get; set; }
    public string? Name { get; set; }
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public double Step { get; set; } = 1;
    public ThumbCollisionBehavior ThumbCollisionBehavior { get; set; } = ThumbCollisionBehavior.Push;
    public ThumbAlignment ThumbAlignment { get; set; } = ThumbAlignment.Center;
    public bool Inset => ThumbAlignment != ThumbAlignment.Center;
    public double[] Values { get; set; } = [0];
    public SliderRootState State { get; set; } = SliderRootState.Default;
    public string? LabelId { get; set; }
    public string? RootLabelId { get; set; }
    public Action<string?> SetLabelId { get; set; } = null!;
    public NumberFormatOptions? FormatOptions { get; set; }
    public string? Locale { get; set; }
    public FieldValidation? Validation { get; set; }
    public bool HasRealtimeSubscribers { get; set; }

    public Action<int> SetActiveThumbIndex { get; set; } = null!;
    public Action<bool> SetDragging { get; set; } = null!;
    public Action<double[], SliderChangeReason, int> SetValue { get; set; } = null!;
    public Action<double[]> SetValueSilent { get; set; } = null!;
    public Action<double[], SliderChangeReason> CommitValue { get; set; } = null!;
    public Func<double, int, SliderChangeReason, Task> HandleInputChange { get; set; } = null!;
    public Func<object, int> RegisterImplicitThumbIndex { get; set; } = _ => 0;
    public Action<object> UnregisterImplicitThumbIndex { get; set; } = _ => { };
    public Func<object, int> GetImplicitThumbIndex { get; set; } = _ => 0;
    public Action<int, ThumbMetadata> RegisterThumb { get; set; } = null!;
    public Action<int> UnregisterThumb { get; set; } = null!;
    public Func<int, ThumbMetadata?> GetThumbMetadata { get; set; } = null!;
    public Func<IReadOnlyDictionary<int, ThumbMetadata>> GetAllThumbMetadata { get; set; } = null!;
    public Action<ElementReference> SetControlElement { get; set; } = null!;
    public Action<ElementReference> SetIndicatorElement { get; set; } = null!;
    public Func<ElementReference?> GetIndicatorElement { get; set; } = null!;
    public Action RegisterRealtimeSubscriber { get; set; } = null!;
    public Action UnregisterRealtimeSubscriber { get; set; } = null!;
    public Action<Action> RegisterValueSubscriber { get; set; } = _ => { };
    public Action<Action> UnregisterValueSubscriber { get; set; } = _ => { };
}

/// <summary>
/// Options for formatting slider values using <c>Intl.NumberFormat</c>.
/// </summary>
/// <param name="Style">Gets or sets the formatting style (e.g., <c>"decimal"</c>, <c>"currency"</c>, <c>"percent"</c>).</param>
/// <param name="Currency">Gets or sets the currency code to use when <paramref name="Style"/> is <c>"currency"</c> (e.g., <c>"USD"</c>).</param>
/// <param name="Unit">Gets or sets the unit identifier to use when <paramref name="Style"/> is <c>"unit"</c>.</param>
/// <param name="MinimumFractionDigits">Gets or sets the minimum number of fraction digits to display.</param>
/// <param name="MaximumFractionDigits">Gets or sets the maximum number of fraction digits to display.</param>
/// <param name="MinimumIntegerDigits">Gets or sets the minimum number of integer digits to display.</param>
/// <param name="MinimumSignificantDigits">Gets or sets the minimum number of significant digits to display.</param>
/// <param name="MaximumSignificantDigits">Gets or sets the maximum number of significant digits to display.</param>
/// <param name="UseGrouping">Determines whether to use grouping separators (e.g., thousands separators).</param>
public sealed record NumberFormatOptions(
    string? Style = null,
    string? Currency = null,
    string? Unit = null,
    int? MinimumFractionDigits = null,
    int? MaximumFractionDigits = null,
    int? MinimumIntegerDigits = null,
    int? MinimumSignificantDigits = null,
    int? MaximumSignificantDigits = null,
    bool? UseGrouping = null);
