namespace BlazorBaseUI.Tests.Progress;

public class ProgressLabelTests : BunitContext, IProgressLabelContract
{
    public ProgressLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateProgressWithLabel(
        double? value = 50,
        Func<ProgressRootState, string?>? labelClassValue = null,
        Func<ProgressRootState, string?>? labelStyleValue = null,
        IReadOnlyDictionary<string, object>? labelAttributes = null,
        RenderFragment<RenderProps<ProgressRootState>>? labelRender = null,
        string labelText = "Loading")
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            var attrIndex = 1;

            if (value.HasValue)
                builder.AddAttribute(attrIndex++, "Value", value.Value);
            else
                builder.AddAttribute(attrIndex++, "Value", (double?)null);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressLabel>(0);
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
        var cut = Render(CreateProgressWithLabel());
        var label = cut.Find("[data-testid='label']");
        label.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateProgressWithLabel(
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
        var cut = Render(CreateProgressWithLabel(
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
        var cut = Render(CreateProgressWithLabel(
            labelClassValue: _ => "label-custom"
        ));
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("class").ShouldContain("label-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateProgressWithLabel(
            labelStyleValue: _ => "font-size: 14px"
        ));
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("style").ShouldContain("font-size: 14px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateProgressWithLabel());
        var label = cut.Find("[data-testid='label']");
        label.GetAttribute("role").ShouldBe("presentation");
        return Task.CompletedTask;
    }

    // ID generation

    [Fact]
    public Task GeneratesAutoId()
    {
        var cut = Render(CreateProgressWithLabel());
        var label = cut.Find("[data-testid='label']");
        var id = label.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesProvidedIdFromAdditionalAttributes()
    {
        var cut = Render(CreateProgressWithLabel(
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
        var cut = Render(CreateProgressWithLabel());
        var progressbar = cut.Find("[role='progressbar']");
        var label = cut.Find("[data-testid='label']");
        var labelId = label.GetAttribute("id");
        progressbar.GetAttribute("aria-labelledby").ShouldBe(labelId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesParentWhenIdChanges()
    {
        var labelId = "progress-label-a";
        var cut = Render(builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressLabel>(0);
                innerBuilder.AddAttribute(1, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        { "id", labelId },
                        { "data-testid", "label" }
                    });
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(contentBuilder =>
                {
                    contentBuilder.AddContent(0, "Loading");
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Find("[role='progressbar']").GetAttribute("aria-labelledby").ShouldBe("progress-label-a");

        labelId = "progress-label-b";
        cut.Render();
        cut.FindComponent<ProgressRoot>().Render();

        cut.Find("[role='progressbar']").GetAttribute("aria-labelledby").ShouldBe("progress-label-b");
        cut.Find("[data-testid='label']").GetAttribute("id").ShouldBe("progress-label-b");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RegistersLabelIdWhenContextCallbackChanges()
    {
        var cut = Render<ProgressLabelContextSwitchHost>();

        cut.Instance.FirstRegisteredId.ShouldBe("progress-label");
        cut.Instance.SecondRegisteredId.ShouldBeNull();

        await cut.InvokeAsync(cut.Instance.SwitchContext);

        cut.Instance.SecondRegisteredId.ShouldBe("progress-label");
    }

    [Fact]
    public Task CleansUpLabelIdOnDispose()
    {
        // Render with label present
        var cutWithLabel = Render(builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressLabel>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.AddContent(0, "Loading");
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var progressbar = cutWithLabel.Find("[role='progressbar']");
        progressbar.HasAttribute("aria-labelledby").ShouldBeTrue();

        // Render without label - verifies that a progress root without label has no aria-labelledby
        var cutWithoutLabel = Render(builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                // No label rendered
            }));
            builder.CloseComponent();
        });

        var progressbarNoLabel = cutWithoutLabel.Find("[role='progressbar']");
        progressbarNoLabel.HasAttribute("aria-labelledby").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataStatusAttribute()
    {
        var cut = Render(CreateProgressWithLabel(value: 50));
        var label = cut.Find("[data-testid='label']");
        label.HasAttribute("data-progressing").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedWithoutRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<ProgressLabel>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: ProgressRootContext is missing. Progress parts must be placed within <Progress.Root>.");
        return Task.CompletedTask;
    }
}

internal sealed class ProgressLabelContextSwitchHost : ComponentBase
{
    private bool useSecondContext;

    public string? FirstRegisteredId { get; private set; }

    public string? SecondRegisteredId { get; private set; }

    public void SwitchContext()
    {
        useSecondContext = true;
        StateHasChanged();
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ProgressRootContext>>(0);
        builder.AddAttribute(1, "Value", CreateContext());
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<ProgressLabel>(0);
            innerBuilder.AddAttribute(1, "AdditionalAttributes",
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    { "id", "progress-label" }
                });
            innerBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private ProgressRootContext CreateContext()
    {
        return new ProgressRootContext
        {
            FormattedValue = "50%",
            Max = 100,
            Min = 0,
            Value = 50,
            State = new ProgressRootState(ProgressStatus.Progressing),
            Status = ProgressStatus.Progressing,
            SetLabelIdAction = useSecondContext
                ? id => SecondRegisteredId = id
                : id => FirstRegisteredId = id
        };
    }
}
