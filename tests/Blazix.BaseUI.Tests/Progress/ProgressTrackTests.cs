namespace Blazix.BaseUI.Tests.Progress;

public class ProgressTrackTests : BunitContext, IProgressTrackContract
{
    public ProgressTrackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateProgressWithTrack(
        double? value = 50,
        Func<ProgressRootState, string?>? trackClassValue = null,
        Func<ProgressRootState, string?>? trackStyleValue = null,
        IReadOnlyDictionary<string, object>? trackAttributes = null,
        RenderFragment<RenderProps<ProgressRootState>>? trackRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);

            if (value.HasValue)
                builder.AddAttribute(1, "Value", value.Value);
            else
                builder.AddAttribute(2, "Value", (double?)null);

            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressTrack>(0);

                if (trackClassValue is not null)
                    innerBuilder.AddAttribute(1, "ClassValue", trackClassValue);
                if (trackStyleValue is not null)
                    innerBuilder.AddAttribute(2, "StyleValue", trackStyleValue);
                if (trackRender is not null)
                    innerBuilder.AddAttribute(3, "Render", trackRender);

                var attrs = new Dictionary<string, object>
                {
                    { "data-testid", "track" }
                };
                if (trackAttributes is not null)
                {
                    foreach (var kvp in trackAttributes)
                        attrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(4, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)attrs);

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateProgressWithTrack());
        var track = cut.Find("[data-testid='track']");
        track.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateProgressWithTrack(
            trackRender: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));
        var element = cut.Find("section[data-testid='track']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateProgressWithTrack(
            trackAttributes: new Dictionary<string, object>
            {
                { "aria-label", "track background" }
            }
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("aria-label").ShouldBe("track background");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateProgressWithTrack(
            trackClassValue: _ => "track-custom"
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("class").ShouldContain("track-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateProgressWithTrack(
            trackStyleValue: _ => "height: 8px"
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("style").ShouldContain("height: 8px");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataStatusAttribute()
    {
        var cut = Render(CreateProgressWithTrack(value: 50));
        var track = cut.Find("[data-testid='track']");
        track.HasAttribute("data-progressing").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedWithoutRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<ProgressTrack>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: ProgressRootContext is missing. Progress parts must be placed within <Progress.Root>.");
        return Task.CompletedTask;
    }
}
