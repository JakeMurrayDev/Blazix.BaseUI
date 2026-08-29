using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Infrastructure;

/// <summary>
/// Renders <see cref="Content"/> with a flag that flips once <see cref="Swap"/> is called, so a test
/// can replace a keyed child component in place and observe the registration hand-off.
/// </summary>
/// <remarks>
/// Blazor initializes the replacement component before disposing the outgoing one, which is the
/// ordering that makes an unguarded registration clear drop the incoming registration.
/// </remarks>
public sealed class ComponentSwapHost : ComponentBase
{
    private bool swapped;

    /// <summary>
    /// Gets or sets the content to render. The argument is <see langword="true"/> after the swap.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? Content { get; set; }

    /// <summary>
    /// Replaces the rendered content with its post-swap variant.
    /// </summary>
    public void Swap()
    {
        swapped = true;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Content is not null)
        {
            builder.AddContent(0, Content(swapped));
        }
    }
}
