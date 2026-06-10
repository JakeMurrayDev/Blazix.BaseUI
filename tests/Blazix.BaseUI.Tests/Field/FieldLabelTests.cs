using Blazix.BaseUI.Field;
using Blazix.BaseUI.Tests.Contracts.Field;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Blazix.BaseUI.Tests.Field;

public class FieldLabelTests : BunitContext, IFieldLabelContract
{
    public FieldLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldWithLabel(
        RenderFragment<RenderProps<FieldRootState>>? labelRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldLabel>(0);
                fieldBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Field Label")));
                if (labelRender is not null)
                    fieldBuilder.AddAttribute(2, "Render", labelRender);
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldControl<string>>(10);
                fieldBuilder.AddAttribute(11, "data-testid", "field-control");
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsLabelByDefault()
    {
        var cut = Render(CreateFieldWithLabel());
        var label = cut.Find("label");
        label.ShouldNotBeNull();
        label.TagName.ShouldBe("LABEL");
        label.TextContent.ShouldContain("Field Label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldWithLabel(
            labelRender: ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var span = cut.Find("span");
        span.ShouldNotBeNull();
        span.TextContent.ShouldContain("Field Label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsHtmlForReferencingControlAutomatically()
    {
        var cut = Render(CreateFieldWithLabel());
        var label = cut.Find("label");
        var control = cut.Find("input[data-testid='field-control']");

        var forValue = label.GetAttribute("for");
        var controlId = control.GetAttribute("id");

        forValue.ShouldNotBeNullOrEmpty();
        controlId.ShouldNotBeNullOrEmpty();
        forValue.ShouldBe(controlId);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DisposeAsyncDisposesImportedModuleWhenElementReferenceIsUnavailable()
    {
        var label = new FieldLabel();
        var module = new TrackingJsObjectReference();
        var moduleTask = new Lazy<Task<IJSObjectReference>>(() => Task.FromResult<IJSObjectReference>(module));
        await moduleTask.Value;

        SetPrivateField(label, "moduleTask", moduleTask);

        await label.DisposeAsync();

        module.Disposed.ShouldBeTrue();
    }

    private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
    {
        var field = instance.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        field.ShouldNotBeNull();
        field.SetValue(instance, value);
    }
}
