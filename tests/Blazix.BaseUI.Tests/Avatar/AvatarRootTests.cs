namespace Blazix.BaseUI.Tests.Avatar;

public class AvatarRootTests : BunitContext, IAvatarRootContract
{
    public AvatarRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        var element = cut.Find("span");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.Render, ctx => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        var element = cut.Find("div");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.ClassValue, _ => "test-class")
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        var element = cut.Find("span");
        element.ClassList.ShouldContain("test-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.StyleValue, _ => "color: red")
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        var element = cut.Find("span");
        element.GetAttribute("style")!.ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
            {
                { "data-custom", "custom-value" },
                { "aria-label", "Avatar" }
            })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        var element = cut.Find("span");
        element.GetAttribute("data-custom").ShouldBe("custom-value");
        element.GetAttribute("aria-label").ShouldBe("Avatar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        AvatarRootContext? capturedContext = null;

        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ContextCapture>(0);
                builder.AddAttribute(1, "OnContextCaptured", (Action<AvatarRootContext>)(ctx => capturedContext = ctx));
                builder.CloseComponent();
            }))
        );

        capturedContext.ShouldNotBeNull();
        capturedContext.ImageLoadingStatus.ShouldBe(ImageLoadingStatus.Idle);
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render<AvatarRoot>(parameters => parameters
            .Add(p => p.ClassValue, _ => "class-from-value")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
            {
                { "class", "class-from-attributes" }
            })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        var element = cut.Find("span");
        element.ClassList.ShouldContain("class-from-value");
        element.ClassList.ShouldContain("class-from-attributes");
        return Task.CompletedTask;
    }

    private class ContextCapture : ComponentBase
    {
        [CascadingParameter]
        private AvatarRootContext? Context { get; set; }

        [Parameter]
        public Action<AvatarRootContext>? OnContextCaptured { get; set; }

        protected override void OnParametersSet()
        {
            if (Context is not null)
            {
                OnContextCaptured?.Invoke(Context);
            }
        }
    }
}
