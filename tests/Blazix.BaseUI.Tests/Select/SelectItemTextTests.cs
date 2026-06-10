namespace Blazix.BaseUI.Tests.Select;

public class SelectItemTextTests : BunitContext, ISelectItemTextContract
{
    private const string SelectModule = "./_content/Blazix.BaseUI/blazix-baseui-select.js";

    public SelectItemTextTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectWithItemText(
        string? defaultValue = null,
        bool defaultOpen = true,
        string? itemTextClass = null,
        string? itemTextContent = "Apple",
        Action<Dictionary<string, object>>? extraItemTextAttrs = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
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
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                        {
                            itemBuilder.OpenComponent<SelectItemText>(0);
                            var idx = 1;
                            if (itemTextClass is not null)
                            {
                                Func<SelectItemTextState, string?> classFn = _ => itemTextClass;
                                itemBuilder.AddAttribute(idx++, "ClassValue", classFn);
                            }
                            if (extraItemTextAttrs is not null)
                            {
                                var dict = new Dictionary<string, object>();
                                extraItemTextAttrs(dict);
                                itemBuilder.AddMultipleAttributes(idx++, dict);
                            }
                            itemBuilder.AddAttribute(idx++, "ChildContent",
                                (RenderFragment)(b => b.AddContent(0, itemTextContent ?? string.Empty)));
                            itemBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(10);
                        popupBuilder.AddAttribute(11, "Value", "banana");
                        popupBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(itemBuilder =>
                        {
                            itemBuilder.OpenComponent<SelectItemText>(0);
                            itemBuilder.AddAttribute(1, "ChildContent",
                                (RenderFragment)(b => b.AddContent(0, "Banana")));
                            itemBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSelectWithItemText());

        // The SelectItemText renders a div wrapping its text content inside the item.
        var items = cut.FindAll("[role='option']");
        var firstItem = items[0];
        var innerDiv = firstItem.QuerySelector("div");
        innerDiv.ShouldNotBeNull();
        innerDiv!.TextContent.ShouldBe("Apple");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateSelectWithItemText(itemTextContent: "Sliced Apple"));

        var items = cut.FindAll("[role='option']");
        items[0].TextContent.ShouldContain("Sliced Apple");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSelectWithItemText(extraItemTextAttrs: d =>
        {
            d["data-testid"] = "item-text-a";
        }));

        var el = cut.Find("[data-testid='item-text-a']");
        el.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSelectWithItemText(itemTextClass: "item-text-style"));

        var items = cut.FindAll("[role='option']");
        var innerDiv = items[0].QuerySelector("div.item-text-style");
        innerDiv.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersSelectedItemTextElementWhenPopupOpens()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(CreateSelectWithItemText(defaultOpen: true));

        // The first item has Index == 0 and, with no value selected, the React-parity
        // fallback branch (`hasNoSelectedItemText && indexRef.current === 0`) fires.
        // SelectItemText therefore pushes its element up to the root, and the root
        // forwards the registration to JS.
        module.VerifyInvoke("setSelectedItemTextElement");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClearsSelectedItemTextElementOnDispose()
    {
        var cut = Render(CreateSelectWithItemText(defaultOpen: true));

        // After initial render, the first SelectItemText has claimed ownership:
        // the root context's selectedItemTextElement is non-null.
        var root = cut.FindComponent<SelectRoot<string>>().Instance;
        root.typedContext.GetSelectedItemTextElement().ShouldNotBeNull();

        // Dispose the owning SelectItemText via its component instance. Its IDisposable
        // Dispose method releases the claim so the next render's Index-0 fallback can
        // re-capture — mirrors React's `isConnected` re-capture guard on the localRef.
        var owner = cut.FindComponents<SelectItemText>()[0].Instance;
        owner.Dispose();

        root.typedContext.GetSelectedItemTextElement().ShouldBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersSelectedByFocusItemTextElement()
    {
        // The Index-0 fallback fires on first render.
        var cut = Render(CreateSelectWithItemText(defaultOpen: true));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        var initialRegistered = root.typedContext.GetSelectedItemTextElement();
        initialRegistered.ShouldNotBeNull();

        // The SelectedByFocus branch ultimately writes to the same root slot —
        // verify the root accepts the new element when a selected-by-focus item
        // claims it (simulating what SelectItemText's OnAfterRender does when
        // ItemContext.SelectedByFocus flips to true on item 1).
        var item1Text = cut.FindComponents<SelectItemText>()[1].Instance;
        item1Text.Element.ShouldNotBeNull();
        root.typedContext.SetSelectedItemTextElement(item1Text.Element);

        var newRegistered = root.typedContext.GetSelectedItemTextElement();
        newRegistered.ShouldNotBeNull();
        newRegistered!.Value.Id.ShouldNotBe(initialRegistered!.Value.Id);
        newRegistered!.Value.Id.ShouldBe(item1Text.Element!.Value.Id);
        return Task.CompletedTask;
    }

    [Fact]
    public Task StaleOwnerDoesNotClearRootOnDispose()
    {
        var cut = Render(CreateSelectWithItemText(defaultOpen: true));

        var root = cut.FindComponent<SelectRoot<string>>().Instance;
        var items = cut.FindComponents<SelectItemText>();
        items.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Initial state: item 0 claimed via Index-0 fallback.
        var item0Element = items[0].Instance.Element;
        var item1Element = items[1].Instance.Element;
        item0Element.ShouldNotBeNull();
        item1Element.ShouldNotBeNull();
        root.typedContext.GetSelectedItemTextElement()!.Value.Id.ShouldBe(item0Element!.Value.Id);

        // Simulate ownership transfer: item 1 claims (as it would on a SelectedByFocus flip).
        root.typedContext.SetSelectedItemTextElement(item1Element);

        // Dispose the FORMER owner (item 0). It must NOT clear the root slot — item 1
        // is the current owner. Parity with React's single-ref-slot semantics.
        items[0].Instance.Dispose();

        var after = root.typedContext.GetSelectedItemTextElement();
        after.ShouldNotBeNull();
        after!.Value.Id.ShouldBe(item1Element!.Value.Id);
        return Task.CompletedTask;
    }
}
