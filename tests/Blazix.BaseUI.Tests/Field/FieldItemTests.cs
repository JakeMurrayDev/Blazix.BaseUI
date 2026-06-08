using Blazix.BaseUI.Field;
using Blazix.BaseUI.Tests.Contracts.Field;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazix.BaseUI.Tests.Field;

public class FieldItemTests : BunitContext, IFieldItemContract
{
    public FieldItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldItem(
        RenderFragment<RenderProps<FieldRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldItem>(0);
                fieldBuilder.AddAttribute(1, "data-testid", "field-item");
                fieldBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item content")));
                if (render is not null)
                    fieldBuilder.AddAttribute(3, "Render", render);
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateFieldItem());
        var item = cut.Find("[data-testid='field-item']");
        item.ShouldNotBeNull();
        item.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldItem(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var section = cut.Find("section");
        section.ShouldNotBeNull();
        section.TextContent.ShouldContain("Item content");
        return Task.CompletedTask;
    }
}
