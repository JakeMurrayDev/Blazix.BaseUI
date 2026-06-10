using Blazix.BaseUI.Fieldset;
using Blazix.BaseUI.Tests.Contracts.Fieldset;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.Fieldset;

public class FieldsetLegendTests : BunitContext, IFieldsetLegendContract
{
    public FieldsetLegendTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateFieldsetWithLegend(
        string? customLegendId = null,
        RenderFragment<RenderProps<FieldsetLegendState>>? legendRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldsetRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldsetBuilder =>
            {
                fieldsetBuilder.OpenComponent<FieldsetLegend>(0);

                if (!string.IsNullOrEmpty(customLegendId))
                    fieldsetBuilder.AddAttribute(1, "id", customLegendId);

                if (legendRender is not null)
                    fieldsetBuilder.AddAttribute(2, "Render", legendRender);

                fieldsetBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Legend text")));
                fieldsetBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateFieldsetWithLegend());
        // FieldsetLegend renders as div by default
        var legendDiv = cut.Find("fieldset div");
        legendDiv.ShouldNotBeNull();
        legendDiv.TagName.ShouldBe("DIV");
        legendDiv.TextContent.ShouldContain("Legend text");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldsetWithLegend(
            legendRender: ctx => builder =>
            {
                builder.OpenElement(0, "h3");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var h3 = cut.Find("fieldset h3");
        h3.ShouldNotBeNull();
        h3.TextContent.ShouldContain("Legend text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByOnFieldsetAutomatically()
    {
        var cut = Render(CreateFieldsetWithLegend());
        var fieldset = cut.Find("fieldset");
        var legend = cut.Find("fieldset div");

        var ariaLabelledBy = fieldset.GetAttribute("aria-labelledby");
        var legendId = legend.GetAttribute("id");

        ariaLabelledBy.ShouldNotBeNullOrEmpty();
        legendId.ShouldNotBeNullOrEmpty();
        ariaLabelledBy.ShouldBe(legendId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByWithCustomId()
    {
        var cut = Render(CreateFieldsetWithLegend(customLegendId: "custom-legend-id"));
        var fieldset = cut.Find("fieldset");

        fieldset.GetAttribute("aria-labelledby").ShouldBe("custom-legend-id");
        return Task.CompletedTask;
    }
}
