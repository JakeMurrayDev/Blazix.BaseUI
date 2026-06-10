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

public class FieldControlTests : BunitContext, IFieldControlContract
{
    public FieldControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldControl(
        RenderFragment<RenderProps<FieldRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldControl<string>>(0);
                fieldBuilder.AddAttribute(1, "data-testid", "field-control");
                if (render is not null)
                    fieldBuilder.AddAttribute(2, "Render", render);
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsInputByDefault()
    {
        var cut = Render(CreateFieldControl());
        var input = cut.Find("input[data-testid='field-control']");
        input.ShouldNotBeNull();
        input.TagName.ShouldBe("INPUT");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldControl(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "textarea");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.CloseElement();
            }
        ));

        var textarea = cut.Find("textarea");
        textarea.ShouldNotBeNull();
        textarea.HasAttribute("id").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DisposeAsyncDisposesImportedModuleWhenElementReferenceIsUnavailable()
    {
        var control = new FieldControl<string>();
        var module = new TrackingJsObjectReference();
        var moduleTask = new Lazy<Task<IJSObjectReference>>(() => Task.FromResult<IJSObjectReference>(module));
        await moduleTask.Value;

        SetPrivateField(control, "moduleTask", moduleTask);

        await control.DisposeAsync();

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
