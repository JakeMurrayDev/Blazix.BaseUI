using System.Reflection;
using BlazorBaseUI.Select;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Select;

public class SelectPositionerTests : BunitContext, ISelectPositionerContract
{
    private const string SelectModule = "./_content/BlazorBaseUI/blazor-baseui-select.js";

    public SelectPositionerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private static RenderFragment Wrap(
        bool defaultOpen = true,
        SelectModalMode modal = SelectModalMode.True,
        bool alignItemWithTrigger = true,
        bool disableAnchorTracking = false,
        string? side = null,
        string? align = null,
        Action<RenderTreeBuilder>? extraRoot = null,
        RenderFragment? positionerChildren = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "Modal", modal);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "T")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", alignItemWithTrigger);
                innerBuilder.AddAttribute(12, "DisableAnchorTracking", disableAnchorTracking);
                if (side is not null)
                {
                    innerBuilder.AddAttribute(13, "Side", Enum.Parse<Side>(side, true));
                }
                if (align is not null)
                {
                    innerBuilder.AddAttribute(14, "Align", Enum.Parse<Align>(align, true));
                }
                if (positionerChildren is not null)
                {
                    innerBuilder.AddAttribute(15, "ChildContent", positionerChildren);
                }
                innerBuilder.CloseComponent();

                extraRoot?.Invoke(innerBuilder);
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersPresentationDivByDefault()
    {
        var cut = Render(Wrap(alignItemWithTrigger: false));
        var positioner = cut.Find("div[data-side]");
        positioner.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataSideAndDataAlign()
    {
        var cut = Render(Wrap(alignItemWithTrigger: false, side: "Top", align: "Start"));
        var positioner = cut.Find("div[data-side]");
        positioner.GetAttribute("data-side").ShouldBe("top");
        positioner.GetAttribute("data-align").ShouldBe("start");
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataOpenWhenOpen()
    {
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: false));
        var positioner = cut.Find("div[data-side]");
        positioner.HasAttribute("data-open").ShouldBeTrue();
        positioner.HasAttribute("data-closed").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataClosedWhenClosed()
    {
        var cut = Render(Wrap(defaultOpen: false, alignItemWithTrigger: false));
        var positioner = cut.Find("div[data-side]");
        positioner.HasAttribute("data-closed").ShouldBeTrue();
        positioner.HasAttribute("data-open").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotOverwritePositioningStylesWhileClosingButStillMounted()
    {
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: false));
        var root = cut.FindComponent<SelectRoot<string>>().Instance;

        await cut.InvokeAsync(() => root.typedContext.SetOpenAsync(false, SelectOpenChangeReason.ItemPress));
        cut.FindComponent<SelectPositioner>().Render();

        var positioner = cut.Find("div[data-side]");
        positioner.HasAttribute("hidden").ShouldBeFalse();
        positioner.HasAttribute("style").ShouldBeFalse();
        positioner.HasAttribute("data-closed").ShouldBeTrue();
    }

    [Fact]
    public Task HiddenAttributeWhenNotMounted()
    {
        var cut = Render(Wrap(defaultOpen: false, alignItemWithTrigger: false));
        var positioner = cut.Find("div[data-side]");
        positioner.HasAttribute("hidden").ShouldBeTrue();
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
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "T")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", false);
                innerBuilder.AddAttribute(12, "data-custom", "custom-value");
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var positioner = cut.Find("div[data-side]");
        positioner.GetAttribute("data-custom").ShouldBe("custom-value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersInternalBackdropWhenMountedAndModal()
    {
        var cut = Render(Wrap(defaultOpen: true, modal: SelectModalMode.True, alignItemWithTrigger: false));
        var backdrops = cut.FindAll("[data-blazor-base-ui-inert]");
        backdrops.Count.ShouldBeGreaterThan(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsInternalBackdropWhenNotModal()
    {
        var cut = Render(Wrap(defaultOpen: true, modal: SelectModalMode.False, alignItemWithTrigger: false));
        var backdrops = cut.FindAll("[data-blazor-base-ui-inert]");
        backdrops.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsSideNoneWhenAlignItemWithTriggerActive()
    {
        // AlignItemWithTrigger is only active when mounted + openMethod != Touch.
        // DefaultOpen=true yields OpenInteractionType == None (not Touch), so the
        // positioner computes alignItemWithTriggerActive = true and emits side=none.
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: true, side: "Bottom"));
        var positioner = cut.Find("div[data-side]");
        positioner.GetAttribute("data-side").ShouldBe("none");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotEmitSideNoneForTouchOpen()
    {
        // If we simulate a touch-opened state by setting OpenInteractionType on the context
        // through a rendered positioner instance, data-side should fall back to the computed side.
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: true, side: "Top"));
        var positionerComp = cut.FindComponent<SelectPositioner>();

        // Flip the root's OpenInteractionType to Touch and force a re-render of the positioner.
        var selectRoot = cut.FindComponent<SelectRoot<string>>();
        ISelectRootContext typedContext = selectRoot.Instance.typedContext;
        typedContext.OpenInteractionType = InteractionType.Touch;

        positionerComp.Render();

        var positioner = cut.Find("div[data-side]");
        positioner.GetAttribute("data-side").ShouldBe("top");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RerenderUpdatesPositionWhenAnchorTrackingDisabled()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(Wrap(
            defaultOpen: true,
            alignItemWithTrigger: false,
            disableAnchorTracking: true));

        var initialUpdateCount = module.Invocations.Count(i => i.Identifier == "updatePosition");

        cut.FindComponent<SelectPositioner>().Render();

        module.Invocations
            .Count(i => i.Identifier == "updatePosition")
            .ShouldBe(initialUpdateCount + 1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ResetsScrollArrowVisibilityOnUnmount()
    {
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: false));
        var selectRoot = cut.FindComponent<SelectRoot<string>>();
        ISelectRootContext typedContext = selectRoot.Instance.typedContext;

        // Pretend JS decided arrows are visible.
        typedContext.ScrollUpArrowVisible = true;
        typedContext.ScrollDownArrowVisible = true;

        // Force an "unmount" transition by toggling the mounted flag and re-rendering
        // the positioner. OnParametersSet detects !mounted && lastMounted and clears.
        typedContext.Mounted = false;
        cut.FindComponent<SelectPositioner>().Render();

        typedContext.ScrollUpArrowVisible.ShouldBeFalse();
        typedContext.ScrollDownArrowVisible.ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesComputedStateOnPositionCallback()
    {
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: false, side: "Bottom", align: "Start"));
        var positionerComp = cut.FindComponent<SelectPositioner>();

        // JS-side callback: flip side to top, align to end, anchor hidden, arrow uncentered.
        positionerComp.Instance.OnPositionUpdated("top", "end", true, true);

        positionerComp.Render();

        var positioner = cut.Find("div[data-side]");
        positioner.GetAttribute("data-side").ShouldBe("top");
        positioner.GetAttribute("data-align").ShouldBe("end");
        positioner.HasAttribute("data-anchor-hidden").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task PrunesSingleSelectValueWhenItemDisappears()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "DefaultValue", "apple");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "T")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", false);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectItem<string>>(0);
                    posBuilder.AddAttribute(1, "Value", "apple");
                    posBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                    posBuilder.CloseComponent();

                    posBuilder.OpenComponent<SelectItem<string>>(20);
                    posBuilder.AddAttribute(21, "Value", "banana");
                    posBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var rootComp = cut.FindComponent<SelectRoot<string>>();
        var typedContext = rootComp.Instance.typedContext;
        typedContext.GetValue().ShouldBe("apple");

        // Simulate removing the selected item by unregistering it from the root and
        // firing the map-changed callback the positioner listens to.
        typedContext.UnregisterItemValue("apple");

        typedContext.GetValue().ShouldBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FallsBackToInitialValueWhenPruned()
    {
        // When the current value disappears but the initial value is still mounted,
        // the positioner should restore the initial value as the selection.
        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "DefaultValue", "apple");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "T")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", false);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectItem<string>>(0);
                    posBuilder.AddAttribute(1, "Value", "apple");
                    posBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                    posBuilder.CloseComponent();

                    posBuilder.OpenComponent<SelectItem<string>>(20);
                    posBuilder.AddAttribute(21, "Value", "banana");
                    posBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var rootComp = cut.FindComponent<SelectRoot<string>>();
        var typedContext = rootComp.Instance.typedContext;

        // Simulate switching the active selection to banana by calling the internal
        // single-select API (apple is the captured initial value). The call must run
        // on the renderer's dispatcher because it may trigger StateHasChanged.
        await cut.InvokeAsync(async () => await typedContext.SelectValueAsync("banana"));
        typedContext.GetValue().ShouldBe("banana");

        // Now remove banana; the positioner prune logic should fall back to "apple".
        await cut.InvokeAsync(() =>
        {
            typedContext.UnregisterItemValue("banana");
            return Task.CompletedTask;
        });
        typedContext.GetValue().ShouldBe("apple");
    }

    [Fact]
    public Task FiltersMultiSelectArrayOnItemMapChange()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "Multiple", true);
            builder.AddAttribute(3, "DefaultValues", new List<string> { "apple", "banana" });
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "T")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", false);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectItem<string>>(0);
                    posBuilder.AddAttribute(1, "Value", "apple");
                    posBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                    posBuilder.CloseComponent();

                    posBuilder.OpenComponent<SelectItem<string>>(20);
                    posBuilder.AddAttribute(21, "Value", "banana");
                    posBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var rootComp = cut.FindComponent<SelectRoot<string>>();
        var typedContext = rootComp.Instance.typedContext;

        typedContext.GetValues().ShouldBe(new[] { "apple", "banana" });

        typedContext.UnregisterItemValue("banana");

        typedContext.GetValues().ShouldBe(new[] { "apple" });
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PrunesSingleSelectValueWhenSameCountReplacementHasHashCollision()
    {
        var oldSelected = new HashCollisionValue(1);
        var oldOther = new HashCollisionValue(2);
        var newValue = new HashCollisionValue(3);
        var newOther = new HashCollisionValue(0);

        var cut = Render(builder =>
        {
            builder.OpenComponent<SelectRoot<HashCollisionValue>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "DefaultValue", oldSelected);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectPositioner>(0);
                innerBuilder.AddAttribute(1, "AlignItemWithTrigger", false);
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var rootComp = cut.FindComponent<SelectRoot<HashCollisionValue>>();
        var typedContext = rootComp.Instance.typedContext;
        var positioner = cut.FindComponent<SelectPositioner>().Instance;

        // Every value in this fixture has hash 0, so the old set and new set
        // collide under XOR aggregation even though no old value remains.
        // This reproduces the same-count replacement case without relying on
        // Blazor's incremental unregister/register ordering, which would prune
        // on the intermediate shrink before the collision is observable.
        SeedPreviousItemMap(positioner, [oldSelected, oldOther]);
        var registeredValues = (List<object?>)typedContext.RegisteredValues;
        registeredValues.Clear();
        registeredValues.Add(newValue);
        registeredValues.Add(newOther);

        await InvokeHandleItemMapChangedAsync(positioner);

        typedContext.GetValue().ShouldBeNull();
    }

    [Fact]
    public Task ExposesScrollArrowSettersOnPositionerContext()
    {
        var cut = Render(Wrap(defaultOpen: true, alignItemWithTrigger: false));
        var positionerComp = cut.FindComponent<SelectPositioner>();

        var ctx = positionerComp.Instance.positionerContext;
        ctx.SetScrollUpArrow.ShouldNotBeNull();
        ctx.SetScrollDownArrow.ShouldNotBeNull();
        ctx.GetScrollUpArrow.ShouldNotBeNull();
        ctx.GetScrollDownArrow.ShouldNotBeNull();
        ctx.SetControlledAlignItemWithTrigger.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    private static void SeedPreviousItemMap(SelectPositioner positioner, IReadOnlyList<object?> values)
    {
        SetPrivateField(positioner, "prevItemMapSize", values.Count);

        var hashField = typeof(SelectPositioner).GetField(
            "prevItemMapHash",
            BindingFlags.Instance | BindingFlags.NonPublic);
        hashField?.SetValue(positioner, values.Aggregate(0, (hash, value) => hash ^ (value?.GetHashCode() ?? 0)));

        var snapshotField = typeof(SelectPositioner).GetField(
            "prevItemMapValues",
            BindingFlags.Instance | BindingFlags.NonPublic);
        snapshotField?.SetValue(positioner, values.ToList());
    }

    private static void SetPrivateField<T>(object target, string name, T value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
        {
            throw new InvalidOperationException($"Could not find field '{name}'.");
        }

        field.SetValue(target, value);
    }

    private static async Task InvokeHandleItemMapChangedAsync(SelectPositioner positioner)
    {
        var method = typeof(SelectPositioner).GetMethod(
            "HandleItemMapChangedAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            throw new InvalidOperationException("Could not find HandleItemMapChangedAsync.");
        }

        var task = (Task?)method.Invoke(positioner, null);
        if (task is null)
        {
            throw new InvalidOperationException("HandleItemMapChangedAsync did not return a Task.");
        }

        await task;
    }

    private sealed record HashCollisionValue(int Id)
    {
        public override int GetHashCode() => 0;
    }
}
