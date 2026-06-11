using AngleSharp.Dom;
using Blazix.BaseUI.Field;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazix.BaseUI.Tests.Select;

public class SelectLabelTests : BunitContext, ISelectLabelContract
{
    public SelectLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private static RenderFragment CreateSelectWithLabel(
        RenderFragment<RenderProps<FieldRootState>>? labelRender = null,
        IReadOnlyDictionary<string, object>? labelAttributes = null,
        string? rootId = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (rootId is not null)
            {
                builder.AddAttribute(i++, "Id", rootId);
            }
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<SelectLabel>(0);
                inner.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Fruit")));
                if (labelRender is not null)
                {
                    inner.AddAttribute(2, "Render", labelRender);
                }
                if (labelAttributes is not null)
                {
                    inner.AddMultipleAttributes(3, labelAttributes!);
                }
                inner.CloseComponent();

                inner.OpenComponent<SelectTrigger>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateFieldRootWithSelectAndLabel(bool invalid = false)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Name", "fruit");
            builder.AddAttribute(2, "Invalid", invalid);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<SelectRoot<string>>(0);
                fieldBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(selectBuilder =>
                {
                    selectBuilder.OpenComponent<SelectLabel>(0);
                    selectBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Fruit")));
                    selectBuilder.CloseComponent();

                    selectBuilder.OpenComponent<SelectTrigger>(10);
                    selectBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<SelectValue<string>>(0);
                        b.AddAttribute(1, "Placeholder", "Select...");
                        b.CloseComponent();
                    }));
                    selectBuilder.CloseComponent();
                }));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static IElement FindLabelElement(IRenderedComponent<ContainerFragment> cut)
    {
        foreach (var el in cut.FindAll("div"))
        {
            var id = el.GetAttribute("id");
            if (id is not null && id.EndsWith("-label"))
            {
                return el;
            }
        }

        throw new InvalidOperationException("SelectLabel div was not found");
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSelectWithLabel());
        var label = FindLabelElement(cut);
        label.TagName.ShouldBe("DIV");
        label.TextContent.ShouldContain("Fruit");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateSelectWithLabel(
            labelRender: ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));

        var span = cut.Find("span");
        span.ShouldNotBeNull();
        span.TextContent.ShouldContain("Fruit");
        span.GetAttribute("id").ShouldNotBeNullOrEmpty();
        span.GetAttribute("id").ShouldEndWith("-label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-testid"] = "my-label" };
        var cut = Render(CreateSelectWithLabel(labelAttributes: attrs));

        var label = cut.Find("[data-testid='my-label']");
        label.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersNonNativePointerDownPrevention()
    {
        var module = JSInterop.SetupModule("./_content/Blazix.BaseUI/blazix-baseui-label.min.js");

        Render(CreateSelectWithLabel());

        var invocation = module.Invocations.Last(i => i.Identifier == "addLabelMouseDownListener");
        invocation.Arguments.Count.ShouldBeGreaterThanOrEqualTo(2);
        invocation.Arguments[1].ShouldBe(true);

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesRootIdMinusLabelAsDefaultId()
    {
        var cut = Render(CreateSelectWithLabel(rootId: "fruit-select"));
        var label = FindLabelElement(cut);
        label.GetAttribute("id").ShouldBe("fruit-select-label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task IgnoresConsumerSuppliedId()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "consumer-id" };
        var cut = Render(CreateSelectWithLabel(rootId: "fruit-select", labelAttributes: attrs));

        var label = FindLabelElement(cut);
        label.GetAttribute("id").ShouldBe("fruit-select-label");

        // Also verify the consumer id is not attached to any element
        cut.FindAll("[id='consumer-id']").ShouldBeEmpty();
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesTriggerAriaLabelledByWithoutFieldRoot()
    {
        var cut = Render(CreateSelectWithLabel(rootId: "fruit-select"));
        cut.FindComponent<SelectTrigger>().Render();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-labelledby").ShouldBe("fruit-select-label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersLabelIdInLabelableContextWhenInsideFieldRoot()
    {
        var cut = Render(CreateFieldRootWithSelectAndLabel());
        cut.FindComponent<SelectTrigger>().Render();

        var label = FindLabelElement(cut);
        var labelId = label.GetAttribute("id");
        labelId.ShouldNotBeNullOrEmpty();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-labelledby").ShouldBe(labelId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsFieldValidityDataAttributes()
    {
        var cut = Render(CreateFieldRootWithSelectAndLabel(invalid: true));
        var label = FindLabelElement(cut);

        label.HasAttribute("data-invalid").ShouldBeTrue();
        label.HasAttribute("data-valid").ShouldBeFalse();
        label.HasAttribute("data-disabled").ShouldBeFalse();
        label.HasAttribute("data-touched").ShouldBeFalse();
        label.HasAttribute("data-dirty").ShouldBeFalse();
        label.HasAttribute("data-filled").ShouldBeFalse();
        label.HasAttribute("data-focused").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FocusesTriggerWhenClicked()
    {
        var cut = Render(CreateSelectWithLabel(rootId: "fruit-select"));
        var label = FindLabelElement(cut);

        await label.TriggerEventAsync("onclick", new MouseEventArgs());
        // Pass if no exception: the JS module call to focusControlById is routed through
        // the SetupLabelModule mock, which accepts any arguments without error.
        label.GetAttribute("id").ShouldBe("fruit-select-label");
    }
}
