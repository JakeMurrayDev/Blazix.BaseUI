using Blazix.BaseUI.Field;
using Blazix.BaseUI.Tests.Contracts.Field;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazix.BaseUI.Tests.Field;

public class FieldRootTests : BunitContext, IFieldRootContract
{
    public FieldRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldRoot(
        bool disabled = false,
        bool? invalid = null,
        bool? dirtyState = null,
        bool? touchedState = null,
        Func<object?, Task<string[]?>>? validate = null,
        int validationDebounceTime = 0,
        RenderFragment? childContent = null,
        RenderFragment<RenderProps<FieldRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Disabled", disabled);

            if (invalid.HasValue)
                builder.AddAttribute(2, "Invalid", invalid.Value);
            if (dirtyState.HasValue)
                builder.AddAttribute(3, "DirtyState", dirtyState.Value);
            if (touchedState.HasValue)
                builder.AddAttribute(4, "TouchedState", touchedState.Value);
            if (validate is not null)
                builder.AddAttribute(5, "Validate", validate);
            if (validationDebounceTime > 0)
                builder.AddAttribute(6, "ValidationDebounceTime", validationDebounceTime);
            if (render is not null)
                builder.AddAttribute(8, "Render", render);

            builder.AddAttribute(7, "ChildContent", childContent ?? ((RenderFragment)(b =>
            {
                b.OpenComponent<FieldControl<string>>(0);
                b.AddAttribute(1, "data-testid", "field-control");
                b.CloseComponent();
            })));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateFieldRoot());
        var divs = cut.FindAll("div");
        divs.Count.ShouldBeGreaterThan(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task AddsDataDisabledToAllComponents()
    {
        var cut = Render(CreateFieldRoot(disabled: true));
        var rootDiv = cut.Find("[data-disabled]");
        rootDiv.ShouldNotBeNull();

        var control = cut.Find("[data-testid='field-control']");
        control.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRunValidationByDefaultOutsideForm()
    {
        var validationCalled = false;
        var cut = Render(CreateFieldRoot(validate: async (value) =>
        {
            validationCalled = true;
            return null;
        }));

        validationCalled.ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ValidationDebounceTimeDebounces()
    {
        // Verify that setting a debounce time doesn't cause immediate validation
        var validationCount = 0;
        var cut = Render(CreateFieldRoot(
            validationDebounceTime: 500,
            validate: async (value) =>
            {
                validationCount++;
                return null;
            }));

        // Validation should not have run on init
        validationCount.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultValueNotResetWhenProgrammaticallyChanged()
    {
        // FieldControl uses DefaultValue for uncontrolled mode (no ValueChanged delegate).
        // Setting DefaultValue initializes the internal currentValue.
        var cut = Render(CreateFieldRoot(childContent: builder =>
        {
            builder.OpenComponent<FieldControl<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "initial");
            builder.AddAttribute(2, "data-testid", "field-control");
            builder.CloseComponent();
        }));

        var control = cut.Find("[data-testid='field-control']");
        control.GetAttribute("value").ShouldBe("initial");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultValueNotResetToNonEmptyOnFocus()
    {
        var cut = Render(CreateFieldRoot(childContent: builder =>
        {
            builder.OpenComponent<FieldControl<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "test");
            builder.AddAttribute(2, "data-testid", "field-control");
            builder.CloseComponent();
        }));

        var control = cut.Find("[data-testid='field-control']");
        control.Focus();
        control.GetAttribute("value").ShouldBe("test");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DirtyStateControlsDirtyState()
    {
        var cut = Render(CreateFieldRoot(dirtyState: true));
        var root = cut.Find("[data-dirty]");
        root.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderFalseStateDataAttributes()
    {
        var cut = Render(CreateFieldRoot());
        var root = cut.Find("div");

        root.HasAttribute("data-disabled").ShouldBeFalse();
        root.HasAttribute("data-valid").ShouldBeFalse();
        root.HasAttribute("data-invalid").ShouldBeFalse();
        root.HasAttribute("data-touched").ShouldBeFalse();
        root.HasAttribute("data-dirty").ShouldBeFalse();
        root.HasAttribute("data-filled").ShouldBeFalse();
        root.HasAttribute("data-focused").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task TouchedStateControlsTouchedState()
    {
        var cut = Render(CreateFieldRoot(touchedState: true));
        var root = cut.Find("[data-touched]");
        root.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldRoot(
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
        return Task.CompletedTask;
    }
}
