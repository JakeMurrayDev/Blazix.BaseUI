using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Slider;

/// <summary>
/// Contains metadata for a registered slider thumb, including element references used for focus management and drag operations.
/// </summary>
/// <param name="InputId">Gets the ID of the hidden range input element.</param>
/// <param name="ThumbElement">Gets the <see cref="ElementReference"/> for the thumb container element.</param>
/// <param name="InputElement">Gets the <see cref="ElementReference"/> for the hidden range input element.</param>
internal sealed record ThumbMetadata(
    string InputId,
    ElementReference ThumbElement,
    ElementReference InputElement);
