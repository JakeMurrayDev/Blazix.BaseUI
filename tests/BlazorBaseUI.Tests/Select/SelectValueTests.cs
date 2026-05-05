namespace BlazorBaseUI.Tests.Select;

public class SelectValueTests : BunitContext, ISelectValueContract
{
    public SelectValueTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectWithValue(
        string? defaultValue = null,
        string? placeholder = null,
        Func<string?, string?>? itemToStringLabel = null,
        RenderFragment<string?>? valueContent = null,
        RenderFragment? childContent = null,
        bool multiple = false,
        IReadOnlyList<string>? defaultValues = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            builder.AddAttribute(i++, "Multiple", multiple);
            if (defaultValues is not null) builder.AddAttribute(i++, "DefaultValues", defaultValues);
            if (itemToStringLabel is not null) builder.AddAttribute(i++, "ItemToStringLabel", itemToStringLabel);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<string>>(0);
                    var vi = 1;
                    if (placeholder is not null) triggerBuilder.AddAttribute(vi++, "Placeholder", placeholder);
                    if (valueContent is not null) triggerBuilder.AddAttribute(vi++, "ValueContent", valueContent);
                    if (childContent is not null) triggerBuilder.AddAttribute(vi++, "ChildContent", childContent);
                    triggerBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task ValueContent_AcceptsFunctionWithValueParameter()
    {
        RenderFragment<string?> valueContent = value => builder => builder.AddContent(0, $"Selected: {value}");

        var cut = Render(CreateSelectWithValue(defaultValue: "apple", valueContent: valueContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldContain("Selected: apple");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ChildContent_OverridesTextWhenProvided()
    {
        RenderFragment childContent = builder => builder.AddContent(0, "Custom Text");

        var cut = Render(CreateSelectWithValue(defaultValue: "apple", childContent: childContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Custom Text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisplaysLabelFromItemToStringLabel()
    {
        var cut = Render(CreateSelectWithValue(
            defaultValue: "apple",
            itemToStringLabel: v => $"Fruit: {v}"));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Fruit: apple");

        return Task.CompletedTask;
    }

    [Fact]
    public Task FallsBackToValueToString()
    {
        var cut = Render(CreateSelectWithValue(defaultValue: "apple"));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("apple");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_DisplaysWhenNoValueSelected()
    {
        var cut = Render(CreateSelectWithValue(placeholder: "Pick one..."));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Pick one...");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_EmitsDataPlaceholderAttribute()
    {
        var cut = Render(CreateSelectWithValue(placeholder: "Pick one..."));

        var span = cut.Find("button span");
        span.HasAttribute("data-placeholder").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_DoesNotDisplayWhenValueSelected()
    {
        var cut = Render(CreateSelectWithValue(defaultValue: "apple", placeholder: "Pick one..."));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("apple");
        span.TextContent.ShouldNotContain("Pick one...");
        span.HasAttribute("data-placeholder").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_DisplaysCommaSeparatedLabels()
    {
        var cut = Render(CreateSelectWithValue(
            multiple: true,
            defaultValues: new[] { "apple", "banana" }));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("apple, banana");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_DisplaysEmptyWhenNoValuesSelected()
    {
        var cut = Render(CreateSelectWithValue(
            multiple: true,
            placeholder: "Select items"));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Select items");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_DisplaysSingleValueWhenOneSelected()
    {
        var cut = Render(CreateSelectWithValue(
            multiple: true,
            defaultValues: new[] { "apple" }));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("apple");

        return Task.CompletedTask;
    }

    // --- New helper for full select with items ---

    private RenderFragment CreateSelectWithValueAndItems(
        string? defaultValue = null,
        string? value = null,
        string? placeholder = null,
        Func<string?, string?>? itemToStringLabel = null,
        RenderFragment<string?>? valueContent = null,
        RenderFragment? childContent = null,
        bool multiple = false,
        IReadOnlyList<string>? defaultValues = null,
        RenderFragment<IReadOnlyList<string>>? valuesContent = null,
        EventCallback<string?>? valueChanged = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            if (value is not null) builder.AddAttribute(i++, "Value", value);
            builder.AddAttribute(i++, "Multiple", multiple);
            if (defaultValues is not null) builder.AddAttribute(i++, "DefaultValues", defaultValues);
            if (itemToStringLabel is not null) builder.AddAttribute(i++, "ItemToStringLabel", itemToStringLabel);
            if (valueChanged.HasValue) builder.AddAttribute(i++, "ValueChanged", valueChanged.Value);
            builder.AddAttribute(i++, "DefaultOpen", true);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<string>>(0);
                    var vi = 1;
                    if (placeholder is not null) triggerBuilder.AddAttribute(vi++, "Placeholder", placeholder);
                    if (valueContent is not null) triggerBuilder.AddAttribute(vi++, "ValueContent", valueContent);
                    if (childContent is not null) triggerBuilder.AddAttribute(vi++, "ChildContent", childContent);
                    if (valuesContent is not null) triggerBuilder.AddAttribute(vi++, "ValuesContent", valuesContent);
                    triggerBuilder.CloseComponent();
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

    // --- Dynamic update ---

    [Fact]
    public Task Value_DisplaysCorrectTextForDifferentValues()
    {
        var cut = Render(CreateSelectWithValueAndItems(
            value: "apple",
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("apple");

        // Render a separate instance with a different value to verify correct text
        var cut2 = Render(CreateSelectWithValueAndItems(
            value: "banana",
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var span2 = cut2.Find("button span");
        span2.TextContent.ShouldBe("banana");

        return Task.CompletedTask;
    }

    // --- Placeholder precedence ---

    [Fact]
    public Task Placeholder_ChildContentTakesPrecedenceOverPlaceholder()
    {
        RenderFragment childContent = b => b.AddContent(0, "Custom child");

        var cut = Render(CreateSelectWithValue(
            placeholder: "Pick one...",
            childContent: childContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Custom child");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_ValueContentTakesPrecedenceOverPlaceholder()
    {
        RenderFragment<string?> valueContent = value => builder =>
            builder.AddContent(0, value is null ? "Nothing" : $"Got: {value}");

        var cut = Render(CreateSelectWithValue(
            placeholder: "Pick one...",
            valueContent: valueContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Nothing");

        return Task.CompletedTask;
    }

    // --- Multiple + callback ---

    [Fact]
    public Task Multiple_ValuesContentReceivesArrayOfValues()
    {
        IReadOnlyList<string>? receivedValues = null;
        RenderFragment<IReadOnlyList<string>> valuesContent = values =>
        {
            receivedValues = values;
            return builder => builder.AddContent(0, $"Count: {values.Count}");
        };

        var cut = Render(CreateSelectWithValueAndItems(
            multiple: true,
            defaultValues: new[] { "apple", "banana" },
            valuesContent: valuesContent));

        receivedValues.ShouldNotBeNull();
        receivedValues!.Count.ShouldBe(2);
        receivedValues.ShouldContain("apple");
        receivedValues.ShouldContain("banana");

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Count: 2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_ChildContentTakesPrecedenceOverItems()
    {
        RenderFragment childContent = b => b.AddContent(0, "Custom multi");

        var cut = Render(CreateSelectWithValueAndItems(
            multiple: true,
            defaultValues: new[] { "apple", "banana" },
            childContent: childContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Custom multi");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Multiple_DefaultsToEmptyArrayWhenNoValueProvided()
    {
        IReadOnlyList<string>? receivedValues = null;
        RenderFragment<IReadOnlyList<string>> valuesContent = values =>
        {
            receivedValues = values;
            return builder => builder.AddContent(0, $"Count: {values.Count}");
        };

        var cut = Render(CreateSelectWithValueAndItems(
            multiple: true,
            valuesContent: valuesContent));

        receivedValues.ShouldNotBeNull();
        receivedValues!.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    // --- Parity with React source ---

    [Fact]
    public Task Placeholder_ChildContentBeatsPlaceholderWhenMultiSelectEmpty()
    {
        RenderFragment childContent = b => b.AddContent(0, "Custom empty");

        var cut = Render(CreateSelectWithValue(
            multiple: true,
            placeholder: "Pick some...",
            childContent: childContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Custom empty");

        return Task.CompletedTask;
    }

    [Fact]
    public Task PlaceholderContent_RendersWhenProvidedAndNoValue()
    {
        RenderFragment placeholderContent = b =>
        {
            b.OpenElement(0, "em");
            b.AddContent(1, "Choose one");
            b.CloseElement();
        };

        var cut = Render(CreateValueWithPlaceholderContent(placeholderContent: placeholderContent));

        var em = cut.Find("button span em");
        em.TextContent.ShouldBe("Choose one");

        return Task.CompletedTask;
    }

    [Fact]
    public Task PlaceholderContent_TakesPrecedenceOverTextPlaceholder()
    {
        RenderFragment placeholderContent = b => b.AddContent(0, "Rich!");

        var cut = Render(CreateValueWithPlaceholderContent(
            placeholder: "Plain text",
            placeholderContent: placeholderContent));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Rich!");
        span.TextContent.ShouldNotContain("Plain text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NullItemLabel_SuppressesPlaceholderWhenNoValueSelected()
    {
        IReadOnlyList<SelectOption<string?>> items = new[]
        {
            new SelectOption<string?>(null, "None"),
            new SelectOption<string?>("apple", "Apple")
        };

        var cut = Render(CreateNullableValueWithItems(
            placeholder: "Pick one...",
            items: items));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("None");
        span.TextContent.ShouldNotContain("Pick one...");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetLabel_ResolvesFromItemGroups()
    {
        IReadOnlyList<SelectOptionGroup<string>> groups = new[]
        {
            new SelectOptionGroup<string>(
                "Fruit",
                new[] { new SelectOption<string>("apple", "Apple"), new SelectOption<string>("banana", "Banana") }),
            new SelectOptionGroup<string>(
                "Veggies",
                new[] { new SelectOption<string>("carrot", "Carrot") })
        };

        var cut = Render(CreateStringValueWithGroups(defaultValue: "carrot", groups: groups));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Carrot");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetLabel_ResolvesFromISelectItemLabelOnValue()
    {
        var value = new LabeledItem("apple", "Apple!");
        var cut = Render(CreateLabeledItemValue(defaultValue: value));

        var span = cut.Find("button span");
        span.TextContent.ShouldBe("Apple!");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_RegistersSpanElementWithRootContext()
    {
        ISelectRootContext? captured = null;

        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<SelectTrigger>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<string>>(0);
                    triggerBuilder.AddAttribute(1, "Placeholder", "-");
                    triggerBuilder.CloseComponent();

                    triggerBuilder.OpenComponent<RootContextSpy>(10);
                    triggerBuilder.AddComponentReferenceCapture(11, inst => captured = ((RootContextSpy)inst).Context);
                    triggerBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Render(fragment);

        captured.ShouldNotBeNull();
        captured!.GetValueElement().ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // --- Helpers for parity tests ---

    private static RenderFragment CreateValueWithPlaceholderContent(
        string? placeholder = null,
        RenderFragment? placeholderContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<SelectTrigger>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<string>>(0);
                    var vi = 1;
                    if (placeholder is not null) triggerBuilder.AddAttribute(vi++, "Placeholder", placeholder);
                    if (placeholderContent is not null) triggerBuilder.AddAttribute(vi++, "PlaceholderContent", placeholderContent);
                    triggerBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateNullableValueWithItems(
        string? placeholder = null,
        IReadOnlyList<SelectOption<string?>>? items = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string?>>(0);
            var i = 1;
            if (items is not null) builder.AddAttribute(i++, "Items", items);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<SelectTrigger>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<string?>>(0);
                    if (placeholder is not null) triggerBuilder.AddAttribute(1, "Placeholder", placeholder);
                    triggerBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateStringValueWithGroups(
        string? defaultValue = null,
        IReadOnlyList<SelectOptionGroup<string>>? groups = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            if (groups is not null) builder.AddAttribute(i++, "ItemGroups", groups);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<SelectTrigger>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<string>>(0);
                    triggerBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateLabeledItemValue(LabeledItem? defaultValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<LabeledItem>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<SelectTrigger>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder =>
                {
                    triggerBuilder.OpenComponent<SelectValue<LabeledItem>>(0);
                    triggerBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private sealed record LabeledItem(string Value, string Label) : ISelectItemLabel;

    internal sealed class RootContextSpy : ComponentBase
    {
        [CascadingParameter]
        public ISelectRootContext? Context { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
        }
    }
}
