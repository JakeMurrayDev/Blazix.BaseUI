namespace Blazix.BaseUI.Tests.Meter;

public class MeterLabelTests : BunitContext, IMeterLabelContract
{
    public MeterLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMeterWithLabel(
        double value = 50,
        Func<MeterRootState, string?>? labelClassValue = null,
        Func<MeterRootState, string?>? labelStyleValue = null,
        IReadOnlyDictionary<string, object>? labelAttributes = null,
        RenderFragment<RenderProps<MeterRootState>>? labelRender = null,
        string labelText = "Usage")
    {
        return builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            var attrIndex = 1;

            builder.AddAttribute(attrIndex++, "Value", value);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterLabel>(0);
                var labelAttrIndex = 1;

                if (labelClassValue is not null)
                    innerBuilder.AddAttribute(labelAttrIndex++, "ClassValue", labelClassValue);
                if (labelStyleValue is not null)
                    innerBuilder.AddAttribute(labelAttrIndex++, "StyleValue", labelStyleValue);
                if (labelRender is not null)
                    innerBuilder.AddAttribute(labelAttrIndex++, "Render", labelRender);

                var attrs = new Dictionary<string, object>
                {
                    { "data-testid", "label" }
                };
                if (labelAttributes is not null)
                {
                    foreach (var kvp in labelAttributes)
                        attrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(labelAttrIndex++, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)attrs);

                innerBuilder.AddAttribute(labelAttrIndex++, "ChildContent", (RenderFragment)(contentBuilder =>
                {
                    contentBuilder.AddContent(0, labelText);
                }));

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateMeterWithLabel());
        var label = cut.Find("[data-testid='label']");
        label.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateMeterWithLabel(
            labelRender: ctx => builder =>
            {
                builder.OpenElement(0, "label");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));
        var element = cut.Find("label[data-testid='label']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateMeterWithLabel(
            labelAttributes: new Dictionary<string, object>
            {
                { "data-custom", "label-attr" }
            }
        ));
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("data-custom").ShouldBe("label-attr");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateMeterWithLabel(
            labelClassValue: _ => "label-custom"
        ));
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("class").ShouldContain("label-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMeterWithLabel(
            labelStyleValue: _ => "font-size: 14px"
        ));
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("style").ShouldContain("font-size: 14px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateMeterWithLabel());
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("role").ShouldBe("presentation");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedOutsideRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<MeterLabel>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: MeterRootContext is missing. Meter parts must be placed within <Meter.Root>.");
        return Task.CompletedTask;
    }

    // ID generation

    [Fact]
    public Task GeneratesAutoId()
    {
        var cut = Render(CreateMeterWithLabel());
        var label = cut.Find("[data-testid='label']");
        var id = label.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesProvidedIdFromAdditionalAttributes()
    {
        var cut = Render(CreateMeterWithLabel(
            labelAttributes: new Dictionary<string, object>
            {
                { "id", "my-custom-label-id" }
            }
        ));
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("id").ShouldBe("my-custom-label-id");
        return Task.CompletedTask;
    }

    // Label-root association

    [Fact]
    public Task NotifiesParentOfLabelId()
    {
        var cut = Render(CreateMeterWithLabel());
        var meter = cut.Find("[role='meter']");
        var label = cut.Find("[data-testid='label']");
        var labelId = label.GetAttribute("id");
        meter.GetAttribute("aria-labelledby").ShouldBe(labelId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesParentWhenIdChanges()
    {
        var labelId = "meter-label-one";
        var cut = Render(builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterLabel>(0);
                innerBuilder.AddAttribute(1, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        { "id", labelId },
                        { "data-testid", "label" }
                    });
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
                {
                    b.AddContent(0, "Usage");
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-labelledby").ShouldBe("meter-label-one");

        labelId = "meter-label-two";
        cut.Render();
        cut.FindComponent<MeterRoot>().Render();

        cut.Find("[role='meter']").GetAttribute("aria-labelledby").ShouldBe("meter-label-two");
        cut.Find("[data-testid='label']").GetAttribute("id").ShouldBe("meter-label-two");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CleansUpLabelIdOnDispose()
    {
        var showLabel = true;
        var cut = Render(builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                if (!showLabel) return;
                innerBuilder.OpenComponent<MeterLabel>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.AddContent(0, "Usage");
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var meter = cut.Find("[role='meter']");
        meter.HasAttribute("aria-labelledby").ShouldBeTrue();

        // Remove label from the same tree to trigger MeterLabel.Dispose()
        showLabel = false;
        cut.Render();
        cut.FindComponent<MeterRoot>().Render();

        meter.HasAttribute("aria-labelledby").ShouldBeFalse();
        return Task.CompletedTask;
    }
}
