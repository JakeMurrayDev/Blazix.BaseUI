namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuLinkTests : BunitContext, INavigationMenuLinkContract
{
    public NavigationMenuLinkTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateLinkInRoot(
        bool active = false,
        string? href = null,
        Func<NavigationMenuLinkState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuLink>(0);
                var attrIndex = 1;
                innerBuilder.AddAttribute(attrIndex++, "Active", active);
                if (href is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Href", href);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Link")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAnchorByDefault()
    {
        var cut = Render(CreateLinkInRoot());

        var link = cut.Find("a");
        link.TagName.ShouldBe("A");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateLinkInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "nav-link" } }
        ));

        var link = cut.Find("a");
        link.GetAttribute("data-testid").ShouldBe("nav-link");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCurrentPageWhenActive()
    {
        var cut = Render(CreateLinkInRoot(active: true));

        var link = cut.Find("a");
        link.GetAttribute("aria-current").ShouldBe("page");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoAriaCurrentWhenInactive()
    {
        var cut = Render(CreateLinkInRoot(active: false));

        var link = cut.Find("a");
        link.HasAttribute("aria-current").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasHref()
    {
        var cut = Render(CreateLinkInRoot(href: "/about"));

        var link = cut.Find("a");
        link.GetAttribute("href").ShouldBe("/about");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateLinkInRoot(
            active: true,
            classValue: state => state.Active ? "active-link" : "link"
        ));

        var link = cut.Find("a");
        link.GetAttribute("class")!.ShouldContain("active-link");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuLink>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Link"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataActiveWhenActive()
    {
        var cut = Render(CreateLinkInRoot(active: true));

        var link = cut.Find("a");
        link.HasAttribute("data-active").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoDataActiveWhenInactive()
    {
        var cut = Render(CreateLinkInRoot(active: false));

        var link = cut.Find("a");
        link.HasAttribute("data-active").ShouldBeFalse();

        return Task.CompletedTask;
    }

    private RenderFragment CreateLinkInFullHierarchy(
        bool closeOnClick,
        Action<NavigationMenuValueChangeEventArgs>? onValueChange = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            var rootAttr = 1;
            builder.AddAttribute(rootAttr++, "DefaultValue", "item1");
            if (onValueChange is not null)
                builder.AddAttribute(rootAttr++, "OnValueChange",
                    EventCallback.Factory.Create(this, onValueChange));
            builder.AddAttribute(rootAttr++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuItem>(0);
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuLink>(0);
                    itemBuilder.AddAttribute(1, "CloseOnClick", closeOnClick);
                    itemBuilder.AddAttribute(2, "Href", "#link");
                    itemBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Link 1")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task CloseOnClickClosesMenu()
    {
        NavigationMenuValueChangeEventArgs? capturedArgs = null;
        var cut = Render(CreateLinkInFullHierarchy(
            closeOnClick: true,
            onValueChange: args => capturedArgs = args));

        var link = cut.Find("a");
        link.Click();

        capturedArgs.ShouldNotBeNull();
        capturedArgs.Value.ShouldBeNull();
        capturedArgs.Reason.ShouldBe(NavigationMenuCloseReason.LinkPress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseOnClickFalseKeepsMenuOpen()
    {
        var cut = Render(CreateLinkInFullHierarchy(closeOnClick: false));

        var link = cut.Find("a");

        // When CloseOnClick is false, no onclick handler is registered,
        // so the link acts as a standard anchor that cannot close the menu.
        link.HasAttribute("blazor:onclick").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task NestedCloseOnClickClosesParentMenu()
    {
        NavigationMenuValueChangeEventArgs? parentArgs = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "DefaultValue", "parent");
            builder.AddAttribute(2, "OnValueChange",
                EventCallback.Factory.Create<NavigationMenuValueChangeEventArgs>(this, args => parentArgs = args));
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(parentBuilder =>
            {
                parentBuilder.OpenComponent<NavigationMenuItem>(0);
                parentBuilder.AddAttribute(1, "Value", "parent");
                parentBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuContent>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(contentBuilder =>
                    {
                        contentBuilder.OpenComponent<NavigationMenuRoot>(0);
                        contentBuilder.AddAttribute(1, "DefaultValue", "nested");
                        contentBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(nestedBuilder =>
                        {
                            nestedBuilder.OpenComponent<NavigationMenuItem>(0);
                            nestedBuilder.AddAttribute(1, "Value", "nested");
                            nestedBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(nestedItemBuilder =>
                            {
                                nestedItemBuilder.OpenComponent<NavigationMenuLink>(0);
                                nestedItemBuilder.AddAttribute(1, "CloseOnClick", true);
                                nestedItemBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Nested link")));
                                nestedItemBuilder.CloseComponent();
                            }));
                            nestedBuilder.CloseComponent();
                        }));
                        contentBuilder.CloseComponent();
                    }));
                    itemBuilder.CloseComponent();
                }));
                parentBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var link = cut.Find("a");
        link.Click();

        parentArgs.ShouldNotBeNull();
        parentArgs.Value.ShouldBeNull();
        parentArgs.Reason.ShouldBe(NavigationMenuCloseReason.LinkPress);

        return Task.CompletedTask;
    }
}
