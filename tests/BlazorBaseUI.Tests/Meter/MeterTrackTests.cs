namespace BlazorBaseUI.Tests.Meter;

public class MeterTrackTests : BunitContext, IMeterTrackContract
{
    public MeterTrackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMeterWithTrack(
        double value = 50,
        Func<MeterRootState, string?>? trackClassValue = null,
        Func<MeterRootState, string?>? trackStyleValue = null,
        IReadOnlyDictionary<string, object>? trackAttributes = null,
        RenderFragment<RenderProps<MeterRootState>>? trackRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            var attrIndex = 1;

            builder.AddAttribute(attrIndex++, "Value", value);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterTrack>(0);
                var trackAttrIndex = 1;

                if (trackClassValue is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "ClassValue", trackClassValue);
                if (trackStyleValue is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "StyleValue", trackStyleValue);
                if (trackRender is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "Render", trackRender);

                var attrs = new Dictionary<string, object>
                {
                    { "data-testid", "track" }
                };
                if (trackAttributes is not null)
                {
                    foreach (var kvp in trackAttributes)
                        attrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(trackAttrIndex++, "AdditionalAttributes",
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
        var cut = Render(CreateMeterWithTrack());
        var track = cut.Find("[data-testid='track']");
        track.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateMeterWithTrack(
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
        var cut = Render(CreateMeterWithTrack(
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
        var cut = Render(CreateMeterWithTrack(
            trackClassValue: _ => "track-custom"
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("class").ShouldContain("track-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMeterWithTrack(
            trackStyleValue: _ => "height: 8px"
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("style").ShouldContain("height: 8px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersOutsideRoot()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MeterTrack>(0);
            builder.AddAttribute(1, "data-testid", "track");
            builder.CloseComponent();
        });

        var track = cut.Find("[data-testid='track']");
        track.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }
}
