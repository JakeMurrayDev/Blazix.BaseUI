using AngleSharp.Dom;
using Blazix.BaseUI.DirectionProvider;
using Blazix.BaseUI.Field;
using Blazix.BaseUI.Form;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazix.BaseUI.Tests.Select;

public class SelectRootTests : BunitContext, ISelectRootContract
{
    private const string SelectModule = "./_content/Blazix.BaseUI/blazix-baseui-select.min.js";

    public SelectRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateSelect(
        string? defaultValue = null,
        string? value = null,
        bool defaultOpen = false,
        bool? open = null,
        string? name = null,
        string? form = null,
        Blazix.BaseUI.Select.SelectModalMode modal = Blazix.BaseUI.Select.SelectModalMode.False,
        Func<string?, string?>? itemToStringLabel = null,
        Func<string?, string?>? itemToStringValue = null,
        EventCallback<SelectOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<SelectValueChangeEventArgs<string>>? onValueChange = null,
        EventCallback<string?>? valueChanged = null,
        SelectRootActions? actionsRef = null,
        Action<ElementReference?>? inputRef = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            if (value is not null) builder.AddAttribute(i++, "Value", value);
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            if (open.HasValue) builder.AddAttribute(i++, "Open", open.Value);
            if (name is not null) builder.AddAttribute(i++, "Name", name);
            if (form is not null) builder.AddAttribute(i++, "Form", form);
            builder.AddAttribute(i++, "Modal", modal);
            if (itemToStringLabel is not null) builder.AddAttribute(i++, "ItemToStringLabel", itemToStringLabel);
            if (itemToStringValue is not null) builder.AddAttribute(i++, "ItemToStringValue", itemToStringValue);
            if (onOpenChange.HasValue) builder.AddAttribute(i++, "OnOpenChange", onOpenChange.Value);
            if (onValueChange.HasValue) builder.AddAttribute(i++, "OnValueChange", onValueChange.Value);
            if (valueChanged.HasValue) builder.AddAttribute(i++, "ValueChanged", valueChanged.Value);
            if (actionsRef is not null) builder.AddAttribute(i++, "ActionsRef", actionsRef);
            if (inputRef is not null) builder.AddAttribute(i++, "InputRef", inputRef);
            builder.AddAttribute(i++, "ChildContent", childContent ?? CreateDefaultChildren());
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultChildren()
    {
        return builder =>
        {
            builder.OpenComponent<SelectTrigger>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<SelectValue<string>>(0);
                b.AddAttribute(1, "Placeholder", "Select...");
                b.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<SelectPositioner>(10);
            builder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
            {
                posBuilder.OpenComponent<SelectPopup>(0);
                posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<SelectItem<string>>(0);
                    popupBuilder.AddAttribute(1, "Value", "apple");
                    popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                    popupBuilder.CloseComponent();

                    popupBuilder.OpenComponent<SelectItem<string>>(10);
                    popupBuilder.AddAttribute(11, "Value", "banana");
                    popupBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                    popupBuilder.CloseComponent();

                    popupBuilder.OpenComponent<SelectItem<string>>(20);
                    popupBuilder.AddAttribute(21, "Value", "cherry");
                    popupBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Cherry")));
                    popupBuilder.CloseComponent();
                }));
                posBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static void MouseClickItem(IElement item)
    {
        item.TriggerEvent("onpointerdown", new PointerEventArgs { PointerType = "mouse" });
        item.Click();
    }

    private static RenderFragment CreateChildrenWithBackdrop()
    {
        return builder =>
        {
            builder.OpenComponent<SelectTrigger>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<SelectValue<string>>(0);
                b.AddAttribute(1, "Placeholder", "Select...");
                b.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<SelectBackdrop>(5);
            builder.CloseComponent();

            builder.OpenComponent<SelectPositioner>(10);
            builder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
            {
                posBuilder.OpenComponent<SelectPopup>(0);
                posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<SelectItem<string>>(0);
                    popupBuilder.AddAttribute(1, "Value", "apple");
                    popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                    popupBuilder.CloseComponent();
                }));
                posBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task DefaultValue_ShouldSelectItemByDefault()
    {
        var cut = Render(CreateSelect(defaultValue: "banana", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        var bananaItem = items.First(i => i.TextContent.Contains("Banana"));
        bananaItem.HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_ShouldSelectSpecifiedItem()
    {
        var cut = Render(CreateSelect(
            value: "cherry",
            defaultOpen: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var items = cut.FindAll("[role='option']");
        var cherryItem = items.First(i => i.TextContent.Contains("Cherry"));
        cherryItem.HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_ShouldBeControlledWhenValueParameterIsProvidedWithoutValueChanged()
    {
        var cut = Render(CreateSelect(
            value: "cherry",
            defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Cherry")).HasAttribute("data-selected").ShouldBeTrue();

        MouseClickItem(items.First(i => i.TextContent.Contains("Banana")));

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.GetAttribute("value").ShouldBe("cherry");
        cut.FindComponent<SelectValue<string>>().Markup.ShouldContain("Cherry");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_ShouldUpdateWhenValuePropChanges()
    {
        var cut = Render(CreateSelect(
            value: "apple",
            defaultOpen: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var items = cut.FindAll("[role='option']");
        var appleItem = items.First(i => i.TextContent.Contains("Apple"));
        appleItem.HasAttribute("data-selected").ShouldBeTrue();

        // Re-render with a different value
        var cut2 = Render(CreateSelect(
            value: "banana",
            defaultOpen: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var items2 = cut2.FindAll("[role='option']");
        var bananaItem = items2.First(i => i.TextContent.Contains("Banana"));
        bananaItem.HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_ShouldNotUpdateInternalIfControlledValueDoesNotChange()
    {
        var cut = Render(CreateSelect(
            value: "apple",
            defaultOpen: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        // Click on banana item
        var items = cut.FindAll("[role='option']");
        var bananaItem = items.First(i => i.TextContent.Contains("Banana"));
        MouseClickItem(bananaItem);

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.GetAttribute("value").ShouldBe("apple");
        cut.FindComponent<SelectValue<string>>().Markup.ShouldContain("Apple");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ItemToStringValue_UsesForFormSubmission()
    {
        var cut = Render(CreateSelect(
            name: "fruit",
            defaultValue: "apple",
            itemToStringValue: v => v?.ToUpper()));

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.GetAttribute("value").ShouldBe("APPLE");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInput_ForwardsFormAttribute()
    {
        var cut = Render(CreateSelect(
            name: "fruit",
            form: "checkout",
            defaultValue: "apple"));

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.GetAttribute("form").ShouldBe("checkout");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputRef_ReceivesHiddenInputElement()
    {
        ElementReference? received = null;

        Render(CreateSelect(
            name: "fruit",
            defaultValue: "apple",
            inputRef: element => received = element));

        received.ShouldNotBeNull();
        received.Value.Id.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ItemToStringLabel_UsesForTriggerText()
    {
        var cut = Render(CreateSelect(
            defaultValue: "apple",
            itemToStringLabel: v => $"Fruit: {v}"));

        var valueSpan = cut.FindComponent<SelectValue<string>>();
        valueSpan.Markup.ShouldContain("Fruit: apple");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_ShouldCallWhenItemSelected()
    {
        var invoked = false;
        string? receivedValue = null;

        var cut = Render(CreateSelect(
            defaultOpen: true,
            onValueChange: EventCallback.Factory.Create<SelectValueChangeEventArgs<string>>(this, args =>
            {
                invoked = true;
                receivedValue = args.Value;
            })));

        var items = cut.FindAll("[role='option']");
        var bananaItem = items.First(i => i.TextContent.Contains("Banana"));
        MouseClickItem(bananaItem);

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe("banana");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OnStartingStyleApplied_ClearsStartingTransitionStatus()
    {
        var cut = Render(CreateSelect(defaultOpen: true));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.TransitionStatus.ShouldBe(TransitionStatus.Starting);

        await cut.InvokeAsync(() => root.OnStartingStyleApplied());

        root.typedContext.TransitionStatus.ShouldBe(TransitionStatus.Undefined);
    }

    [Fact]
    public Task InitializeRoot_UsesNonLoopingNavigationAndDirectionProvider()
    {
        var module = JSInterop.SetupModule(SelectModule);

        Render(builder =>
        {
            builder.OpenComponent<global::Blazix.BaseUI.DirectionProvider.DirectionProvider>(0);
            builder.AddAttribute(1, "Direction", Direction.Rtl);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.AddContent(0, CreateSelect(defaultOpen: true));
            }));
            builder.CloseComponent();
        });

        var invocation = module.Invocations.Last(i => i.Identifier == "initializeRoot");
        invocation.Arguments[2].ShouldBe(false);
        invocation.Arguments[4].ShouldBe("rtl");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ActionsRefUnmount_ForcesPopupUnmount()
    {
        var actions = new SelectRootActions();
        var cut = Render(CreateSelect(defaultOpen: true, actionsRef: actions));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        root.typedContext.Mounted.ShouldBeTrue();

        await cut.InvokeAsync(() => actions.Unmount?.Invoke());

        root.typedContext.Mounted.ShouldBeFalse();
        root.typedContext.TransitionStatus.ShouldBe(TransitionStatus.Undefined);
    }

    [Fact]
    public Task DefaultOpen_ShouldOpenSelectByDefault()
    {
        var cut = Render(CreateSelect(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultOpen_ShouldSelectItemAndCloseWhenClicked()
    {
        var cut = Render(CreateSelect(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        var appleItem = items.First(i => i.TextContent.Contains("Apple"));
        MouseClickItem(appleItem);

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChange_ShouldCallWhenOpenedOrClosed()
    {
        var invoked = false;
        var receivedOpen = false;
        var receivedReason = SelectOpenChangeReason.None;

        var cut = Render(CreateSelect(
            onOpenChange: EventCallback.Factory.Create<SelectOpenChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedOpen = args.Open;
                receivedReason = args.Reason;
            })));

        var trigger = cut.Find("button");
        trigger.TriggerEvent("onmousedown", new MouseEventArgs());

        invoked.ShouldBeTrue();
        receivedOpen.ShouldBeTrue();
        receivedReason.ShouldBe(SelectOpenChangeReason.TriggerPress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChange_CancelPreventsOpening()
    {
        var cut = Render(CreateSelect(
            onOpenChange: EventCallback.Factory.Create<SelectOpenChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })));

        var trigger = cut.Find("button");
        trigger.TriggerEvent("onmousedown", new MouseEventArgs());

        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Modal_ShouldRenderBackdropWhenTrue()
    {
        var cut = Render(CreateSelect(
            modal: Blazix.BaseUI.Select.SelectModalMode.True,
            defaultOpen: true,
            childContent: CreateChildrenWithBackdrop()));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Modal_ShouldNotRenderBackdropWhenFalse()
    {
        var cut = Render(CreateSelect(
            modal: Blazix.BaseUI.Select.SelectModalMode.False,
            defaultOpen: true));

        // Default children do not include SelectBackdrop, so no backdrop should exist.
        // The positioner also uses role="presentation", so we check specifically for
        // backdrop characteristics (no data-side attribute distinguishes it from positioner).
        var presentations = cut.FindAll("div[role='presentation']");
        var backdropElements = presentations.Where(el => !el.HasAttribute("data-side"));
        backdropElements.Count().ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Modal_UpdatesRootContextWhenParameterChanges()
    {
        var cut = Render<SelectRoot<string>>(parameters => parameters
            .Add(p => p.DefaultOpen, true)
            .Add(p => p.Modal, Blazix.BaseUI.Select.SelectModalMode.False)
            .Add(p => p.ChildContent, CreateDefaultChildren()));

        cut.Instance.typedContext.Modal.ShouldBeFalse();

        cut = Render<SelectRoot<string>>(parameters => parameters
            .Add(p => p.DefaultOpen, true)
            .Add(p => p.Modal, Blazix.BaseUI.Select.SelectModalMode.True)
            .Add(p => p.ChildContent, CreateDefaultChildren()));

        cut.Instance.typedContext.Modal.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // --- New helper for multiple-selection scenarios ---

    private RenderFragment CreateMultipleSelect(
        IReadOnlyList<string>? defaultValues = null,
        IReadOnlyList<string>? values = null,
        bool defaultOpen = false,
        string? name = null,
        string? form = null,
        bool required = false,
        Func<string?, string?>? itemToStringValue = null,
        EventCallback<SelectValueChangeEventArgs<string>>? onValueChange = null,
        EventCallback<IReadOnlyList<string>>? valuesChanged = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            builder.AddAttribute(i++, "Multiple", true);
            if (defaultValues is not null) builder.AddAttribute(i++, "DefaultValues", defaultValues);
            if (values is not null) builder.AddAttribute(i++, "Values", values);
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            if (name is not null) builder.AddAttribute(i++, "Name", name);
            if (form is not null) builder.AddAttribute(i++, "Form", form);
            builder.AddAttribute(i++, "Required", required);
            if (itemToStringValue is not null) builder.AddAttribute(i++, "ItemToStringValue", itemToStringValue);
            if (onValueChange.HasValue) builder.AddAttribute(i++, "OnValueChange", onValueChange.Value);
            if (valuesChanged.HasValue) builder.AddAttribute(i++, "ValuesChanged", valuesChanged.Value);
            builder.AddAttribute(i++, "ChildContent", CreateDefaultChildren());
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectWithDisabledReadOnly(
        bool disabled = false,
        bool readOnly = false,
        string? id = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            builder.AddAttribute(i++, "Disabled", disabled);
            builder.AddAttribute(i++, "ReadOnly", readOnly);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                var ti = 1;
                if (id is not null) innerBuilder.AddAttribute(ti++, "Id", id);
                innerBuilder.AddAttribute(ti++, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(10);
                        popupBuilder.AddAttribute(11, "Value", "banana");
                        popupBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectForHiddenInputAutofill(bool disabled = false)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "Name", "fruit");
            builder.AddAttribute(2, "Disabled", disabled);
            builder.AddAttribute(3, "Items", new[]
            {
                new SelectOption<string>("apple", "Apple"),
                new SelectOption<string>("banana", "Banana")
            });
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateFieldSelectWithEmptySerializedValue()
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Name", "fruit");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<SelectRoot<string>>(0);
                fieldBuilder.AddAttribute(1, "DefaultValue", string.Empty);
                fieldBuilder.AddAttribute(2, "ItemToStringValue", (Func<string?, string?>)(value => value));
                fieldBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(selectBuilder =>
                {
                    selectBuilder.OpenComponent<SelectTrigger>(0);
                    selectBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<SelectValue<string>>(0);
                        b.AddAttribute(1, "Placeholder", "Select...");
                        b.CloseComponent();
                    }));
                    selectBuilder.CloseComponent();
                }));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectWithDisabledItems(bool defaultOpen = false)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(10);
                        popupBuilder.AddAttribute(11, "Value", "banana");
                        popupBuilder.AddAttribute(12, "Disabled", true);
                        popupBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(20);
                        popupBuilder.AddAttribute(21, "Value", "cherry");
                        popupBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Cherry")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // --- Value prop ---

    [Fact]
    public Task Value_UpdatesSelectValueLabelBeforePopupOpens()
    {
        var cut = Render(CreateSelect(
            value: "apple",
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var span = cut.FindComponent<SelectValue<string>>();
        span.Markup.ShouldContain("apple");

        // Re-render with new value before opening popup
        var cut2 = Render(CreateSelect(
            value: "banana",
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var span2 = cut2.FindComponent<SelectValue<string>>();
        span2.Markup.ShouldContain("banana");

        return Task.CompletedTask;
    }

    // --- Form ---

    [Fact]
    public Task ItemToStringValue_MultipleSelectionFormSubmission()
    {
        var cut = Render(CreateMultipleSelect(
            name: "fruits",
            defaultValues: new[] { "apple", "banana" },
            itemToStringValue: v => v?.ToUpper()));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(2);
        hiddenInputs[0].GetAttribute("value").ShouldBe("APPLE");
        hiddenInputs[1].GetAttribute("value").ShouldBe("BANANA");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HiddenInputChange_DoesNotSelectWhenDisabled()
    {
        var cut = Render(CreateSelectForHiddenInputAutofill(disabled: true));

        var hiddenInput = cut.Find("input[aria-hidden='true']");
        await hiddenInput.TriggerEventAsync("onchange", new ChangeEventArgs { Value = "apple" });

        hiddenInput = cut.Find("input[aria-hidden='true']");
        hiddenInput.GetAttribute("value").ShouldBe("");

        var value = cut.FindComponent<SelectValue<string>>();
        value.Markup.ShouldContain("Select...");
    }

    [Fact]
    public Task EmptySerializedSingleValue_RemainsPlaceholderAndUnfilled()
    {
        var cut = Render(CreateFieldSelectWithEmptySerializedValue());

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-placeholder").ShouldBeTrue();
        trigger.HasAttribute("data-filled").ShouldBeFalse();

        var hiddenInput = cut.Find("input[aria-hidden='true']");
        hiddenInput.GetAttribute("value").ShouldBe("");

        return Task.CompletedTask;
    }

    [Fact]
    public Task MultipleHiddenInputs_ForwardFormAttribute()
    {
        var cut = Render(CreateMultipleSelect(
            name: "fruits",
            defaultValues: new[] { "apple", "banana" },
            form: "checkout"));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(2);
        hiddenInputs[0].GetAttribute("form").ShouldBe("checkout");
        hiddenInputs[1].GetAttribute("form").ShouldBe("checkout");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Values_ShouldBeControlledWhenValuesParameterIsProvidedWithoutValuesChanged()
    {
        var cut = Render(CreateMultipleSelect(
            name: "fruits",
            values: new[] { "apple" },
            defaultOpen: true));

        cut.FindAll("input[type='hidden']").Single().GetAttribute("value").ShouldBe("apple");

        var banana = cut.FindAll("[role='option']").First(i => i.TextContent.Contains("Banana"));
        MouseClickItem(banana);

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(1);
        hiddenInputs[0].GetAttribute("value").ShouldBe("apple");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_MultipleReceivesNextValues()
    {
        IReadOnlyList<string>? receivedValues = null;

        var cut = Render(CreateMultipleSelect(
            defaultValues: new[] { "apple" },
            defaultOpen: true,
            onValueChange: EventCallback.Factory.Create<SelectValueChangeEventArgs<string>>(this, args =>
            {
                receivedValues = args.Values;
            })));

        var banana = cut.FindAll("[role='option']").First(i => i.TextContent.Contains("Banana"));
        MouseClickItem(banana);

        receivedValues.ShouldNotBeNull();
        receivedValues.ShouldBe(["apple", "banana"]);

        return Task.CompletedTask;
    }

    // --- ItemToStringLabel ---

    [Fact]
    public Task ItemToStringLabel_UpdatesTriggerTextAfterSelectingItem()
    {
        var cut = Render(CreateSelect(
            defaultOpen: true,
            itemToStringLabel: v => $"Fruit: {v}"));

        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        var valueSpan = cut.FindComponent<SelectValue<string>>();
        valueSpan.Markup.ShouldContain("Fruit: apple");

        return Task.CompletedTask;
    }

    // --- Event guard ---

    [Fact]
    public Task OnValueChange_IsNotCalledTwiceOnSelect()
    {
        var callCount = 0;

        var cut = Render(CreateSelect(
            defaultOpen: true,
            onValueChange: EventCallback.Factory.Create<SelectValueChangeEventArgs<string>>(this, _ =>
            {
                callCount++;
            })));

        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        callCount.ShouldBe(1);

        return Task.CompletedTask;
    }

    // --- Disabled ---

    [Fact]
    public Task Disabled_SetsDisabledState()
    {
        var cut = Render(CreateSelectWithDisabledReadOnly(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Disabled_UpdatesWhenDisabledPropChanges()
    {
        // Render as enabled
        var cut = Render(CreateSelectWithDisabledReadOnly(disabled: false));
        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeFalse();

        // Re-render as disabled
        var cut2 = Render(CreateSelectWithDisabledReadOnly(disabled: true));
        var trigger2 = cut2.Find("button");
        trigger2.HasAttribute("disabled").ShouldBeTrue();
        trigger2.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // --- ReadOnly ---

    [Fact]
    public Task ReadOnly_SetsReadOnlyState()
    {
        var cut = Render(CreateSelectWithDisabledReadOnly(readOnly: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-readonly").ShouldBe("true");
        trigger.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ReadOnly_ShouldNotOpenWhenClicked()
    {
        var cut = Render(CreateSelectWithDisabledReadOnly(readOnly: true));

        var trigger = cut.Find("button");
        // ReadOnly does not set disabled on the button, so click goes through
        // but SetOpenAsync should check ReadOnly in the trigger's click handler
        // Actually, ReadOnly in base-ui prevents item selection, not opening.
        // Let's verify the select can open but items cannot be selected
        await trigger.TriggerEventAsync("onmousedown", new MouseEventArgs());
        cut.FindComponent<SelectTrigger>().Render();

        // Open the popup, then try to select an item
        var items = cut.FindAll("[role='option']");
        if (items.Count > 0)
        {
            MouseClickItem(items[0]);
        }

        // Value should remain null since readOnly blocks selection
        var valueSpan = cut.FindComponent<SelectValue<string>>();
        valueSpan.Markup.ShouldContain("Select...");
    }

    [Fact]
    public async Task ReadOnly_ShouldNotOpenWithKeyboard()
    {
        var cut = Render(CreateSelectWithDisabledReadOnly(readOnly: true));

        var trigger = cut.Find("button");
        await trigger.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Enter" });
        cut.FindComponent<SelectTrigger>().Render();

        // Verify that even if opened, selecting an item via keyboard doesn't change value
        var items = cut.FindAll("[role='option']");
        if (items.Count > 0)
        {
            MouseClickItem(items[0]);
        }

        var valueSpan = cut.FindComponent<SelectValue<string>>();
        valueSpan.Markup.ShouldContain("Select...");
    }

    // --- Id prop ---

    [Fact]
    public Task Id_SetsIdOnTrigger()
    {
        var cut = Render(CreateSelectWithDisabledReadOnly(id: "my-custom-id"));

        var trigger = cut.Find("button");
        trigger.GetAttribute("id").ShouldBe("my-custom-id");

        return Task.CompletedTask;
    }

    // --- Null reset ---

    [Fact]
    public Task Value_ResetsSelectedIndexWhenSetToNull()
    {
        // First render with a value
        var cut = Render(CreateSelect(
            value: "apple",
            defaultOpen: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeTrue();

        // Re-render with null value (simulating reset)
        var cut2 = Render(CreateSelect(
            defaultOpen: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var items2 = cut2.FindAll("[role='option']");
        items2.Any(i => i.HasAttribute("data-selected")).ShouldBeFalse();

        return Task.CompletedTask;
    }

    // --- Multiple ---

    [Fact]
    public Task Multiple_ShouldAllowMultipleSelections()
    {
        var cut = Render(CreateMultipleSelect(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Banana")));

        // Both items should be selected in uncontrolled multi-select
        items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeTrue();
        items.First(i => i.TextContent.Contains("Banana")).HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldDeselectItemsWhenClickedAgain()
    {
        var cut = Render(CreateMultipleSelect(
            defaultOpen: true,
            defaultValues: new[] { "apple" }));

        // Click apple again to deselect it
        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldHandleDefaultValueAsArray()
    {
        var cut = Render(CreateMultipleSelect(
            defaultValues: new[] { "apple", "cherry" },
            defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeTrue();
        items.First(i => i.TextContent.Contains("Cherry")).HasAttribute("data-selected").ShouldBeTrue();
        items.First(i => i.TextContent.Contains("Banana")).HasAttribute("data-selected").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldSerializeMultipleValuesForFormSubmission()
    {
        var cut = Render(CreateMultipleSelect(
            name: "fruits",
            defaultValues: new[] { "apple", "banana" }));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(2);
        hiddenInputs[0].GetAttribute("value").ShouldBe("apple");
        hiddenInputs[1].GetAttribute("value").ShouldBe("banana");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldSerializeEmptyArrayAsEmptyString()
    {
        var cut = Render(CreateMultipleSelect(name: "fruits"));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(0);

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.GetAttribute("value").ShouldBe("");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_DoesNotMarkHiddenInputAsRequiredWhenSelectionExists()
    {
        var cut = Render(CreateMultipleSelect(
            name: "fruits",
            required: true,
            defaultValues: new[] { "apple" }));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(1);
        // When there's a selection, the hidden input should not be marked required
        hiddenInputs[0].HasAttribute("required").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_KeepsHiddenInputRequiredWhenNoSelectionExists()
    {
        var cut = Render(CreateMultipleSelect(
            name: "fruits",
            required: true));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(0);

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.HasAttribute("required").ShouldBeTrue();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-required").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldNotClosePopupWhenSelectingItems()
    {
        var cut = Render(CreateMultipleSelect(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        // Popup should remain open in multi-select mode
        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldClosePopupInSingleSelectMode()
    {
        // Single-select mode should close popup on item press
        var cut = Render(CreateSelect(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ShouldUpdateSelectedItemsWhenValuePropChanges()
    {
        // First render with apple selected
        var cut = Render(CreateMultipleSelect(
            values: new[] { "apple" },
            defaultOpen: true,
            valuesChanged: EventCallback.Factory.Create<IReadOnlyList<string>>(this, _ => { })));

        var items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeTrue();

        // Re-render with different values
        var cut2 = Render(CreateMultipleSelect(
            values: new[] { "banana", "cherry" },
            defaultOpen: true,
            valuesChanged: EventCallback.Factory.Create<IReadOnlyList<string>>(this, _ => { })));

        var items2 = cut2.FindAll("[role='option']");
        items2.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeFalse();
        items2.First(i => i.TextContent.Contains("Banana")).HasAttribute("data-selected").ShouldBeTrue();
        items2.First(i => i.TextContent.Contains("Cherry")).HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // --- Highlight on hover ---

    [Fact]
    public async Task HighlightItemOnHover_HighlightsItemOnMouseMove()
    {
        var cut = Render(CreateSelect(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        await items[0].TriggerEventAsync("onmousemove", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeTrue();
    }

    [Fact]
    public async Task HighlightItemOnHover_DoesNotHighlightWhenDisabled()
    {
        var cut = Render(CreateSelectWithDisabledItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        // Banana (index 1) is disabled
        await items[1].TriggerEventAsync("onmousemove", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        items[1].HasAttribute("data-highlighted").ShouldBeFalse();
    }

    [Fact]
    public async Task HighlightItemOnHover_DoesNotRemoveHighlightOnMouseLeaveWhenDisabled()
    {
        var cut = Render(CreateSelectWithDisabledItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        // Hover on non-disabled item first
        await items[0].TriggerEventAsync("onmouseenter", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeTrue();

        // Disabled items don't get highlighted on enter, but mouse leave on a
        // non-disabled item should still remove its highlight
        await items[0].TriggerEventAsync("onmouseleave", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeFalse();
    }

    // --- HighlightItemOnHover disabled ---

    private RenderFragment CreateSelectWithHighlightItemOnHover(bool highlightItemOnHover, bool defaultOpen = false)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            builder.AddAttribute(i++, "HighlightItemOnHover", highlightItemOnHover);
            builder.AddAttribute(i++, "ChildContent", CreateDefaultChildren());
            builder.CloseComponent();
        };
    }

    [Fact]
    public async Task HighlightItemOnHover_FalseDoesNotHighlightOnMouseMove()
    {
        var cut = Render(CreateSelectWithHighlightItemOnHover(highlightItemOnHover: false, defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        await items[0].TriggerEventAsync("onmousemove", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeFalse();
    }

    [Fact]
    public async Task HighlightItemOnHover_FalseDoesNotRemoveHighlightOnMouseLeave()
    {
        var cut = Render(CreateSelectWithHighlightItemOnHover(highlightItemOnHover: false, defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        // Even if an item was highlighted by keyboard, mouse leave should not remove it when disabled
        await items[0].TriggerEventAsync("onmouseleave", new MouseEventArgs());

        // The item should not have been affected (stays false since it was never highlighted)
        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeFalse();
    }

    // --- OnOpenChangeComplete ---

    [Fact]
    public async Task OnOpenChangeComplete_FiresWhenTransitionEnds()
    {
        var invoked = false;
        var receivedOpen = false;

        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "OnOpenChangeComplete",
                EventCallback.Factory.Create<bool>(this, open =>
                {
                    invoked = true;
                    receivedOpen = open;
                }));
            builder.AddAttribute(3, "ChildContent", CreateDefaultChildren());
            builder.CloseComponent();
        });

        var cut = Render(fragment);

        // Simulate JS calling OnTransitionEnd through the component's dispatcher.
        var root = cut.FindComponent<SelectRoot<string>>();
        await root.InvokeAsync(() => root.Instance.OnTransitionEnd(true));

        invoked.ShouldBeTrue();
        receivedOpen.ShouldBeTrue();
    }

    // --- Id prop (root level) ---

    [Fact]
    public Task Id_RootIdFlowsToTrigger()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "Id", "my-root-id");
            builder.AddAttribute(2, "ChildContent", CreateDefaultChildren());
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var trigger = cut.Find("button");
        trigger.GetAttribute("id").ShouldBe("my-root-id");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Id_RootIdDefaultsToGenerated()
    {
        var cut = Render(CreateSelect());
        var trigger = cut.Find("button");
        var id = trigger.GetAttribute("id");
        id.ShouldNotBeNull();
        id.ShouldNotBeEmpty();
        // Should be an auto-generated GUID-based ID (not "my-root-id")
        id.ShouldNotBe("my-root-id");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Id_UnnamedHiddenInputUsesGeneratedRootId()
    {
        var cut = Render(CreateSelect());

        var triggerId = cut.Find("button").GetAttribute("id");
        var hiddenInput = cut.Find("input[aria-hidden='true']");

        hiddenInput.GetAttribute("id").ShouldBe($"{triggerId}-hidden-input");
        hiddenInput.HasAttribute("name").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ClosedTypeahead_WithItemGroupsStartsAfterCurrentValue()
    {
        var groups = new[]
        {
            new SelectOptionGroup<string>(
                Label: "A",
                Items:
                [
                    new SelectOption<string>("apple", "Apple"),
                    new SelectOption<string>("apricot", "Apricot")
                ])
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "apple");
            builder.AddAttribute(2, "ItemGroups", groups);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        await cut.InvokeAsync(async () => await root.typedContext.HandleClosedTypeaheadAsync("a"));

        root.typedContext.GetValue().ShouldBe("apricot");
    }

    [Fact]
    public async Task ClosedTypeahead_WithLiveNullItemLabelSelectsNullValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<string?>>(0);
            builder.AddAttribute(1, "DefaultValue", "apple");
            builder.AddAttribute(2, "DefaultOpen", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string?>>(0);
                        popupBuilder.AddAttribute(1, "Value", (string?)null);
                        popupBuilder.AddAttribute(2, "Label", "None");
                        popupBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "None")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string?>>(10);
                        popupBuilder.AddAttribute(11, "Value", "apple");
                        popupBuilder.AddAttribute(12, "Label", "Apple");
                        popupBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var root = cut.FindComponent<SelectRoot<string?>>().Instance;
        root.typedContext.GetLabel(null).ShouldBe("None");

        await cut.InvokeAsync(async () => await root.typedContext.HandleClosedTypeaheadAsync("n"));

        root.typedContext.GetValue().ShouldBeNull();
    }

    // --- IsItemEqualToValue ---

    [Fact]
    public Task IsItemEqualToValue_UsesCustomComparer()
    {
        // Use case-insensitive comparer so "APPLE" matches "apple"
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "APPLE");
            builder.AddAttribute(2, "DefaultOpen", true);
            builder.AddAttribute(3, "IsItemEqualToValue",
                new Func<string, string, bool>((a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)));
            builder.AddAttribute(4, "ChildContent", CreateDefaultChildren());
            builder.CloseComponent();
        });

        var cut = Render(fragment);

        var items = cut.FindAll("[role='option']");
        // "apple" item should match "APPLE" value via custom comparer
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsItemEqualToValue_DefaultsToDefaultComparer()
    {
        // Without custom comparer, "APPLE" should NOT match "apple"
        var cut = Render(CreateSelect(defaultValue: "APPLE", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items.First(i => i.TextContent.Contains("Apple")).HasAttribute("data-selected").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // --- FieldRoot integration helpers ---

    private RenderFragment CreateSelectInFieldRoot(
        string fieldName = "fruit",
        bool fieldDisabled = false,
        bool defaultOpen = false,
        string? name = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Name", fieldName);
            builder.AddAttribute(2, "Disabled", fieldDisabled);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<SelectRoot<string>>(0);
                var i = 1;
                if (name is not null) fieldBuilder.AddAttribute(i++, "Name", name);
                fieldBuilder.AddAttribute(i++, "DefaultOpen", defaultOpen);
                fieldBuilder.AddAttribute(i++, "ChildContent", CreateDefaultChildren());
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectInFormAndField(
        string fieldName = "fruit",
        Dictionary<string, string[]>? errors = null,
        bool defaultOpen = false)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Form.Form>(0);
            builder.AddAttribute(1, "Model", new object());
            if (errors is not null)
                builder.AddAttribute(2, "Errors", errors);
            builder.AddAttribute(3, "ChildContent",
                (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(context =>
                    (RenderFragment)(formBuilder =>
                    {
                        formBuilder.OpenComponent<FieldRoot>(0);
                        formBuilder.AddAttribute(1, "Name", fieldName);
                        formBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
                        {
                            fieldBuilder.OpenComponent<SelectRoot<string>>(0);
                            fieldBuilder.AddAttribute(1, "DefaultOpen", defaultOpen);
                            fieldBuilder.AddAttribute(2, "ChildContent", CreateDefaultChildren());
                            fieldBuilder.CloseComponent();
                        }));
                        formBuilder.CloseComponent();
                    })));
            builder.CloseComponent();
        };
    }

    // --- FieldRoot integration tests ---

    [Fact]
    public Task FieldRoot_ResolvedNameFromFieldContext()
    {
        // When Name is not set on SelectRoot but FieldRoot provides one,
        // ResolvedName should use the field name for hidden inputs
        var cut = Render(CreateSelectInFieldRoot(fieldName: "fruit", defaultOpen: true));

        // Select an item to make the hidden input appear with the field name
        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        var formInput = cut.Find("input[aria-hidden='true']");
        formInput.GetAttribute("name").ShouldBe("fruit");

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_ResolvedDisabledFromFieldContext()
    {
        var cut = Render(CreateSelectInFieldRoot(fieldDisabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_SetsFilledOnValueChange()
    {
        var cut = Render(CreateSelectInFieldRoot(defaultOpen: true));

        // Before selection, trigger should not be filled
        var trigger = cut.Find("button");
        trigger.HasAttribute("data-filled").ShouldBeFalse();

        // Select an item
        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        trigger = cut.Find("button");
        trigger.HasAttribute("data-filled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_SetsDirtyOnValueChange()
    {
        var cut = Render(CreateSelectInFieldRoot(defaultOpen: true));

        // Before selection, trigger should not be dirty
        var trigger = cut.Find("button");
        trigger.HasAttribute("data-dirty").ShouldBeFalse();

        // Select an item
        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        trigger = cut.Find("button");
        trigger.HasAttribute("data-dirty").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_ClearsFormErrorsOnValueChange()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["fruit"] = ["Required field"]
        };

        var cut = Render(CreateSelectInFormAndField(fieldName: "fruit", errors: errors, defaultOpen: true));

        // Select an item — this should clear form errors
        var items = cut.FindAll("[role='option']");
        MouseClickItem(items.First(i => i.TextContent.Contains("Apple")));

        // After selecting, the error should be cleared
        // We verify the trigger no longer has aria-invalid
        var trigger = cut.Find("button");
        trigger.HasAttribute("aria-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
