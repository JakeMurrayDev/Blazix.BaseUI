namespace BlazorBaseUI.Tests.Menu;

public class MenuLinkItemTests : BunitContext, IMenuLinkItemContract
{
    public MenuLinkItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateMenuWithLinkItem(
        string? label = null,
        string? id = null,
        bool closeOnClick = false,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null,
        Func<MenuLinkItemState, string?>? classValue = null,
        Func<MenuLinkItemState, string?>? styleValue = null,
        RenderFragment<RenderProps<MenuLinkItemState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "Open", (bool?)true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => childBuilder =>
            {
                childBuilder.OpenComponent<MenuPositioner>(0);
                childBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuLinkItem>(0);
                        var attrIndex = 1;
                        if (label is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Label", label);
                        if (id is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Id", id);
                        if (closeOnClick)
                            popupBuilder.AddAttribute(attrIndex++, "CloseOnClick", true);
                        if (additionalAttributes is not null)
                            popupBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                        if (classValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                        if (styleValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                        if (render is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Render", render);
                        popupBuilder.AddAttribute(attrIndex++, "ChildContent",
                            childContent ?? (RenderFragment)(b => b.AddContent(0, "Link Item")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsAnchorElement()
    {
        var cut = Render(CreateMenuWithLinkItem(additionalAttributes: new Dictionary<string, object> { { "href", "/test" } }));
        var element = cut.Find("a[role='menuitem']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersHrefViaAdditionalAttributes()
    {
        var cut = Render(CreateMenuWithLinkItem(additionalAttributes: new Dictionary<string, object> { { "href", "/test-url" } }));
        var element = cut.Find("a[role='menuitem']");
        element.GetAttribute("href")!.ShouldBe("/test-url");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersRoleMenuitem()
    {
        var cut = Render(CreateMenuWithLinkItem());
        var element = cut.Find("[role='menuitem']");
        element.TagName.ShouldBe("A", StringCompareShould.IgnoreCase);
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateMenuWithLinkItem());
        var element = cut.Find("[role='menuitem']");
        element.TextContent.ShouldContain("Link Item");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DataHighlightedFalseByDefault()
    {
        var cut = Render(CreateMenuWithLinkItem());
        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("tabindex")!.ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersLabelAsDataAttribute()
    {
        var cut = Render(CreateMenuWithLinkItem(label: "My Label"));
        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("data-label")!.ShouldBe("My Label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersIdAttribute()
    {
        var cut = Render(CreateMenuWithLinkItem(id: "link-1"));
        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("id")!.ShouldBe("link-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ResolvesIdFromAdditionalAttributes()
    {
        var cut = Render(CreateMenuWithLinkItem(
            additionalAttributes: new Dictionary<string, object> { ["id"] = "link-from-attrs" }));

        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("id")!.ShouldBe("link-from-attrs");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseOnClickDefaultsFalse()
    {
        // CloseOnClick defaults to false - clicking should not close the menu
        var cut = Render(CreateMenuWithLinkItem());
        var element = cut.Find("[role='menuitem']");
        element.Click();
        // Menu should still be open (popup still rendered)
        cut.Find("[role='menu']").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HighlightsOnMouseEnter()
    {
        var cut = Render(CreateMenuWithLinkItem());
        var element = cut.Find("[role='menuitem']");
        element.MouseEnter();
        element.GetAttribute("tabindex")!.ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task UnhighlightsOnMouseLeave()
    {
        var cut = Render(CreateMenuWithLinkItem());
        var element = cut.Find("[role='menuitem']");
        element.MouseEnter();
        element.GetAttribute("tabindex")!.ShouldBe("0");
        element.MouseLeave();
        element = cut.Find("[role='menuitem']");
        element.GetAttribute("tabindex")!.ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseOnClickTrueClosesMenu()
    {
        var closeEmitted = false;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "OnOpenChange", EventCallback.Factory.Create<MenuOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open && args.Reason == MenuOpenChangeReason.ItemPress)
                {
                    closeEmitted = true;
                }
            }));
            builder.AddAttribute(3, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuPositioner>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuLinkItem>(0);
                        popupBuilder.AddAttribute(1, "CloseOnClick", true);
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close Link")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var element = cut.Find("[role='menuitem']");
        element.Click();
        closeEmitted.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuLinkItemState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateMenuWithLinkItem(render: renderAsSpan));
        var element = cut.Find("span[role='menuitem']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateMenuWithLinkItem(
            additionalAttributes: new Dictionary<string, object> { { "data-custom", "test-value" } }));
        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("data-custom")!.ShouldBe("test-value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateMenuWithLinkItem(classValue: _ => "link-class"));
        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("class")!.ShouldContain("link-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMenuWithLinkItem(styleValue: _ => "color: red"));
        var element = cut.Find("[role='menuitem']");
        element.GetAttribute("style")!.ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesState()
    {
        MenuLinkItemState? capturedState = null;
        var cut = Render(CreateMenuWithLinkItem(classValue: state =>
        {
            capturedState = state;
            return "test";
        }));
        capturedState.ShouldNotBeNull();
        capturedState!.Value.Highlighted.ShouldBeFalse();
        return Task.CompletedTask;
    }
}
