using Blazix.BaseUI.Csp;

namespace Blazix.BaseUI.Tests.Select;

public class SelectListTests : BunitContext, ISelectListContract
{
    private const string SelectModule = "./_content/Blazix.BaseUI/blazix-baseui-select.min.js";

    public SelectListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private static RenderFragment CreateSelectWithList(
        bool defaultOpen = true,
        bool multiple = false,
        string? userClass = null,
        string? userStyle = null,
        Func<bool>? renderList = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "Multiple", multiple);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        if (renderList?.Invoke() != false)
                        {
                            popupBuilder.OpenComponent<SelectList>(0);
                            if (userClass is not null)
                            {
                                popupBuilder.AddAttribute(1, "class", userClass);
                            }
                            if (userStyle is not null)
                            {
                                popupBuilder.AddAttribute(2, "style", userStyle);
                            }
                            popupBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(listBuilder =>
                            {
                                listBuilder.OpenComponent<SelectItem<string>>(0);
                                listBuilder.AddAttribute(1, "Value", "apple");
                                listBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                                listBuilder.CloseComponent();
                            }));
                            popupBuilder.CloseComponent();
                        }
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static AngleSharp.Dom.IElement FindList(IRenderedComponent<ContainerFragment> cut)
    {
        return cut.FindComponent<SelectList>().Find("[role='listbox']");
    }

    private static RenderFragment WrapInCspProvider(RenderFragment child, string? nonce = null, bool disableStyleElements = false)
    {
        return builder =>
        {
            builder.OpenComponent<CspProvider>(0);
            if (nonce is not null)
            {
                builder.AddAttribute(1, "Nonce", nonce);
            }
            builder.AddAttribute(2, "DisableStyleElements", disableStyleElements);
            builder.AddAttribute(3, "ChildContent", child);
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSelectWithList());

        var list = FindList(cut);
        list.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesRootIdDashListAsListId()
    {
        var cut = Render(CreateSelectWithList());

        var list = FindList(cut);
        var id = list.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        id!.ShouldEndWith("-list");
        return Task.CompletedTask;
    }

    [Fact]
    public Task WritesListIdToRootContext()
    {
        var showList = true;
        var cut = Render(CreateSelectWithList(renderList: () => showList));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.ListId.ShouldNotBeNullOrEmpty();
        root.typedContext.ListId!.ShouldEndWith("-list");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsTabIndex()
    {
        var cut = Render(CreateSelectWithList());

        var list = FindList(cut);
        list.HasAttribute("tabindex").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesRoleListbox()
    {
        var cut = Render(CreateSelectWithList());

        var list = FindList(cut);
        list.GetAttribute("role").ShouldBe("listbox");
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsAriaMultiselectableWhenMultiple()
    {
        var cut = Render(CreateSelectWithList(multiple: true));

        var list = FindList(cut);
        list.GetAttribute("aria-multiselectable").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsAriaMultiselectableWhenSingleSelect()
    {
        var cut = Render(CreateSelectWithList(multiple: false));

        var list = FindList(cut);
        list.HasAttribute("aria-multiselectable").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesFunctionalStylesWhenAlignItemWithTriggerActive()
    {
        var cut = Render(CreateSelectWithList());
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.AlignItemWithTriggerActive = true;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var style = list.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("position:relative");
        style.ShouldContain("max-height:100%");
        style.ShouldContain("overflow-x:hidden");
        style.ShouldContain("overflow-y:auto");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsFunctionalStylesWhenInactive()
    {
        var cut = Render(CreateSelectWithList());
        var root = cut.FindComponent<SelectRoot<string>>().Instance;
        root.typedContext.AlignItemWithTriggerActive = false;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var style = list.GetAttribute("style") ?? string.Empty;
        style.ShouldNotContain("overflow-y:auto");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesDisableScrollbarClassWhenScrollArrowsActiveAndNotTouch()
    {
        var cut = Render(CreateSelectWithList());
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.OpenInteractionType = InteractionType.Click;
        root.typedContext.ScrollArrowsMountedCount = 1;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var className = list.GetAttribute("class") ?? string.Empty;
        className.ShouldContain("blazix-base-ui-disable-scrollbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsDisableScrollbarClassWhenOpenMethodIsTouch()
    {
        var cut = Render(CreateSelectWithList());
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.OpenInteractionType = InteractionType.Touch;
        root.typedContext.ScrollArrowsMountedCount = 1;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var className = list.GetAttribute("class") ?? string.Empty;
        className.ShouldNotContain("blazix-base-ui-disable-scrollbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsDisableScrollbarClassWhenNoScrollArrows()
    {
        var cut = Render(CreateSelectWithList());
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.OpenInteractionType = InteractionType.Click;
        root.typedContext.ScrollArrowsMountedCount = 0;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var className = list.GetAttribute("class") ?? string.Empty;
        className.ShouldNotContain("blazix-base-ui-disable-scrollbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersListElementOnFirstRender()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(CreateSelectWithList());

        // SelectRoot's private SetListElement forwards the ref to JS via
        // module.setListElement(rootId, element) when hasRendered && HasValue.
        // Verifying the JS invocation confirms both SelectList registered the
        // element via the context delegate and the root forwarded it.
        module.VerifyInvoke("setListElement");
        return Task.CompletedTask;
    }

    [Fact]
    public Task InjectsScrollbarDisableStyleOnFirstRender()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(CreateSelectWithList());

        module.VerifyInvoke("injectScrollbarDisableStyle");
        return Task.CompletedTask;
    }

    [Fact]
    public Task InjectsScrollbarDisableStyleWithCspNonce()
    {
        var module = JSInterop.SetupModule(SelectModule);

        Render(WrapInCspProvider(CreateSelectWithList(), nonce: "select-nonce"));

        module.Invocations
            .Any(i => i.Identifier == "injectScrollbarDisableStyle" &&
                      i.Arguments.Count > 0 &&
                      Equals(i.Arguments[0], "select-nonce"))
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotInjectScrollbarDisableStyleWhenCspDisablesStyleElements()
    {
        var module = JSInterop.SetupModule(SelectModule);

        Render(WrapInCspProvider(CreateSelectWithList(), disableStyleElements: true));

        module.Invocations.Any(i => i.Identifier == "injectScrollbarDisableStyle").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClearsListElementOnDispose()
    {
        var showList = true;
        var cut = Render(CreateSelectWithList(renderList: () => showList));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        // Before dispose: SelectList registered ListId on the root context.
        root.typedContext.HasList.ShouldBeTrue();
        root.typedContext.ListId.ShouldNotBeNullOrEmpty();

        showList = false;
        cut.Render();

        // After dispose: ListId cleared so HasList reports false, matching
        // React's ref-detach semantics (`setListElement(null)`).
        cut.WaitForAssertion(() =>
        {
            root.typedContext.HasList.ShouldBeFalse();
            root.typedContext.ListId.ShouldBeNull();
        });
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectList>(0);
                        popupBuilder.AddAttribute(1, "data-testid", "list");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, string.Empty)));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var list = FindList(cut);
        list.GetAttribute("data-testid").ShouldBe("list");
        return Task.CompletedTask;
    }

    [Fact]
    public Task MergesUserClassWithDisableScrollbarClass()
    {
        var cut = Render(CreateSelectWithList(userClass: "my-list"));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;
        root.typedContext.OpenInteractionType = InteractionType.Click;
        root.typedContext.ScrollArrowsMountedCount = 1;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var className = list.GetAttribute("class") ?? string.Empty;
        className.ShouldContain("my-list");
        className.ShouldContain("blazix-base-ui-disable-scrollbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task MergesUserStyleWithFunctionalStyles()
    {
        var cut = Render(CreateSelectWithList(userStyle: "color:red;"));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;
        root.typedContext.AlignItemWithTriggerActive = true;
        root.typedContext.NotifyStateChanged();
        cut.FindComponent<SelectList>().Render();

        var list = FindList(cut);
        var style = list.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("color:red");
        style.ShouldContain("overflow-y:auto");
        return Task.CompletedTask;
    }
}
