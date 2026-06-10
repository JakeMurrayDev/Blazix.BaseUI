namespace Blazix.BaseUI.Tests.Avatar;

public class AvatarImageTests : BunitContext, IAvatarImageContract
{
    public AvatarImageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateAvatarRootWrapper(RenderFragment childContent)
    {
        return builder =>
        {
            builder.OpenComponent<AvatarRoot>(0);
            builder.AddAttribute(1, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersWhenLoaded()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.CloseComponent();
        }));

        JSInterop.VerifyInvoke("import");
        var images = cut.FindAll("img");
        images.Count.ShouldBe(1);
        images[0].GetAttribute("src").ShouldBe("https://example.com/image.jpg");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenNotLoaded()
    {
        JsInteropSetup.SetupErrorImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" },
                { "data-testid", "avatar-image" }
            });
            builder.CloseComponent();
        }));

        var images = cut.FindAll("img");
        images.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStatusOnLoad()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/test-image.jpg" }
            });
            builder.CloseComponent();
        }));

        JSInterop.VerifyInvoke("import");
        var images = cut.FindAll("img");
        images.Count.ShouldBe(1);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStatusOnError()
    {
        JsInteropSetup.SetupErrorImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/broken-image.jpg" }
            });
            builder.CloseComponent();
        }));

        var images = cut.FindAll("img");
        images.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokesOnLoadingStatusChange()
    {
        ImageLoadingStatus? capturedStatus = null;

        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.AddAttribute(2, "OnLoadingStatusChange", EventCallback.Factory.Create<ImageLoadingStatus>(
                this,
                status => capturedStatus = status
            ));
            builder.CloseComponent();
        }));

        await Task.Delay(100);
        cut.Render();
        capturedStatus.ShouldBe(ImageLoadingStatus.Loaded);
    }

    [Fact]
    public Task ForwardsAttributes()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" },
                { "alt", "Test Image" },
                { "data-custom", "custom-value" }
            });
            builder.CloseComponent();
        }));

        var image = cut.Find("img");
        image.GetAttribute("alt").ShouldBe("Test Image");
        image.GetAttribute("data-custom").ShouldBe("custom-value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        Should.Throw<InvalidOperationException>(() =>
        {
            Render<AvatarImage>(parameters => parameters
                .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
                {
                    { "src", "https://example.com/image.jpg" }
                })
            );
        });
        return Task.CompletedTask;
    }

    [Fact]
    public void Render_WithCustomRender_RendersCustomElement()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "Render", (RenderFragment<RenderProps<AvatarRootState>>)(ctx => innerBuilder =>
            {
                innerBuilder.OpenElement(0, "picture");
                innerBuilder.AddMultipleAttributes(1, ctx.Attributes);
                innerBuilder.AddContent(2, ctx.ChildContent);
                innerBuilder.CloseElement();
            }));
            builder.AddAttribute(2, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.CloseComponent();
        }));

        var picture = cut.Find("picture");
        picture.ShouldNotBeNull();
    }

    [Fact]
    public void ClassValue_AcceptsStateFunction()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "ClassValue", (Func<AvatarRootState, string?>)(state =>
                state.ImageLoadingStatus == ImageLoadingStatus.Loaded ? "loaded-class" : "not-loaded-class"
            ));
            builder.AddAttribute(2, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.CloseComponent();
        }));

        cut.Markup.ShouldContain("loaded-class");
    }
}
