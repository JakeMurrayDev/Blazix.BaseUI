using Blazix.BaseUI.Tests.Contracts.Autocomplete;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Tests.Autocomplete;

    public class AutocompleteRootTests : BunitContext, IAutocompleteRootContract
{
    private const string AutocompleteModule = "./_content/Blazix.BaseUI/blazix-baseui-autocomplete.min.js";

    public AutocompleteRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly IReadOnlyList<string> Fruits = ["Apple", "Apricot", "Banana"];

    private RenderFragment CreateAutocomplete(
        IReadOnlyList<string>? items = null,
        string? defaultValue = null,
        string? value = null,
        bool defaultOpen = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        AutocompleteMode mode = AutocompleteMode.List,
        AutocompleteAutoHighlight autoHighlight = AutocompleteAutoHighlight.False,
        EventCallback<AutocompleteValueChangeEventArgs>? onValueChange = null,
        EventCallback<AutocompleteOpenChangeEventArgs>? onOpenChange = null)
    {
        return builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            var i = 1;
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.Items), items ?? Fruits);
            if (defaultValue is not null) builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.DefaultValue), defaultValue);
            if (value is not null) builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.Value), value);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.DefaultOpen), defaultOpen);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.Disabled), disabled);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.ReadOnly), readOnly);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.Required), required);
            if (name is not null) builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.Name), name);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.Mode), mode);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.AutoHighlight), autoHighlight);
            if (onValueChange.HasValue) builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.OnValueChange), onValueChange.Value);
            if (onOpenChange.HasValue) builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.OnOpenChange), onOpenChange.Value);
            builder.AddAttribute(i++, nameof(AutocompleteRoot<string>.ChildContent), CreateDefaultChildren());
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultChildren()
    {
        return builder =>
        {
            builder.OpenComponent<AutocompleteInput>(0);
            builder.AddAttribute(1, "placeholder", "Search");
            builder.CloseComponent();

            builder.OpenComponent<AutocompleteTrigger>(10);
            builder.AddAttribute(11, nameof(AutocompleteTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
            builder.CloseComponent();

            builder.OpenComponent<AutocompletePositioner>(20);
            builder.AddAttribute(21, nameof(AutocompletePositioner.ChildContent), (RenderFragment)(positionerBuilder =>
            {
                positionerBuilder.OpenComponent<AutocompletePopup>(0);
                positionerBuilder.AddAttribute(1, nameof(AutocompletePopup.ChildContent), (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<AutocompleteEmpty>(0);
                    popupBuilder.AddAttribute(1, nameof(AutocompleteEmpty.ChildContent), (RenderFragment)(b => b.AddContent(0, "No matches")));
                    popupBuilder.CloseComponent();

                    popupBuilder.OpenComponent<AutocompleteList>(10);
                    popupBuilder.AddAttribute(11, nameof(AutocompleteList.ChildContent), (RenderFragment)(listBuilder =>
                    {
                        listBuilder.OpenComponent<AutocompleteItem<string>>(0);
                        listBuilder.AddAttribute(1, nameof(AutocompleteItem<string>.Value), "Apple");
                        listBuilder.AddAttribute(2, nameof(AutocompleteItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Apple")));
                        listBuilder.CloseComponent();

                        listBuilder.OpenComponent<AutocompleteItem<string>>(10);
                        listBuilder.AddAttribute(11, nameof(AutocompleteItem<string>.Value), "Apricot");
                        listBuilder.AddAttribute(12, nameof(AutocompleteItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Apricot")));
                        listBuilder.CloseComponent();

                        listBuilder.OpenComponent<AutocompleteItem<string>>(20);
                        listBuilder.AddAttribute(21, nameof(AutocompleteItem<string>.Value), "Banana");
                        listBuilder.AddAttribute(22, nameof(AutocompleteItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Banana")));
                        listBuilder.CloseComponent();
                    }));
                    popupBuilder.CloseComponent();
                }));
                positionerBuilder.AddAttribute(2, "data-testid", "autocomplete-popup");
                positionerBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<AutocompleteValue>(30);
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateTriggerAnchoredPopupChildren()
    {
        return builder =>
        {
            builder.OpenComponent<AutocompleteTrigger>(0);
            builder.AddAttribute(1, nameof(AutocompleteTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
            builder.CloseComponent();

            builder.OpenComponent<AutocompletePositioner>(10);
            builder.AddAttribute(11, nameof(AutocompletePositioner.ChildContent), (RenderFragment)(positionerBuilder =>
            {
                positionerBuilder.OpenComponent<AutocompletePopup>(0);
                positionerBuilder.AddAttribute(1, nameof(AutocompletePopup.ChildContent), (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<AutocompleteInput>(0);
                    popupBuilder.CloseComponent();
                }));
                positionerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task Input_ShouldExposeComboboxAttributes()
    {
        var cut = Render(CreateAutocomplete(defaultValue: "Ap", defaultOpen: true, name: "fruit"));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("type").ShouldBe("text");
        input.GetAttribute("value").ShouldBe("Ap");
        input.GetAttribute("aria-expanded").ShouldBe("true");
        input.GetAttribute("aria-haspopup").ShouldBe("listbox");
        input.GetAttribute("aria-autocomplete").ShouldBe("list");
        input.GetAttribute("autocomplete").ShouldBe("off");
        input.GetAttribute("spellcheck").ShouldBe("false");
        input.GetAttribute("autocorrect").ShouldBe("off");
        input.GetAttribute("autocapitalize").ShouldBe("none");
        input.GetAttribute("name").ShouldBe("fruit");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ListMode_ShouldFilterItemsAndExposeEmptyState()
    {
        var cut = Render(CreateAutocomplete(defaultValue: "zz", defaultOpen: true));

        cut.FindAll("[role='option']").Count.ShouldBe(0);
        cut.Markup.ShouldContain("No matches");

        var input = cut.Find("input[role='combobox']");
        input.HasAttribute("data-list-empty").ShouldBeTrue();

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-list-empty").ShouldBeTrue();

        var popup = cut.Find("[data-testid='autocomplete-popup']");
        popup.HasAttribute("data-empty").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ItemPress_ShouldFillInputClosePopupAndRaiseChangeDetails()
    {
        AutocompleteValueChangeEventArgs? received = null;
        var callback = EventCallback.Factory.Create<AutocompleteValueChangeEventArgs>(this, args => received = args);
        var cut = Render(CreateAutocomplete(defaultOpen: true, onValueChange: callback));

        var banana = cut.FindAll("[role='option']").Single(i => i.TextContent == "Banana");
        await banana.ClickAsync(new MouseEventArgs());

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("value").ShouldBe("Banana");
        input.GetAttribute("aria-expanded").ShouldBe("false");

        received.ShouldNotBeNull();
        received.Value.ShouldBe("Banana");
        received.Reason.ShouldBe(AutocompleteChangeReason.ItemPress);
    }

    [Fact]
    public async Task OnValueChangeCancelPreventsInputValueChange()
    {
        var callback = EventCallback.Factory.Create<AutocompleteValueChangeEventArgs>(this, args => args.Cancel());
        var cut = Render(CreateAutocomplete(defaultValue: "Ap", onValueChange: callback));

        var input = cut.Find("input[role='combobox']");
        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "Banana" });

        input = cut.Find("input[role='combobox']");
        input.GetAttribute("value").ShouldBe("Ap");
    }

    [Fact]
    public async Task OnOpenChangeCancelPreventsOpening()
    {
        var callback = EventCallback.Factory.Create<AutocompleteOpenChangeEventArgs>(this, args => args.Cancel());
        var cut = Render(CreateAutocomplete(onOpenChange: callback));

        var trigger = cut.Find("button");
        await trigger.TriggerEventAsync("onmousedown", new MouseEventArgs { Button = 0 });

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public async Task Virtualized_ShouldRetainActiveIndexWhenRenderedWindowUnmounts()
    {
        var showWindow = true;
        AutocompleteValueChangeEventArgs? received = null;
        var callback = EventCallback.Factory.Create<AutocompleteValueChangeEventArgs>(this, args => received = args);
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.DefaultOpen), true);
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.Virtualized), true);
            builder.AddAttribute(4, nameof(AutocompleteRoot<string>.OnValueChange), callback);
            builder.AddAttribute(5, nameof(AutocompleteRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AutocompleteInput>(0);
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<AutocompleteList>(10);
                childBuilder.AddAttribute(11, nameof(AutocompleteList.ChildContent), (RenderFragment)(listBuilder =>
                {
                    if (showWindow)
                    {
                        listBuilder.OpenComponent<AutocompleteItem<string>>(0);
                        listBuilder.AddAttribute(1, nameof(AutocompleteItem<string>.Value), Fruits[0]);
                        listBuilder.AddAttribute(2, nameof(AutocompleteItem<string>.Index), 0);
                        listBuilder.AddAttribute(3, nameof(AutocompleteItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, Fruits[0])));
                        listBuilder.CloseComponent();
                    }
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
        var cut = Render(fragment);
        var root = cut.FindComponent<AutocompleteRoot<string>>();

        await cut.InvokeAsync(() => root.Instance.OnNavigate(1));
        await cut.InvokeAsync(() => root.Instance.OnNavigate(1));
        showWindow = false;
        cut.Render();
        await cut.InvokeAsync(() => root.Instance.OnCommitActive());

        received.ShouldNotBeNull();
        received.Value.ShouldBe("Apricot");
        received.Reason.ShouldBe(AutocompleteChangeReason.ItemPress);
    }

    [Fact]
    public Task Input_ShouldUseStringBooleanAttributeValues()
    {
        var cut = Render(CreateAutocomplete(defaultValue: "Ap", disabled: true, readOnly: true, required: true));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("readonly").ShouldBe("readonly");
        input.GetAttribute("required").ShouldBe("required");
        input.GetAttribute("disabled").ShouldBe("disabled");
        input.GetAttribute("data-readonly").ShouldBe("true");
        input.GetAttribute("data-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task InputGroup_ShouldInvokeConsumerMouseDownHandler()
    {
        var mouseDownCount = 0;
        var cut = Render(builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.OpenOnInputClick), true);
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AutocompleteInputGroup>(0);
                childBuilder.AddAttribute(1, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, () => mouseDownCount++));
                childBuilder.AddAttribute(2, nameof(AutocompleteInputGroup.ChildContent), (RenderFragment)(groupBuilder =>
                {
                    groupBuilder.OpenComponent<AutocompleteInput>(0);
                    groupBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        await cut.Find("[role='group']").TriggerEventAsync("onmousedown", new MouseEventArgs { Button = 0 });

        mouseDownCount.ShouldBe(1);
    }

    [Fact]
    public async Task Item_ShouldInvokeConsumerPointerLeaveHandler()
    {
        var pointerLeaveCount = 0;
        var cut = Render(builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.DefaultOpen), true);
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AutocompleteList>(0);
                childBuilder.AddAttribute(1, nameof(AutocompleteList.ChildContent), (RenderFragment)(listBuilder =>
                {
                    listBuilder.OpenComponent<AutocompleteItem<string>>(0);
                    listBuilder.AddAttribute(1, nameof(AutocompleteItem<string>.Value), "Apple");
                    listBuilder.AddAttribute(2, "onpointerleave", EventCallback.Factory.Create<PointerEventArgs>(this, () => pointerLeaveCount++));
                    listBuilder.AddAttribute(3, nameof(AutocompleteItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Apple")));
                    listBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        await cut.Find("[role='option']").TriggerEventAsync("onpointerleave", new PointerEventArgs());

        pointerLeaveCount.ShouldBe(1);
    }

    [Fact]
    public Task ItemNativeButton_ShouldExposeNativeButtonSemantics()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.DefaultOpen), true);
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AutocompleteList>(0);
                childBuilder.AddAttribute(1, nameof(AutocompleteList.ChildContent), (RenderFragment)(listBuilder =>
                {
                    listBuilder.OpenComponent<AutocompleteItem<string>>(0);
                    listBuilder.AddAttribute(1, nameof(AutocompleteItem<string>.Value), "Apple");
                    listBuilder.AddAttribute(2, nameof(AutocompleteItem<string>.NativeButton), true);
                    listBuilder.AddAttribute(3, nameof(AutocompleteItem<string>.Disabled), true);
                    listBuilder.AddAttribute(4, nameof(AutocompleteItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Apple")));
                    listBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("button[role='option']");
        item.GetAttribute("type").ShouldBe("button");
        item.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClearNativeButtonFalse_ShouldRenderNonNativeButton()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.DefaultValue), "Apple");
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AutocompleteClear>(0);
                childBuilder.AddAttribute(1, nameof(AutocompleteClear.NativeButton), false);
                childBuilder.AddAttribute(2, nameof(AutocompleteClear.ChildContent), (RenderFragment)(b => b.AddContent(0, "Clear")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var clear = cut.Find("[role='button']");
        clear.TagName.ShouldBe("DIV");
        clear.HasAttribute("type").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Arrow_ShouldRefreshWhenPositionerParametersChange()
    {
        static RenderFragment BuildChildren(Side side) => childBuilder =>
        {
            childBuilder.OpenComponent<AutocompletePositioner>(0);
            childBuilder.AddAttribute(1, nameof(AutocompletePositioner.Side), side);
            childBuilder.AddAttribute(2, nameof(AutocompletePositioner.ChildContent), (RenderFragment)(positionerBuilder =>
            {
                positionerBuilder.OpenComponent<AutocompleteArrow>(0);
                positionerBuilder.CloseComponent();
            }));
            childBuilder.CloseComponent();
        };

        var cut = Render<AutocompleteRoot<string>>(parameters => parameters
            .Add(p => p.Items, Fruits)
            .Add(p => p.DefaultOpen, true)
            .Add(p => p.ChildContent, BuildChildren(Side.Bottom)));

        cut.Find("[aria-hidden='true']").GetAttribute("data-side").ShouldBe("bottom");

        cut.Render(parameters => parameters
            .Add(p => p.Items, Fruits)
            .Add(p => p.DefaultOpen, true)
            .Add(p => p.ChildContent, BuildChildren(Side.Top)));

        cut.FindComponent<AutocompletePositioner>().Instance.Side.ShouldBe(Side.Top);
        cut.Find("[aria-hidden='true']").GetAttribute("data-side").ShouldBe("top");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RootId_ShouldRemainStableAfterFirstRenderWhenIdParameterChanges()
    {
        var id = "autocomplete-initial";
        var cut = Render(builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Id), id);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AutocompleteInput>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Find("input[role='combobox']").GetAttribute("id").ShouldBe("autocomplete-initial");

        id = "autocomplete-next";
        cut.Render();

        cut.Find("input[role='combobox']").GetAttribute("id").ShouldBe("autocomplete-initial");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task BothMode_ShouldUseTypedQueryForFilteringAndInlineHighlightForDisplay()
    {
        var cut = Render(CreateAutocomplete(
            defaultValue: "Ap",
            defaultOpen: true,
            mode: AutocompleteMode.Both,
            autoHighlight: AutocompleteAutoHighlight.Always));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("value").ShouldBe("Apple");

        cut.FindAll("[role='option']").Select(i => i.TextContent).ShouldBe(["Apple", "Apricot"]);

        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "Ba" });
        input = cut.Find("input[role='combobox']");

        input.GetAttribute("value").ShouldBe("Banana");
        cut.FindAll("[role='option']").Select(i => i.TextContent).ShouldBe(["Banana"]);
    }

    [Fact]
    public async Task BothMode_ShouldPreserveTypedPrefixWhenReplacingInlineSuffix()
    {
        var cut = Render(CreateAutocomplete(
            defaultValue: "Ap",
            defaultOpen: true,
            mode: AutocompleteMode.Both,
            autoHighlight: AutocompleteAutoHighlight.Always));

        var input = cut.Find("input[role='combobox']");
        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "ApBa" });
        input = cut.Find("input[role='combobox']");

        input.GetAttribute("value").ShouldBe("ApBa");
        cut.FindAll("[role='option']").ShouldBeEmpty();
    }

    [Fact]
    public async Task BothMode_ShouldNotReapplyInlineCompletionWhenDeletingSelectedSuffix()
    {
        var cut = Render(CreateAutocomplete(
            defaultValue: "Ap",
            defaultOpen: true,
            mode: AutocompleteMode.Both,
            autoHighlight: AutocompleteAutoHighlight.Always));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("value").ShouldBe("Apple");

        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "Ap" });
        input = cut.Find("input[role='combobox']");

        input.GetAttribute("value").ShouldBe("Ap");
        cut.Find("[role='option'][data-highlighted]").TextContent.ShouldBe("Apple");
    }

    [Fact]
    public async Task BothMode_ShouldNotReapplySameInlineCompletionWhileBackspacingPrefix()
    {
        var cut = Render(CreateAutocomplete(
            defaultValue: "Ap",
            defaultOpen: true,
            mode: AutocompleteMode.Both,
            autoHighlight: AutocompleteAutoHighlight.Always));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("value").ShouldBe("Apple");

        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "Ap" });
        input = cut.Find("input[role='combobox']");
        input.GetAttribute("value").ShouldBe("Ap");

        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "A" });
        input = cut.Find("input[role='combobox']");

        input.GetAttribute("value").ShouldBe("A");
        cut.Find("[role='option'][data-highlighted]").TextContent.ShouldBe("Apple");
    }

    [Fact]
    public async Task BothMode_ShouldSynchronizeInlineCompletionSelection()
    {
        var module = JSInterop.SetupModule(AutocompleteModule);

        var cut = Render(CreateAutocomplete(
            defaultValue: "Ap",
            defaultOpen: true,
            mode: AutocompleteMode.Both,
            autoHighlight: AutocompleteAutoHighlight.Always));

        var input = cut.Find("input[role='combobox']");
        await input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "Ba" });

        var invocation = module.Invocations.Last(i => i.Identifier == "syncInputSelection");
        invocation.Arguments[1].ShouldBe("Ba");
        invocation.Arguments[2].ShouldBe("Banana");
    }

    [Fact]
    public Task Positioner_ShouldUseTriggerAnchorWhenInputIsInsidePopup()
    {
        var module = JSInterop.SetupModule(AutocompleteModule);
        var cut = Render(builder =>
        {
            builder.OpenComponent<AutocompleteRoot<string>>(0);
            builder.AddAttribute(1, nameof(AutocompleteRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(AutocompleteRoot<string>.DefaultOpen), true);
            builder.AddAttribute(3, nameof(AutocompleteRoot<string>.ChildContent), CreateTriggerAnchoredPopupChildren());
            builder.CloseComponent();
        });

        var triggerElement = cut.FindComponent<AutocompleteTrigger>().Instance.Element;
        var popupInputElement = cut.FindComponent<AutocompleteInput>().Instance.Element;
        triggerElement.ShouldNotBeNull();
        popupInputElement.ShouldNotBeNull();

        var positionerInvocations = module.Invocations
            .Where(i => i.Identifier is "initializePositioner" or "updatePosition")
            .ToList();

        positionerInvocations.ShouldNotBeEmpty();
        foreach (var invocation in positionerInvocations)
        {
            invocation.Arguments[1].ShouldBe(triggerElement.Value);
            invocation.Arguments[1].ShouldNotBe(popupInputElement.Value);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public Task Trigger_ShouldExposePopupStateDisabledReadonlyRequiredAndListEmptyAttributes()
    {
        var cut = Render(CreateAutocomplete(
            items: Fruits,
            defaultValue: "zz",
            defaultOpen: true,
            disabled: true,
            readOnly: true,
            required: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("role").ShouldBeNull();
        trigger.GetAttribute("aria-expanded").ShouldBe("true");
        trigger.GetAttribute("aria-haspopup").ShouldBe("listbox");
        trigger.GetAttribute("aria-required").ShouldBe("true");
        trigger.HasAttribute("disabled").ShouldBeTrue();
        trigger.HasAttribute("data-disabled").ShouldBeTrue();
        trigger.HasAttribute("data-readonly").ShouldBeTrue();
        trigger.HasAttribute("data-required").ShouldBeTrue();
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();
        trigger.HasAttribute("data-list-empty").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
