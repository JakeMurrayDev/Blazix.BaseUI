using Blazix.BaseUI.Field;
using Blazix.BaseUI.Tests.Contracts.Field;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazix.BaseUI.Tests.Field;

public class FieldDescriptionTests : BunitContext, IFieldDescriptionContract
{
    public FieldDescriptionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldWithDescription(
        RenderFragment<RenderProps<FieldRootState>>? descriptionRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldControl<string>>(0);
                fieldBuilder.AddAttribute(1, "data-testid", "field-control");
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldDescription>(10);
                fieldBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Help text")));
                if (descriptionRender is not null)
                    fieldBuilder.AddAttribute(12, "Render", descriptionRender);
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsParagraphByDefault()
    {
        var cut = Render(CreateFieldWithDescription());
        var p = cut.Find("p");
        p.ShouldNotBeNull();
        p.TagName.ShouldBe("P");
        p.TextContent.ShouldContain("Help text");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldWithDescription(
            descriptionRender: ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var span = cut.Find("span");
        span.ShouldNotBeNull();
        span.TextContent.ShouldContain("Help text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByOnControlAutomatically()
    {
        var cut = Render(CreateFieldWithDescription());
        var control = cut.Find("input[data-testid='field-control']");
        var description = cut.Find("p");

        var ariaDescribedBy = control.GetAttribute("aria-describedby");
        var descriptionId = description.GetAttribute("id");

        ariaDescribedBy.ShouldNotBeNullOrEmpty();
        descriptionId.ShouldNotBeNullOrEmpty();
        ariaDescribedBy.ShouldContain(descriptionId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task PreservesExternalAriaDescribedByOnControl()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldControl<string>>(0);
                fieldBuilder.AddAttribute(1, "aria-describedby", "external-description");
                fieldBuilder.AddAttribute(2, "data-testid", "field-control");
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldDescription>(10);
                fieldBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Help text")));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var control = cut.Find("input[data-testid='field-control']");
        var description = cut.Find("p");

        control.GetAttribute("aria-describedby")
            .ShouldBe($"external-description {description.GetAttribute("id")}");
        return Task.CompletedTask;
    }
}
