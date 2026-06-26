namespace Blazix.BaseUI.Tests.Collapsible;

public class CollapsibleRootTests : BunitContext, ICollapsibleRootContract
{
    public CollapsibleRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCollapsiblePanel(JSInterop);
    }

    private RenderFragment CreateCollapsibleRoot(
        bool? open = null,
        bool defaultOpen = false,
        bool disabled = false,
        bool hiddenUntilFound = false,
        Func<CollapsibleRootState, string>? classValue = null,
        Func<CollapsibleRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<CollapsibleRootState>>? render = null,
        EventCallback<CollapsibleOpenChangeEventArgs>? onOpenChange = null,
        bool includeTrigger = true,
        bool includePanel = true)
    {
        return builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            var attrIndex = 1;

            if (open.HasValue)
                builder.AddAttribute(attrIndex++, "Open", open.Value);
            builder.AddAttribute(attrIndex++, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (onOpenChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChange", onOpenChange.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent(includeTrigger, includePanel, hiddenUntilFound));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent(bool includeTrigger = true, bool includePanel = true, bool hiddenUntilFound = false)
    {
        return builder =>
        {
            if (includeTrigger)
            {
                builder.OpenComponent<CollapsibleTrigger>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                builder.CloseComponent();
            }
            if (includePanel)
            {
                builder.OpenComponent<CollapsiblePanel>(2);
                builder.AddAttribute(3, "KeepMounted", true);
                builder.AddAttribute(4, "HiddenUntilFound", hiddenUntilFound);
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel Content")));
                builder.CloseComponent();
            }
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateCollapsibleRoot());

        var div = cut.Find("div");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateCollapsibleRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var section = cut.Find("section");
        section.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateCollapsibleRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "collapsible-root" },
                { "aria-label", "Collapsible" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"collapsible-root\"");
        cut.Markup.ShouldContain("aria-label=\"Collapsible\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateCollapsibleRoot(
            classValue: _ => "custom-class"
        ));

        cut.Markup.ShouldContain("custom-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateCollapsibleRoot(
            styleValue: _ => "background: blue"
        ));

        cut.Markup.ShouldContain("background: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateCollapsibleRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        cut.Markup.ShouldContain("static-class");
        cut.Markup.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        CollapsibleRootState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CollapsibleTrigger>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<CollapsibleRootState, string>)(state =>
                {
                    capturedState = state;
                    return "trigger-class";
                }));
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Open.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsOpenParameter()
    {
        var cut = Render(CreateCollapsibleRoot(open: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultOpen()
    {
        var cut = Render(CreateCollapsibleRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnOpenChange()
    {
        var invoked = false;
        var receivedOpen = false;

        var cut = Render(CreateCollapsibleRoot(
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedOpen = args.Open;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        invoked.ShouldBeTrue();
        receivedOpen.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnOpenChangeWithCorrectReason()
    {
        CollapsibleOpenChangeReason? receivedReason = null;

        var cut = Render(CreateCollapsibleRoot(
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                receivedReason = args.Reason;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        receivedReason.ShouldNotBeNull();
        receivedReason.ShouldBe(CollapsibleOpenChangeReason.TriggerPress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeReceivesTriggerEventDetails()
    {
        MouseEventArgs? receivedTriggerEvent = null;
        ElementReference? receivedTriggerElement = null;

        var cut = Render(CreateCollapsibleRoot(
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                receivedTriggerEvent = args.TriggerEvent;
                receivedTriggerElement = args.TriggerElement;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click(new MouseEventArgs { Detail = 2 });

        receivedTriggerEvent.ShouldNotBeNull();
        receivedTriggerEvent!.Detail.ShouldBe(2);
        receivedTriggerElement.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancellationPreventsStateChange()
    {
        var cut = Render(CreateCollapsibleRoot(
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancellationPreventsClosing()
    {
        var cut = Render(CreateCollapsibleRoot(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("true");
        cut.Find("div[data-open]").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledTriggerDoesNotInvokeOnOpenChange()
    {
        var callCount = 0;

        var cut = Render(CreateCollapsibleRoot(
            disabled: true,
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, _ =>
            {
                callCount++;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        callCount.ShouldBe(0);
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task BeforeMatchOpensWhenRootDisabled()
    {
        var cut = Render(CreateCollapsibleRoot(disabled: true, hiddenUntilFound: true));
        var panelComponent = cut.FindComponent<CollapsiblePanel>();

        var accepted = await panelComponent.InvokeAsync(() => panelComponent.Instance.OnBeforeMatch());

        accepted.ShouldBeTrue();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");
        cut.Find("div[data-open]").ShouldNotBeNull();
    }

    [Fact]
    public Task ControlledModeDoesNotMutateOpenWithoutParameterUpdate()
    {
        var cut = Render(CreateCollapsibleRoot(open: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        cut.FindAll("div[data-open]").ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReceivesCorrectState()
    {
        CollapsibleRootState? capturedState = null;

        var cut = Render(CreateCollapsibleRoot(
            defaultOpen: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Open.ShouldBeTrue();
        capturedState.Disabled.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StateCallbacksUpdateAcrossRootTriggerAndPanel()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            builder.AddAttribute(1, "data-testid", "root");
            builder.AddAttribute(2, "ClassValue", (Func<CollapsibleRootState, string>)(state =>
                state.Open ? "root-open" : "root-closed"));
            builder.AddAttribute(3, "StyleValue", (Func<CollapsibleRootState, string>)(state =>
                state.Open ? "opacity: 1" : "opacity: 0.5"));
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CollapsibleTrigger>(0);
                innerBuilder.AddAttribute(1, "data-testid", "trigger");
                innerBuilder.AddAttribute(2, "ClassValue", (Func<CollapsibleRootState, string>)(state =>
                    state.Open ? "trigger-open" : "trigger-closed"));
                innerBuilder.AddAttribute(3, "StyleValue", (Func<CollapsibleRootState, string>)(state =>
                    state.Open ? "color: green" : "color: red"));
                innerBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<CollapsiblePanel>(5);
                innerBuilder.AddAttribute(6, "data-testid", "panel");
                innerBuilder.AddAttribute(7, "KeepMounted", true);
                innerBuilder.AddAttribute(8, "ClassValue", (Func<CollapsiblePanelState, string>)(state =>
                    state.Open ? "panel-open" : "panel-closed"));
                innerBuilder.AddAttribute(9, "StyleValue", (Func<CollapsiblePanelState, string>)(state =>
                    state.Open ? "display: block" : "display: none"));
                innerBuilder.AddAttribute(10, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel Content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Find("[data-testid='root']").GetAttribute("class")!.ShouldContain("root-closed");
        cut.Find("[data-testid='root']").GetAttribute("style")!.ShouldContain("opacity: 0.5");
        cut.Find("[data-testid='trigger']").GetAttribute("class")!.ShouldContain("trigger-closed");
        cut.Find("[data-testid='trigger']").GetAttribute("style")!.ShouldContain("color: red");
        cut.Find("[data-testid='panel']").GetAttribute("class")!.ShouldContain("panel-closed");
        cut.Find("[data-testid='panel']").GetAttribute("style")!.ShouldContain("display: none");

        cut.Find("[data-testid='trigger']").Click();

        cut.Find("[data-testid='root']").GetAttribute("class")!.ShouldContain("root-open");
        cut.Find("[data-testid='root']").GetAttribute("style")!.ShouldContain("opacity: 1");
        cut.Find("[data-testid='trigger']").GetAttribute("class")!.ShouldContain("trigger-open");
        cut.Find("[data-testid='trigger']").GetAttribute("style")!.ShouldContain("color: green");
        cut.Find("[data-testid='panel']").GetAttribute("class")!.ShouldContain("panel-open");
        cut.Find("[data-testid='panel']").GetAttribute("style")!.ShouldContain("display: block");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitialOpenStateReportsIdleTransitionStatus()
    {
        CollapsibleRootState? capturedState = null;

        Render(CreateCollapsibleRoot(
            defaultOpen: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.TransitionStatus.ShouldBe(TransitionStatus.Idle);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateCollapsibleRoot(defaultOpen: true));

        var root = cut.Find("div[data-open]");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateCollapsibleRoot(defaultOpen: false));

        var root = cut.Find("div[data-closed]");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateCollapsibleRoot(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledOnRootWhenDisabled()
    {
        var cut = Render(CreateCollapsibleRoot(disabled: true));

        var root = cut.Find("div[data-disabled]");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowPropagationOnEventArgs()
    {
        var propagationAllowed = false;

        var cut = Render(CreateCollapsibleRoot(
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                args.AllowPropagation();
                propagationAllowed = args.IsPropagationAllowed;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        propagationAllowed.ShouldBeTrue();

        return Task.CompletedTask;
    }
}
