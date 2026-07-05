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
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        EventCallback<ComboboxValueChangeEventArgs<string>>? onValueChange = null,
        EventCallback<ComboboxInputValueChangeEventArgs>? onInputValueChange = null)
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
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Disabled), disabled);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.ReadOnly), readOnly);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Required), required);
            if (name is not null) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.Name), name);
            if (onValueChange.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.OnValueChange), onValueChange.Value);
            if (onInputValueChange.HasValue) builder.AddAttribute(i++, nameof(ComboboxRoot<string>.OnInputValueChange), onInputValueChange.Value);
            builder.AddAttribute(i++, nameof(ComboboxRoot<string>.ChildContent), CreateDefaultChildren(multiple));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultChildren(bool multiple = false)
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
            builder.AddAttribute(31, nameof(ComboboxTrigger.ChildContent), (RenderFragment)(b => b.AddContent(0, "Toggle")));
            builder.CloseComponent();

            builder.OpenComponent<ComboboxPositioner>(40);
            builder.AddAttribute(41, nameof(ComboboxPositioner.ChildContent), (RenderFragment)(positionerBuilder =>
            {
                positionerBuilder.OpenComponent<ComboboxPopup>(0);
                positionerBuilder.AddAttribute(1, nameof(ComboboxPopup.ChildContent), (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<ComboboxList>(0);
                    popupBuilder.AddAttribute(1, nameof(ComboboxList.ChildContent), CreateListItems());
                    popupBuilder.CloseComponent();
                }));
                positionerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateListItems()
    {
        return listBuilder =>
        {
            for (var index = 0; index < Fruits.Count; index++)
            {
                var fruit = Fruits[index];
                listBuilder.OpenComponent<ComboboxItem<string>>(index * 10);
                listBuilder.AddAttribute(index * 10 + 1, nameof(ComboboxItem<string>.Value), fruit);
                listBuilder.AddAttribute(index * 10 + 2, nameof(ComboboxItem<string>.Index), index);
                listBuilder.AddAttribute(index * 10 + 3, nameof(ComboboxItem<string>.ChildContent), (RenderFragment)(itemBuilder =>
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
}
