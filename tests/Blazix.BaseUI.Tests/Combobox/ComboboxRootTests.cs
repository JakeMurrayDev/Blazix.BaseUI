using Blazix.BaseUI.Combobox;
using Blazix.BaseUI.Tests.Contracts.Combobox;

namespace Blazix.BaseUI.Tests.Combobox;

public class ComboboxRootTests : BunitContext, IComboboxRootContract
{
    private static readonly IReadOnlyList<string> Fruits = ["Apple", "Apricot", "Banana"];

    private sealed record FruitOption(string Value, string Label);

    public ComboboxRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateCombobox(
        IReadOnlyList<string>? items = null,
        string? defaultValue = null,
        IReadOnlyList<string>? defaultValues = null,
        string? defaultInputValue = null,
        bool multiple = false,
        bool defaultOpen = false,
        bool inline = false,
        ComboboxAutoHighlight autoHighlight = ComboboxAutoHighlight.False,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        EventCallback<ComboboxValueChangeEventArgs<string>>? onValueChange = null,
        EventCallback<ComboboxInputValueChangeEventArgs>? onInputValueChange = null,
        bool? open = null,
        EventCallback<ComboboxOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<bool>? openChanged = null,
        IReadOnlyDictionary<string, object>? itemAdditionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            var i = 1;
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Items), items ?? Fruits);
            if (defaultValue is not null) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.DefaultValue), defaultValue);
            if (defaultValues is not null) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.DefaultValues), defaultValues);
            if (defaultInputValue is not null) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.DefaultInputValue), defaultInputValue);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Multiple), multiple);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.DefaultOpen), defaultOpen);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Inline), inline);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.AutoHighlight), autoHighlight);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Disabled), disabled);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.ReadOnly), readOnly);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Required), required);
            if (name is not null) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Name), name);
            if (onValueChange.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.OnValueChange), onValueChange.Value);
            if (onInputValueChange.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.OnInputValueChange), onInputValueChange.Value);
            if (open.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Open), open.Value);
            if (onOpenChange.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.OnOpenChange), onOpenChange.Value);
            if (openChanged.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.OpenChanged), openChanged.Value);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.ChildContent), CreateDefaultChildren(multiple, itemAdditionalAttributes));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultChildren(
        bool multiple = false,
        IReadOnlyDictionary<string, object>? itemAdditionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<ComboboxInput>(0);
            builder.AddAttribute(1, "placeholder", "Search");
            builder.CloseComponent();

            builder.OpenComponent<ComboboxValue>(10);
            builder.AddAttribute(11, nameof(ComboboxValue.Placeholder), "Pick a fruit");
            builder.CloseComponent();

            if (multiple)
            {
                builder.OpenComponent<ComboboxChips>(15);
                builder.AddAttribute(16, nameof(ComboboxChips.ChildContent), (RenderFragment)(chipsBuilder =>
                {
                    chipsBuilder.OpenComponent<ComboboxChip>(0);
                    chipsBuilder.AddAttribute(1, nameof(ComboboxChip.ChildContent), (RenderFragment)(b =>
                    {
                        b.AddContent(0, "Apple chip");
                        b.OpenComponent<ComboboxChipRemove>(1);
                        b.AddAttribute(2, nameof(ComboboxChipRemove.ChildContent), (RenderFragment)(removeBuilder => removeBuilder.AddContent(0, "Remove")));
                        b.CloseComponent();
                    }));
                    chipsBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }

            builder.OpenComponent<ComboboxClear>(20);
            builder.AddAttribute(21, nameof(ComboboxClear.ChildContent), (RenderFragment)(b => b.AddContent(0, "Clear")));
            builder.CloseComponent();

            builder.OpenComponent<ComboboxTrigger>(30);
            builder.AddAttribute(31, "data-testid", "combobox-trigger");
            builder.AddAttribute(32, nameof(ComboboxTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
            builder.CloseComponent();

            builder.OpenComponent<ComboboxPositioner>(40);
            builder.AddAttribute(41, nameof(ComboboxPositioner.ChildContent), (RenderFragment)(positionerBuilder =>
            {
                positionerBuilder.OpenComponent<ComboboxPopup>(0);
                positionerBuilder.AddAttribute(1, nameof(ComboboxPopup.ChildContent), (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<ComboboxList>(0);
                    popupBuilder.AddAttribute(1, nameof(ComboboxList.ChildContent), CreateListItems(itemAdditionalAttributes));
                    popupBuilder.CloseComponent();
                }));
                positionerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateListItems(IReadOnlyDictionary<string, object>? itemAdditionalAttributes = null)
    {
        return listBuilder =>
        {
            for (var index = 0; index < Fruits.Count; index++)
            {
                var fruit = Fruits[index];
                listBuilder.OpenComponent<ComboboxItem<string>>(index * 10);
                listBuilder.AddAttribute(index * 10 + 1, nameof(ComboboxItem<string>.Value), fruit);
                listBuilder.AddAttribute(index * 10 + 2, nameof(ComboboxItem<string>.Index), index);
                listBuilder.AddAttribute(index * 10 + 3, nameof(ComboboxItem<string>.AdditionalAttributes), itemAdditionalAttributes);
                listBuilder.AddAttribute(index * 10 + 4, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<ComboboxItemIndicator>(0);
                    itemBuilder.AddAttribute(1, nameof(ComboboxItemIndicator.ChildContent), (RenderFragment)(b => b.AddContent(0, "Selected")));
                    itemBuilder.CloseComponent();
                    itemBuilder.AddContent(2, fruit);
                }));
                listBuilder.CloseComponent();
            }
        };
    }

    private static RenderFragment CreatePopupInputCombobox(
        string? defaultValue,
        bool multiple = false,
        string? inputValue = null,
        EventCallback<string>? inputValueChanged = null,
        EventCallback<ComboboxInputValueChangeEventArgs>? onInputValueChange = null)
    {
        return builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.Items), Fruits);
            if (defaultValue is not null)
            {
                builder.AddAttribute(2, nameof(ComboboxRoot<string>.DefaultValue), defaultValue);
            }

            builder.AddAttribute(3, nameof(ComboboxRoot<string>.Multiple), multiple);
            if (inputValueChanged.HasValue)
            {
                builder.AddAttribute(4, nameof(ComboboxRoot<string>.InputValue), inputValue);
                builder.AddAttribute(5, nameof(ComboboxRoot<string>.InputValueChanged), inputValueChanged.Value);
            }
            if (onInputValueChange.HasValue)
            {
                builder.AddAttribute(6, nameof(ComboboxRoot<string>.OnInputValueChange), onInputValueChange.Value);
            }

            builder.AddAttribute(7, nameof(ComboboxRoot<string>.DefaultOpen), true);
            builder.AddAttribute(8, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxTrigger>(0);
                childBuilder.AddAttribute(1, nameof(ComboboxTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<ComboboxPositioner>(10);
                childBuilder.AddAttribute(11, nameof(ComboboxPositioner.ChildContent), (RenderFragment)(positionerBuilder =>
                {
                    positionerBuilder.OpenComponent<ComboboxPopup>(0);
                    positionerBuilder.AddAttribute(1, nameof(ComboboxPopup.ChildContent), (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<ComboboxInput>(0);
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<ComboboxList>(10);
                        popupBuilder.AddAttribute(11, nameof(ComboboxList.ChildContent), (RenderFragment)(listBuilder =>
                        {
                            listBuilder.OpenComponent<ComboboxItem<string>>(0);
                            listBuilder.AddAttribute(1, nameof(ComboboxItem<string>.Value), "Apple");
                            listBuilder.AddAttribute(2, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Apple")));
                            listBuilder.CloseComponent();

                            listBuilder.OpenComponent<ComboboxItem<string>>(10);
                            listBuilder.AddAttribute(11, nameof(ComboboxItem<string>.Value), "Apricot");
                            listBuilder.AddAttribute(12, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Apricot")));
                            listBuilder.CloseComponent();

                            listBuilder.OpenComponent<ComboboxItem<string>>(20);
                            listBuilder.AddAttribute(21, nameof(ComboboxItem<string>.Value), "Banana");
                            listBuilder.AddAttribute(22, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, "Banana")));
                            listBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();
                    }));
                    positionerBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public async Task InputPress_ShouldReportInputPressOpenReason()
    {
        ComboboxOpenChangeEventArgs? received = null;
        var callback = EventCallback.Factory.Create<ComboboxOpenChangeEventArgs>(this, args => received = args);
        var cut = Render(CreateCombobox(onOpenChange: callback));

        await cut.Find("input[role='combobox']").MouseDownAsync(new MouseEventArgs());

        received.ShouldNotBeNull();
        received.Open.ShouldBeTrue();
        received.Reason.ShouldBe(ComboboxChangeReason.InputPress);
    }

    [Fact]
    public Task InlineList_ShouldExposeExpandedAriaOnInput()
    {
        var cut = Render(CreateCombobox(inline: true, defaultOpen: false));

        var input = cut.Find("input[role='combobox']");
        var list = cut.Find("[role='listbox']");
        input.GetAttribute("aria-expanded").ShouldBe("true");
        input.GetAttribute("aria-haspopup").ShouldBe("listbox");
        input.GetAttribute("aria-controls").ShouldBe(list.Id);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DisabledRoot_ShouldDisableItems()
    {
        ComboboxValueChangeEventArgs<string>? received = null;
        var callback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, args => received = args);
        var cut = Render(CreateCombobox(disabled: true, defaultOpen: true, onValueChange: callback));

        var options = cut.FindAll("[role='option']");
        options.Count.ShouldBe(Fruits.Count);
        options.ShouldAllBe(option => option.HasAttribute("data-disabled"));
        options.ShouldAllBe(option => option.GetAttribute("aria-disabled") == "true");

        var banana = options.Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        await banana.ClickAsync(new MouseEventArgs());

        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe(string.Empty);
        received.ShouldBeNull();
    }

    [Fact]
    public async Task DisabledItem_ShouldStillInvokeConsumerClickHandler()
    {
        var consumerClickHandled = false;
        var valueChangeCount = 0;
        var valueCallback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, _ => valueChangeCount++);
        var itemAttributes = new Dictionary<string, object>
        {
            { "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => consumerClickHandled = true) }
        };
        var cut = Render(CreateCombobox(
            disabled: true,
            defaultOpen: true,
            onValueChange: valueCallback,
            itemAdditionalAttributes: itemAttributes));

        var banana = cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        await banana.ClickAsync(new MouseEventArgs());

        consumerClickHandled.ShouldBeTrue();
        valueChangeCount.ShouldBe(0);
    }

    [Fact]
    public async Task QueryClear_ShouldRestoreHighlightToSelectedItem()
    {
        var selectedCut = Render(CreatePopupInputCombobox("Banana"));
        var input = selectedCut.Find("input[role='combobox']");

        await input.InputAsync(new ChangeEventArgs { Value = "Ap" });

        var filteredOptions = selectedCut.FindAll("[role='option']");
        filteredOptions.Count.ShouldBe(2);
        filteredOptions.ShouldNotContain(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));

        await selectedCut.Find("input[role='combobox']").InputAsync(new ChangeEventArgs { Value = "" });

        var restoredOptions = selectedCut.FindAll("[role='option']");
        var banana = restoredOptions.Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        banana.HasAttribute("data-highlighted").ShouldBeTrue();
        selectedCut.Find("input[role='combobox']").GetAttribute("aria-activedescendant").ShouldBe(banana.Id);

        var emptyCut = Render(CreatePopupInputCombobox(null));
        await emptyCut.Find("input[role='combobox']").InputAsync(new ChangeEventArgs { Value = "Ap" });
        await emptyCut.Find("input[role='combobox']").InputAsync(new ChangeEventArgs { Value = "" });

        emptyCut.Find("input[role='combobox']").HasAttribute("aria-activedescendant").ShouldBeFalse();
        emptyCut.FindAll("[role='option']").ShouldAllBe(option => !option.HasAttribute("data-highlighted"));
    }

    [Fact]
    public async Task QueryClear_ShouldRestoreHighlightWithControlledInputValue()
    {
        var inputValue = string.Empty;
        var inputValueChanged = EventCallback.Factory.Create<string>(this, value => inputValue = value);
        var cut = Render(CreatePopupInputCombobox(
            defaultValue: null,
            inputValue: inputValue,
            inputValueChanged: inputValueChanged));
        var root = cut.FindComponent<ComboboxRoot<string>>();

        var banana = cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        await banana.ClickAsync(new MouseEventArgs());

        await cut.Find("button").MouseDownAsync(new MouseEventArgs());
        banana = cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        banana.GetAttribute("aria-selected").ShouldBe("true");

        await cut.Find("input[role='combobox']").InputAsync(new ChangeEventArgs { Value = "Ap" });
        inputValue.ShouldBe("Ap");
        root.Render(parameters => parameters
            .Add(component => component.InputValue, inputValue)
            .Add(component => component.InputValueChanged, inputValueChanged));

        var filteredOptions = cut.FindAll("[role='option']");
        filteredOptions.Count.ShouldBe(2);
        filteredOptions.ShouldNotContain(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));

        await cut.Find("input[role='combobox']").InputAsync(new ChangeEventArgs { Value = "" });
        inputValue.ShouldBe(string.Empty);
        root.Render(parameters => parameters
            .Add(component => component.InputValue, inputValue)
            .Add(component => component.InputValueChanged, inputValueChanged));

        var restoredOptions = cut.FindAll("[role='option']");
        banana = restoredOptions.Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        banana.HasAttribute("data-highlighted").ShouldBeTrue();
        cut.Find("input[role='combobox']").GetAttribute("aria-activedescendant").ShouldBe(banana.Id);
    }

    [Fact]
    public Task Input_ShouldExposeComboboxAttributesFromSelectedValue()
    {
        var cut = Render(CreateCombobox(defaultValue: "Apple", defaultOpen: true, name: "fruit", required: true));

        var input = cut.Find("input[role='combobox']");
        input.HasAttribute("type").ShouldBeFalse();
        input.GetAttribute("value").ShouldBe("Apple");
        input.GetAttribute("aria-expanded").ShouldBe("true");
        input.GetAttribute("aria-haspopup").ShouldBe("listbox");
        input.GetAttribute("aria-autocomplete").ShouldBe("list");
        input.GetAttribute("autocomplete").ShouldBe("off");
        input.GetAttribute("spellcheck").ShouldBe("false");
        input.GetAttribute("autocorrect").ShouldBe("off");
        input.GetAttribute("autocapitalize").ShouldBe("none");
        input.HasAttribute("name").ShouldBeFalse();

        var hiddenInput = cut.Find("input[aria-hidden='true']");
        hiddenInput.GetAttribute("name").ShouldBe("fruit");
        hiddenInput.GetAttribute("value").ShouldBe("Apple");
        hiddenInput.HasAttribute("required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ItemPress_ShouldSelectSingleValueAndSerializeHiddenInput()
    {
        ComboboxValueChangeEventArgs<string>? received = null;
        var callback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, args => received = args);
        var cut = Render(CreateCombobox(defaultOpen: true, name: "fruit", onValueChange: callback));

        var banana = cut.FindAll("[role='option']").Single(i => i.TextContent.Contains("Banana", StringComparison.Ordinal));
        await banana.ClickAsync(new MouseEventArgs());

        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe("Banana");
        cut.Find("input[aria-hidden='true']").GetAttribute("value").ShouldBe("Banana");
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").ShouldBe("false");
        cut.Markup.ShouldContain("Banana");

        received.ShouldNotBeNull();
        received.Value.ShouldBe("Banana");
        received.Values.ShouldBeNull();
        received.Reason.ShouldBe(ComboboxChangeReason.ItemPress);
    }

    [Fact]
    public async Task MultipleItemPress_ShouldToggleSelectedValuesAndRenderIndicators()
    {
        ComboboxValueChangeEventArgs<string>? received = null;
        var callback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, args => received = args);
        var cut = Render(CreateCombobox(defaultValues: ["Apple"], multiple: true, defaultOpen: true, name: "fruit", onValueChange: callback));

        cut.FindAll("[aria-selected='true']").Count.ShouldBe(1);
        cut.FindAll("[aria-hidden='true']").Count(element => element.TextContent == "Selected").ShouldBe(1);
        cut.FindAll("input[type='hidden'][name='fruit']").Select(input => input.GetAttribute("value")).ShouldBe(["Apple"]);

        var banana = cut.FindAll("[role='option']").Single(i => i.TextContent.Contains("Banana", StringComparison.Ordinal));
        await banana.ClickAsync(new MouseEventArgs());

        cut.FindAll("[aria-selected='true']").Count.ShouldBe(2);
        cut.FindAll("input[type='hidden'][name='fruit']").Select(input => input.GetAttribute("value")).ShouldBe(["Apple", "Banana"]);
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").ShouldBe("true");

        received.ShouldNotBeNull();
        received.Value.ShouldBeNull();
        received.Values.ShouldBe(["Apple", "Banana"]);
        received.Reason.ShouldBe(ComboboxChangeReason.ItemPress);
    }

    [Fact]
    public async Task MultipleToggle_ShouldNotClearInputWhenQueryIsEmpty()
    {
        var inputChangeReasons = new List<ComboboxChangeReason>();
        var callback = EventCallback.Factory.Create<ComboboxInputValueChangeEventArgs>(this, args => inputChangeReasons.Add(args.Reason));
        var cut = Render(CreatePopupInputCombobox(
            defaultValue: null,
            multiple: true,
            onInputValueChange: callback));

        var apple = cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Apple", StringComparison.Ordinal));
        await apple.ClickAsync(new MouseEventArgs());

        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe(string.Empty);
        inputChangeReasons.ShouldNotContain(ComboboxChangeReason.InputClear);

        await cut.Find("input[role='combobox']").InputAsync(new ChangeEventArgs { Value = "Ba" });
        var banana = cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Banana", StringComparison.Ordinal));
        await banana.ClickAsync(new MouseEventArgs());

        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe(string.Empty);
        inputChangeReasons.Count(reason => reason == ComboboxChangeReason.InputClear).ShouldBe(1);
    }

    [Fact]
    public Task Label_ShouldExposeDerivedIdAndLabelTrigger()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxLabel>(0);
                childBuilder.AddAttribute(1, "id", "ignored-id");
                childBuilder.AddAttribute(2, nameof(ComboboxLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Favorite fruit")));
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<ComboboxTrigger>(10);
                childBuilder.AddAttribute(11, nameof(ComboboxTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var label = cut.Find("div");
        label.Id.ShouldEndWith("-label");
        label.Id.ShouldNotBe("ignored-id");
        label.TextContent.ShouldBe("Favorite fruit");

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-labelledby").ShouldBe(label.Id);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ChipRemove_ShouldRemoveValueAtResolvedChipIndex()
    {
        ComboboxValueChangeEventArgs<string>? received = null;
        var callback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, args => received = args);
        var cut = Render(CreateCombobox(defaultValues: ["Apple", "Banana"], multiple: true, name: "fruit", onValueChange: callback));

        var remove = cut.FindAll("button").Single(button => button.TextContent.Contains("Remove", StringComparison.Ordinal));
        await remove.ClickAsync(new MouseEventArgs());

        cut.FindAll("input[type='hidden'][name='fruit']").Select(input => input.GetAttribute("value")).ShouldBe(["Banana"]);
        received.ShouldNotBeNull();
        received.Values.ShouldBe(["Banana"]);
        received.Reason.ShouldBe(ComboboxChangeReason.ChipRemovePress);
    }

    [Fact]
    public async Task NonNativeChipRemove_ShouldRemoveValueOnKeyboardActivation()
    {
        ComboboxValueChangeEventArgs<string>? received = null;
        var callback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, args => received = args);
        var cut = Render(builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.DefaultValues), new[] { "Apple", "Banana" });
            builder.AddAttribute(2, nameof(ComboboxRoot<string>.Multiple), true);
            builder.AddAttribute(3, nameof(ComboboxRoot<string>.Name), "fruit");
            builder.AddAttribute(4, nameof(ComboboxRoot<string>.OnValueChange), callback);
            builder.AddAttribute(5, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxChips>(0);
                childBuilder.AddAttribute(1, nameof(ComboboxChips.ChildContent), (RenderFragment)(chipsBuilder =>
                {
                    chipsBuilder.OpenComponent<ComboboxChip>(0);
                    chipsBuilder.AddAttribute(1, nameof(ComboboxChip.ChildContent), (RenderFragment)(chipBuilder =>
                    {
                        chipBuilder.AddContent(0, "Apple chip");
                        chipBuilder.OpenComponent<ComboboxChipRemove>(1);
                        chipBuilder.AddAttribute(2, nameof(ComboboxChipRemove.NativeButton), false);
                        chipBuilder.AddAttribute(3, nameof(ComboboxChipRemove.ChildContent), (RenderFragment)(removeBuilder => removeBuilder.AddContent(0, "Remove")));
                        chipBuilder.CloseComponent();
                    }));
                    chipsBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var remove = cut.Find("[role='button']");
        await remove.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        cut.FindAll("input[type='hidden'][name='fruit']").Select(input => input.GetAttribute("value")).ShouldBe(["Banana"]);
        received.ShouldNotBeNull();
        received.Values.ShouldBe(["Banana"]);
        received.Reason.ShouldBe(ComboboxChangeReason.ChipRemovePress);
    }

    [Fact]
    public async Task Clear_ShouldClearSelectedValueAndInputValue()
    {
        ComboboxValueChangeEventArgs<string>? valueChange = null;
        ComboboxInputValueChangeEventArgs? inputValueChange = null;
        var valueCallback = EventCallback.Factory.Create<ComboboxValueChangeEventArgs<string>>(this, args => valueChange = args);
        var inputCallback = EventCallback.Factory.Create<ComboboxInputValueChangeEventArgs>(this, args => inputValueChange = args);
        var cut = Render(CreateCombobox(defaultValue: "Apple", defaultInputValue: "App", name: "fruit", onValueChange: valueCallback, onInputValueChange: inputCallback));

        var clear = cut.Find("button");
        clear.TextContent.ShouldBe("Clear");
        clear.HasAttribute("data-visible").ShouldBeTrue();

        await clear.ClickAsync(new MouseEventArgs());

        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe(string.Empty);
        cut.Find("input[aria-hidden='true']").GetAttribute("value").ShouldBe(string.Empty);
        cut.Markup.ShouldContain("Pick a fruit");

        valueChange.ShouldNotBeNull();
        valueChange.Value.ShouldBeNull();
        valueChange.Reason.ShouldBe(ComboboxChangeReason.ClearPress);
        inputValueChange.ShouldNotBeNull();
        inputValueChange.Value.ShouldBe(string.Empty);
        inputValueChange.Reason.ShouldBe(ComboboxChangeReason.ClearPress);
    }

    [Fact]
    public Task Clear_ShouldBeDisabledWhenRootIsReadOnly()
    {
        var cut = Render(CreateCombobox(defaultValue: "Apple", defaultInputValue: "App", readOnly: true));

        var clear = cut.Find("button");
        clear.GetAttribute("disabled").ShouldBe(string.Empty);
        clear.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task MultipleHiddenInputs_ShouldBeDisabledWhenRootIsDisabled()
    {
        var cut = Render(CreateCombobox(defaultValues: ["Apple", "Banana"], multiple: true, disabled: true, name: "fruit"));

        var hiddenInputs = cut.FindAll("input[type='hidden'][name='fruit']");
        hiddenInputs.Count.ShouldBe(2);
        hiddenInputs.ShouldAllBe(input => input.HasAttribute("disabled"));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_ShouldRenderSelectedLabelsAndPlaceholder()
    {
        var empty = Render(CreateCombobox());
        empty.Markup.ShouldContain("Pick a fruit");

        var single = Render(CreateCombobox(defaultValue: "Apple"));
        single.Markup.ShouldContain("Apple");

        var multiple = Render(CreateCombobox(defaultValues: ["Apple", "Banana"], multiple: true));
        multiple.Markup.ShouldContain("Apple, Banana");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Portal_ShouldNotRenderPopupContentWhenClosedWithoutKeepMounted()
    {
        var cut = Render(CreateComboboxWithPortal(keepMounted: false));

        cut.FindAll("[data-testid='combobox-positioner']").ShouldBeEmpty();
        cut.FindAll("[data-testid='combobox-popup']").ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Portal_ShouldKeepPopupContentMountedWhenKeepMounted()
    {
        var cut = Render(CreateComboboxWithPortal(keepMounted: true));

        var positioner = cut.Find("[data-testid='combobox-positioner']");
        positioner.HasAttribute("hidden").ShouldBeTrue();

        var popup = cut.Find("[data-testid='combobox-popup']");
        popup.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ObjectValues_ShouldUseLabelForInputAndValueForHiddenInput()
    {
        var selected = new FruitOption("apple-id", "Apple");
        var cut = Render(builder =>
        {
            builder.OpenComponent<ComboboxRoot<FruitOption>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<FruitOption>.DefaultValue), selected);
            builder.AddAttribute(2, nameof(ComboboxRoot<FruitOption>.Name), "fruit");
            builder.AddAttribute(3, nameof(ComboboxRoot<FruitOption>.ItemToStringLabel), (Func<FruitOption?, string?>)(item => item?.Label));
            builder.AddAttribute(4, nameof(ComboboxRoot<FruitOption>.ItemToStringValue), (Func<FruitOption?, string?>)(item => item?.Value));
            builder.AddAttribute(5, nameof(ComboboxRoot<FruitOption>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxInput>(0);
                childBuilder.CloseComponent();
                childBuilder.OpenComponent<ComboboxValue>(10);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe("Apple");
        cut.Find("input[aria-hidden='true']").GetAttribute("value").ShouldBe("apple-id");
        cut.Markup.ShouldContain("Apple");

        return Task.CompletedTask;
    }

    private static RenderFragment CreateComboboxWithPortal(bool keepMounted)
    {
        return builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxPortal>(0);
                childBuilder.AddAttribute(1, nameof(ComboboxPortal.KeepMounted), keepMounted);
                childBuilder.AddAttribute(2, nameof(ComboboxPortal.ChildContent), (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<ComboboxPositioner>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "combobox-positioner");
                    portalBuilder.AddAttribute(2, nameof(ComboboxPositioner.ChildContent), (RenderFragment)(positionerBuilder =>
                    {
                        positionerBuilder.OpenComponent<ComboboxPopup>(0);
                        positionerBuilder.AddAttribute(1, "data-testid", "combobox-popup");
                        positionerBuilder.AddAttribute(2, nameof(ComboboxPopup.ChildContent), (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<ComboboxList>(0);
                            popupBuilder.AddAttribute(1, nameof(ComboboxList.ChildContent), CreateListItems());
                            popupBuilder.CloseComponent();
                        }));
                        positionerBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task StaticItems_ShouldUseCustomFilterForListEmptyState()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.DefaultInputValue), "ap");
            builder.AddAttribute(2, nameof(ComboboxRoot<string>.DefaultOpen), true);
            builder.AddAttribute(3, nameof(ComboboxRoot<string>.Filter), (Func<string, string, Func<string, string?>?, bool>)((_, _, _) => false));
            builder.AddAttribute(4, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxList>(0);
                childBuilder.AddAttribute(1, nameof(ComboboxList.ChildContent), (RenderFragment)(listBuilder =>
                {
                    listBuilder.OpenComponent<ComboboxItem<string>>(0);
                    listBuilder.AddAttribute(1, nameof(ComboboxItem<string>.Value), "Apple");
                    listBuilder.AddAttribute(2, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(itemBuilder => itemBuilder.AddContent(0, "Apple")));
                    listBuilder.CloseComponent();

                    listBuilder.OpenComponent<ComboboxItem<string>>(10);
                    listBuilder.AddAttribute(11, nameof(ComboboxItem<string>.Value), "Banana");
                    listBuilder.AddAttribute(12, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(itemBuilder => itemBuilder.AddContent(0, "Banana")));
                    listBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.FindAll("[role='option']").ShouldBeEmpty();
        cut.Find("[role='listbox']").HasAttribute("data-empty").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ObjectValues_ShouldUseCustomEqualityForSelectedItems()
    {
        var selected = new FruitOption("apple-id", "Apple");
        var itemValue = new FruitOption("apple-id", "Apple clone");
        var cut = Render(builder =>
        {
            builder.OpenComponent<ComboboxRoot<FruitOption>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<FruitOption>.DefaultValue), selected);
            builder.AddAttribute(2, nameof(ComboboxRoot<FruitOption>.DefaultOpen), true);
            builder.AddAttribute(3, nameof(ComboboxRoot<FruitOption>.IsItemEqualToValue), (Func<FruitOption, FruitOption, bool>)((item, value) => item.Value == value.Value));
            builder.AddAttribute(4, nameof(ComboboxRoot<FruitOption>.ItemToStringLabel), (Func<FruitOption?, string?>)(item => item?.Label));
            builder.AddAttribute(5, nameof(ComboboxRoot<FruitOption>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxList>(0);
                childBuilder.AddAttribute(1, nameof(ComboboxList.ChildContent), (RenderFragment)(listBuilder =>
                {
                    listBuilder.OpenComponent<ComboboxItem<FruitOption>>(0);
                    listBuilder.AddAttribute(1, nameof(ComboboxItem<FruitOption>.Value), itemValue);
                    listBuilder.AddAttribute(2, nameof(ComboboxItem<FruitOption>.ChildContent), (RenderFragment)(itemBuilder =>
                    {
                        itemBuilder.OpenComponent<ComboboxItemIndicator>(0);
                        itemBuilder.CloseComponent();
                        itemBuilder.AddContent(1, "Apple clone");
                    }));
                    listBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("[role='option']");
        item.GetAttribute("aria-selected").ShouldBe("true");
        item.HasAttribute("data-selected").ShouldBeTrue();
        cut.Markup.ShouldContain("Selected");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GroupedFiltering_ShouldStopAfterGlobalLimit()
    {
        var groups = new[]
        {
            new ComboboxOptionGroup<string>(["Apple", "Apricot", "Avocado"], "A"),
            new ComboboxOptionGroup<string>(["Banana", "Blackberry"], "B")
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.ItemGroups), groups);
            builder.AddAttribute(2, nameof(ComboboxRoot<string>.DefaultInputValue), "a");
            builder.AddAttribute(3, nameof(ComboboxRoot<string>.Limit), 2);
            builder.AddAttribute(4, nameof(ComboboxRoot<string>.DefaultOpen), true);
            builder.AddAttribute(5, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxList>(0);
                childBuilder.AddAttribute(1, nameof(ComboboxList.ChildContent), (RenderFragment)(listBuilder =>
                {
                    listBuilder.OpenComponent<ComboboxCollection<string>>(0);
                    listBuilder.AddAttribute(1, nameof(ComboboxCollection<string>.ChildContent), (RenderFragment<ComboboxCollectionItem<string>>)(entry => itemBuilder =>
                    {
                        itemBuilder.OpenComponent<ComboboxItem<string>>(0);
                        itemBuilder.AddAttribute(1, nameof(ComboboxItem<string>.Value), entry.Item);
                        itemBuilder.AddAttribute(2, nameof(ComboboxItem<string>.Index), entry.Index);
                        itemBuilder.AddAttribute(3, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(b => b.AddContent(0, entry.Item)));
                        itemBuilder.CloseComponent();
                    }));
                    listBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.FindAll("[role='option']").Select(item => item.TextContent).ShouldBe(["Apple", "Apricot"]);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HiddenInputChange_ShouldBeIgnoredWhenReadOnly()
    {
        var cut = Render(CreateCombobox(defaultValue: "Apple", readOnly: true, name: "fruit"));

        var hiddenInput = cut.Find("input[aria-hidden='true']");
        await hiddenInput.TriggerEventAsync("onchange", new ChangeEventArgs { Value = "Banana" });

        cut.Find("input[aria-hidden='true']").GetAttribute("value").ShouldBe("Apple");
        cut.Find("input[role='combobox']").GetAttribute("value").ShouldBe("Apple");
    }

    [Fact]
    public async Task CancelOpen_ShouldDiscardPendingOpenAwaitingOnOpenChange()
    {
        var gate = new TaskCompletionSource();
        var openStates = new List<bool>();
        var onOpenChange = EventCallback.Factory.Create<ComboboxOpenChangeEventArgs>(this, _ => gate.Task);
        var openChanged = EventCallback.Factory.Create<bool>(this, value => openStates.Add(value));
        var cut = Render(CreateCombobox(onOpenChange: onOpenChange, openChanged: openChanged));
        var root = cut.FindComponent<ComboboxRoot<string>>().Instance;

        var pendingOpen = cut.Find("[data-testid='combobox-trigger']").TriggerEventAsync("onmousedown", new MouseEventArgs { Button = 0 });
        await cut.InvokeAsync(() => root.OnCancelOpen());

        gate.SetResult();
        await pendingOpen;

        openStates.ShouldBeEmpty();
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public async Task CancelOpen_ShouldDiscardPendingOpenAwaitingOnOpenChangeWhenControlled()
    {
        var gate = new TaskCompletionSource();
        var openStates = new List<bool>();
        var onOpenChange = EventCallback.Factory.Create<ComboboxOpenChangeEventArgs>(this, _ => gate.Task);
        var openChanged = EventCallback.Factory.Create<bool>(this, value => openStates.Add(value));
        var cut = Render(CreateCombobox(open: false, onOpenChange: onOpenChange, openChanged: openChanged));
        var root = cut.FindComponent<ComboboxRoot<string>>().Instance;

        var pendingOpen = cut.Find("[data-testid='combobox-trigger']").TriggerEventAsync("onmousedown", new MouseEventArgs { Button = 0 });
        await cut.InvokeAsync(() => root.OnCancelOpen());

        gate.SetResult();
        await pendingOpen;

        openStates.ShouldBeEmpty();
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public async Task EscapeKey_ShouldDiscardPendingOpenAwaitingOnOpenChange()
    {
        var gate = new TaskCompletionSource();
        var openStates = new List<bool>();
        var onOpenChange = EventCallback.Factory.Create<ComboboxOpenChangeEventArgs>(this, _ => gate.Task);
        var openChanged = EventCallback.Factory.Create<bool>(this, value => openStates.Add(value));
        var cut = Render(CreateCombobox(onOpenChange: onOpenChange, openChanged: openChanged));
        var root = cut.FindComponent<ComboboxRoot<string>>().Instance;

        var pendingOpen = cut.Find("[data-testid='combobox-trigger']").TriggerEventAsync("onmousedown", new MouseEventArgs { Button = 0 });
        await cut.InvokeAsync(() => root.OnEscapeKey());

        gate.SetResult();
        await pendingOpen;

        openStates.ShouldBeEmpty();
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public async Task Label_ShouldKeepTriggerAssociationWhenLabelIsReplaced()
    {
        var cut = Render<ComponentSwapHost>(ps => ps.Add(p => p.Content, swapped => builder =>
        {
            builder.OpenComponent<ComboboxRoot<string>>(0);
            builder.AddAttribute(1, nameof(ComboboxRoot<string>.Items), Fruits);
            builder.AddAttribute(2, nameof(ComboboxRoot<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ComboboxLabel>(0);
                childBuilder.SetKey(swapped ? "second" : "first");
                childBuilder.AddAttribute(1, nameof(ComboboxLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Favorite fruit")));
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<ComboboxTrigger>(10);
                childBuilder.AddAttribute(11, nameof(ComboboxTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }));

        // The label id is derived from the root id, so the replacement registers the same value the
        // outgoing instance is about to clear.
        var labelId = cut.Find("button").GetAttribute("aria-labelledby");
        labelId.ShouldNotBeNullOrEmpty();

        await cut.InvokeAsync(() => cut.Instance.Swap());

        cut.WaitForAssertion(() =>
            cut.Find("button").GetAttribute("aria-labelledby").ShouldBe(labelId));
    }

    [Fact]
    public async Task GroupLabel_ShouldKeepGroupAssociationWhenSupersededLabelUnmounts()
    {
        var cut = Render<ComponentSwapHost>(ps => ps.Add(p => p.Content, removeFirst => builder =>
        {
            builder.OpenComponent<ComboboxGroup>(0);
            builder.AddAttribute(1, nameof(ComboboxGroup.ChildContent), (RenderFragment)(groupBuilder =>
            {
                if (!removeFirst)
                {
                    groupBuilder.OpenComponent<ComboboxGroupLabel>(0);
                    groupBuilder.SetKey("first");
                    groupBuilder.AddAttribute(1, nameof(ComboboxGroupLabel.Id), "label-a");
                    groupBuilder.CloseComponent();
                }

                groupBuilder.OpenComponent<ComboboxGroupLabel>(10);
                groupBuilder.SetKey("second");
                groupBuilder.AddAttribute(11, nameof(ComboboxGroupLabel.Id), "label-b");
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }));

        cut.WaitForAssertion(() =>
            cut.Find("[role='group']").GetAttribute("aria-labelledby").ShouldBe("label-b"));

        await cut.InvokeAsync(() => cut.Instance.Swap());

        cut.WaitForAssertion(() =>
            cut.Find("[role='group']").GetAttribute("aria-labelledby").ShouldBe("label-b"));
    }
}
