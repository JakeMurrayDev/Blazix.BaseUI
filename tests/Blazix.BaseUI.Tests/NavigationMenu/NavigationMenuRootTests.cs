namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuRootTests : BunitContext, INavigationMenuRootContract
{
    private readonly BunitJSModuleInterop navigationMenuModule;

    public NavigationMenuRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        navigationMenuModule = JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateNavRoot(
        string? value = null,
        string? defaultValue = null,
        NavigationMenuOrientation orientation = NavigationMenuOrientation.Horizontal,
        EventCallback<NavigationMenuValueChangeEventArgs>? onValueChange = null,
        bool includeItems = true)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            if (onValueChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnValueChange", onValueChange.Value);

            if (includeItems)
                builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent());
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent()
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuList>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(listBuilder =>
            {
                // Item 1
                listBuilder.OpenComponent<NavigationMenuItem>(0);
                listBuilder.AddAttribute(1, "Value", "item1");
                listBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<NavigationMenuContent>(2);
                    itemBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    itemBuilder.CloseComponent();
                }));
                listBuilder.CloseComponent();

                // Item 2
                listBuilder.OpenComponent<NavigationMenuItem>(4);
                listBuilder.AddAttribute(5, "Value", "item2");
                listBuilder.AddAttribute(6, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 2")));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<NavigationMenuContent>(2);
                    itemBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                    itemBuilder.CloseComponent();
                }));
                listBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersNavByDefault()
    {
        var cut = Render(CreateNavRoot());

        var nav = cut.Find("nav");
        nav.TagName.ShouldBe("NAV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersDivWhenNested()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuRoot>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Nested")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        // The outer should be nav, the inner should be div
        var navElements = cut.FindAll("nav");
        navElements.Count.ShouldBe(1);

        var divs = cut.FindAll("div");
        divs.ShouldNotBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersDataOrientation()
    {
        var cut = Render(CreateNavRoot(orientation: NavigationMenuOrientation.Vertical));

        var nav = cut.Find("nav");
        nav.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "data-testid", "my-nav");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
            builder.CloseComponent();
        });

        var nav = cut.Find("nav");
        nav.GetAttribute("data-testid").ShouldBe("my-nav");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ClassValue", (Func<NavigationMenuRootState, string>)(_ => "my-class"));
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
            builder.CloseComponent();
        });

        var nav = cut.Find("nav");
        nav.GetAttribute("class")!.ShouldContain("my-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "StyleValue", (Func<NavigationMenuRootState, string>)(_ => "color: red"));
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
            builder.CloseComponent();
        });

        var nav = cut.Find("nav");
        nav.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContext()
    {
        NavigationMenuTriggerState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "DefaultValue", "item1");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuItem>(0);
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    itemBuilder.AddAttribute(1, "ClassValue", (Func<NavigationMenuTriggerState, string>)(state =>
                    {
                        capturedState = state;
                        return "trigger-class";
                    }));
                    itemBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Open.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledDefaultValue()
    {
        var cut = Render(CreateNavRoot(defaultValue: "item1"));

        var trigger = cut.Find("button[id='nav-trigger-item1']");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledValue()
    {
        var cut = Render(CreateNavRoot(value: "item1"));

        var trigger = cut.Find("button[id='nav-trigger-item1']");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        var trigger2 = cut.Find("button[id='nav-trigger-item2']");
        trigger2.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledNullValueDoesNotFallbackToDefaultValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "Value", (string?)null);
            builder.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<string?>(this, _ => { }));
            builder.AddAttribute(3, "DefaultValue", "item1");
            builder.AddAttribute(4, "ChildContent", CreateChildContent());
            builder.CloseComponent();
        });

        var trigger = cut.Find("button[id='nav-trigger-item1']");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledParameterChangeSyncsJavaScriptRootValue()
    {
        string? value = null;
        var valueChanged = EventCallback.Factory.Create<string?>(this, next => value = next);

        var cut = Render<NavigationMenuRoot>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, valueChanged)
            .Add(p => p.ChildContent, CreateChildContent()));

        cut.WaitForAssertion(() =>
        {
            navigationMenuModule.Invocations
                .Any(invocation => invocation.Identifier == "initializeRoot")
                .ShouldBeTrue();
        });

        var setRootValueCountBefore = navigationMenuModule.Invocations
            .Count(invocation =>
                invocation.Identifier == "setRootValue" &&
                invocation.Arguments.Count > 1 &&
                Equals(invocation.Arguments[1], "item1"));

        cut.Render(parameters => parameters
            .Add(p => p.Value, "item1")
            .Add(p => p.ValueChanged, valueChanged)
            .Add(p => p.ChildContent, CreateChildContent()));

        cut.WaitForAssertion(() =>
        {
            var setRootValueCountAfter = navigationMenuModule.Invocations
                .Count(invocation =>
                    invocation.Identifier == "setRootValue" &&
                    invocation.Arguments.Count > 1 &&
                    Equals(invocation.Arguments[1], "item1"));
            (setRootValueCountAfter > setRootValueCountBefore).ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnValueChange()
    {
        var invoked = false;
        string? receivedValue = null;

        var cut = Render(CreateNavRoot(
            onValueChange: EventCallback.Factory.Create<NavigationMenuValueChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedValue = args.Value;
            })
        ));

        var trigger = cut.Find("button[id='nav-trigger-item1']");
        trigger.Click();

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe("item1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientation()
    {
        var cut = Render(CreateNavRoot(orientation: NavigationMenuOrientation.Vertical));

        var nav = cut.Find("nav");
        nav.HasAttribute("aria-orientation").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
