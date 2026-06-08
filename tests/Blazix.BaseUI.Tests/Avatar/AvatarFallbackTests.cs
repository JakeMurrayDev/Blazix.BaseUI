using Microsoft.Extensions.DependencyInjection;

namespace Blazix.BaseUI.Tests.Avatar;

public class AvatarFallbackTests : BunitContext, IAvatarFallbackContract
{
    private readonly FakeTimeProvider fakeTime = new();

    public AvatarFallbackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<TimeProvider>(fakeTime);
    }

    private RenderFragment CreateFallbackInRoot(
        RenderFragment? childContent = null,
        int? delay = null,
        RenderFragment<RenderProps<AvatarRootState>>? render = null,
        Func<AvatarRootState, string?>? classValue = null,
        Func<AvatarRootState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<AvatarRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AvatarFallback>(0);

                var attrIndex = 1;
                if (childContent is not null)
                {
                    innerBuilder.AddAttribute(attrIndex++, "ChildContent", childContent);
                }
                else
                {
                    innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(fb => fb.AddContent(0, "FB")));
                }

                if (delay.HasValue)
                {
                    innerBuilder.AddAttribute(attrIndex++, "Delay", delay.Value);
                }
                if (render is not null)
                {
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                }
                if (classValue is not null)
                {
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                }
                if (styleValue is not null)
                {
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                }
                if (additionalAttributes is not null)
                {
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                }

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "FB")
        ));

        cut.Markup.ShouldContain("FB");
        var spans = cut.FindAll("span");
        spans.Count.ShouldBeGreaterThanOrEqualTo(2);
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenImageFails()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "AC")
        ));

        cut.Markup.ShouldContain("AC");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenImageLoaded()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(builder =>
        {
            builder.OpenComponent<AvatarRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AvatarImage>(0);
                innerBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                {
                    { "src", "https://example.com/image.jpg" }
                });
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<AvatarFallback>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(fb => fb.AddContent(0, "Fallback")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Markup.ShouldNotContain("Fallback");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render<AvatarFallback>(parameters => parameters
                .Add(p => p.ChildContent, builder => builder.AddContent(0, "FB"))
            );
        });
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotShowBeforeDelayElapsed()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "DelayedContent"),
            delay: 1000
        ));

        cut.Markup.ShouldNotContain("DelayedContent");

        fakeTime.Advance(TimeSpan.FromMilliseconds(500));
        cut.Render();

        cut.Markup.ShouldNotContain("DelayedContent");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ShowsAfterDelayElapsed()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "DelayedContent"),
            delay: 1000
        ));

        cut.Markup.ShouldNotContain("DelayedContent");

        fakeTime.Advance(TimeSpan.FromMilliseconds(1100));
        cut.Render();

        cut.Markup.ShouldContain("DelayedContent");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ShowsImmediatelyWhenNoDelay()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "Immediate")
        ));

        cut.Markup.ShouldContain("Immediate");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ReceivesCorrectState()
    {
        ImageLoadingStatus? capturedStatus = null;

        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "FB"),
            classValue: state =>
            {
                capturedStatus = state.ImageLoadingStatus;
                return "test-class";
            }
        ));

        capturedStatus.ShouldBe(ImageLoadingStatus.Idle);
        return Task.CompletedTask;
    }

    [Fact]
    public void Render_WithCustomRender_RendersCustomElement()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "FB"),
            render: ctx => innerBuilder =>
            {
                innerBuilder.OpenElement(0, "div");
                innerBuilder.AddMultipleAttributes(1, ctx.Attributes);
                innerBuilder.AddContent(2, ctx.ChildContent);
                innerBuilder.CloseElement();
            }
        ));

        var div = cut.Find("div");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void ClassValue_AppliesClass_WhenVisible()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "FB"),
            classValue: _ => "fallback-class"
        ));

        cut.Markup.ShouldContain("fallback-class");
    }

    [Fact]
    public void StyleValue_AppliesStyle_WhenVisible()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "FB"),
            styleValue: _ => "background: blue"
        ));

        cut.Markup.ShouldContain("background: blue");
    }

    [Fact]
    public void AdditionalAttributes_ForwardedToElement()
    {
        var cut = Render(CreateFallbackInRoot(
            childContent: builder => builder.AddContent(0, "FB"),
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "fallback" },
                { "aria-label", "Fallback" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"fallback\"");
        cut.Markup.ShouldContain("aria-label=\"Fallback\"");
    }
}
