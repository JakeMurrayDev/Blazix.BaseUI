using Blazix.BaseUI.Field;
using Blazix.BaseUI.Form;
using Blazix.BaseUI.Tests.Contracts.Field;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazix.BaseUI.Tests.Field;

public class FieldErrorTests : BunitContext, IFieldErrorContract
{
    public FieldErrorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldWithError(
        bool? invalid = null,
        bool? match = null,
        RenderFragment<RenderProps<FieldErrorState>>? errorRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);

            if (invalid.HasValue)
                builder.AddAttribute(1, "Invalid", invalid.Value);

            builder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldControl<string>>(0);
                fieldBuilder.AddAttribute(1, "data-testid", "field-control");
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldError>(10);
                if (match.HasValue)
                    fieldBuilder.AddAttribute(11, "Match", match.Value);
                fieldBuilder.AddAttribute(12, "data-testid", "field-error");
                fieldBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Error message")));
                if (errorRender is not null)
                    fieldBuilder.AddAttribute(14, "Render", errorRender);
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivWhenInvalid()
    {
        // FieldError.ShouldRenderError checks validityData.State.Valid, not FieldRoot's Invalid param.
        // Use Match=true to force render, and Invalid=true so the field state is invalid.
        var cut = Render(CreateFieldWithError(invalid: true, match: true));
        var error = cut.Find("[data-testid='field-error']");
        error.ShouldNotBeNull();
        error.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldWithError(
            match: true,
            errorRender: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var section = cut.Find("section");
        section.ShouldNotBeNull();
        section.TextContent.ShouldContain("Error message");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByOnControlAutomatically()
    {
        // Use Match=true so the error renders and registers its message id
        var cut = Render(CreateFieldWithError(invalid: true, match: true));
        var control = cut.Find("input[data-testid='field-control']");
        var error = cut.Find("[data-testid='field-error']");

        var ariaDescribedBy = control.GetAttribute("aria-describedby");
        var errorId = error.GetAttribute("id");

        ariaDescribedBy.ShouldNotBeNullOrEmpty();
        errorId.ShouldNotBeNullOrEmpty();
        ariaDescribedBy.ShouldContain(errorId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task MatchTrueAlwaysRendersErrorMessage()
    {
        // Match=true should render even when not invalid
        var cut = Render(CreateFieldWithError(match: true));
        var error = cut.Find("[data-testid='field-error']");
        error.ShouldNotBeNull();
        error.TextContent.ShouldContain("Error message");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SpecificMatchDoesNotRenderForFormErrorOnly()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Form.Form>(0);
            builder.AddAttribute(1, "Errors", new Dictionary<string, string[]>
            {
                ["username"] = ["Username is reserved"]
            });
            builder.AddAttribute(2, "ChildContent", (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(_ =>
                (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<FieldRoot>(0);
                    innerBuilder.AddAttribute(1, "Name", "username");
                    innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
                    {
                        fieldBuilder.OpenComponent<FieldControl<string>>(0);
                        fieldBuilder.CloseComponent();

                        fieldBuilder.OpenComponent<FieldError>(10);
                        fieldBuilder.AddAttribute(11, "MatchValidity", "valueMissing");
                        fieldBuilder.AddAttribute(12, "data-testid", "specific-error");
                        fieldBuilder.CloseComponent();

                        fieldBuilder.OpenComponent<FieldError>(20);
                        fieldBuilder.AddAttribute(21, "data-testid", "default-error");
                        fieldBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                })));
            builder.CloseComponent();
        });

        cut.FindAll("[data-testid='specific-error']").ShouldBeEmpty();
        cut.Find("[data-testid='default-error']").TextContent.ShouldBe("Username is reserved");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersMultipleFormErrorsAsList()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Form.Form>(0);
            builder.AddAttribute(1, "Errors", new Dictionary<string, string[]>
            {
                ["username"] = ["Username is reserved", "Username is too short"]
            });
            builder.AddAttribute(2, "ChildContent", (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(_ =>
                (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<FieldRoot>(0);
                    innerBuilder.AddAttribute(1, "Name", "username");
                    innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
                    {
                        fieldBuilder.OpenComponent<FieldControl<string>>(0);
                        fieldBuilder.CloseComponent();

                        fieldBuilder.OpenComponent<FieldError>(10);
                        fieldBuilder.AddAttribute(11, "data-testid", "field-error");
                        fieldBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                })));
            builder.CloseComponent();
        });

        var items = cut.FindAll("[data-testid='field-error'] li");
        items.Count.ShouldBe(2);
        items[0].TextContent.ShouldBe("Username is reserved");
        items[1].TextContent.ShouldBe("Username is too short");
        return Task.CompletedTask;
    }
}
