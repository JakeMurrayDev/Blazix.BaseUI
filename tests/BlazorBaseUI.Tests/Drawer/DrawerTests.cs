using BlazorBaseUI.Drawer;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Drawer;

public class DrawerTests : BunitContext
{
    private const string ButtonMinModule = "./_content/BlazorBaseUI/blazor-baseui-button.min.js";

    public DrawerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupDrawerModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDrawer(
        bool? open = null,
        bool defaultOpen = false,
        DrawerModalMode modal = DrawerModalMode.True,
        bool keepMounted = false,
        bool includeBackdrop = true,
        bool includeViewport = true,
        bool includeSwipeArea = true,
        bool triggerDisabled = false,
        bool swipeAreaDisabled = false,
        DrawerSwipeDirection swipeDirection = DrawerSwipeDirection.Down,
        IReadOnlyList<DrawerSnapPoint>? snapPoints = null,
        DrawerSnapPoint? snapPoint = null,
        bool includeSnapPointParameter = false,
        DrawerSnapPoint? defaultSnapPoint = null,
        bool includeDefaultSnapPointParameter = false,
        EventCallback<DrawerOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<DrawerSnapPointChangeEventArgs>? onSnapPointChange = null,
        RenderFragment<RenderProps<DrawerPopupState>>? popupRender = null,
        RenderFragment? popupContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<DrawerRoot>(0);

            if (open.HasValue)
            {
                builder.AddAttribute(1, "Open", open.Value);
            }

            builder.AddAttribute(2, "DefaultOpen", defaultOpen);
            builder.AddAttribute(3, "Modal", modal);
            builder.AddAttribute(4, "SwipeDirection", swipeDirection);

            if (snapPoints is not null)
            {
                builder.AddAttribute(5, "SnapPoints", snapPoints);
            }

            if (includeSnapPointParameter || snapPoint.HasValue)
            {
                builder.AddAttribute(6, "SnapPoint", snapPoint);
            }

            if (includeDefaultSnapPointParameter || defaultSnapPoint.HasValue)
            {
                builder.AddAttribute(7, "DefaultSnapPoint", defaultSnapPoint);
            }

            if (onOpenChange.HasValue)
            {
                builder.AddAttribute(8, "OnOpenChange", onOpenChange.Value);
            }

            if (onSnapPointChange.HasValue)
            {
                builder.AddAttribute(9, "OnSnapPointChange", onSnapPointChange.Value);
            }

            builder.AddAttribute(10, "ChildContent", (RenderFragment<DrawerRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DrawerTrigger>(0);
                innerBuilder.AddAttribute(1, "data-testid", "trigger");
                innerBuilder.AddAttribute(2, "Disabled", triggerDisabled);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open drawer")));
                innerBuilder.CloseComponent();

                if (includeSwipeArea)
                {
                    innerBuilder.OpenComponent<DrawerSwipeArea>(10);
                    innerBuilder.AddAttribute(11, "data-testid", "swipe-area");
                    innerBuilder.AddAttribute(12, "Disabled", swipeAreaDisabled);
                    innerBuilder.CloseComponent();
                }

                innerBuilder.OpenComponent<DrawerPortal>(20);
                innerBuilder.AddAttribute(21, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    if (includeBackdrop)
                    {
                        portalBuilder.OpenComponent<DrawerBackdrop>(0);
                        portalBuilder.AddAttribute(1, "data-testid", "backdrop");
                        portalBuilder.CloseComponent();
                    }

                    if (includeViewport)
                    {
                        portalBuilder.OpenComponent<DrawerViewport>(10);
                        portalBuilder.AddAttribute(11, "data-testid", "viewport");
                        portalBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(viewportBuilder =>
                        {
                            RenderPopup(viewportBuilder, popupRender, popupContent);
                        }));
                        portalBuilder.CloseComponent();
                    }
                    else
                    {
                        RenderPopup(portalBuilder, popupRender, popupContent);
                    }
                }));
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    [Fact]
    public Task TriggerEmitsDialogAriaAndClosedState()
    {
        var cut = Render(CreateDrawer(open: false));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.TagName.ShouldBe("BUTTON");
        trigger.GetAttribute("aria-haspopup").ShouldBe("dialog");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        trigger.HasAttribute("data-disabled").ShouldBeFalse();
        trigger.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultOpenRendersPopupWithDrawerAttributes()
    {
        var cut = Render(CreateDrawer(defaultOpen: true));

        var popup = cut.Find("[data-testid='popup']");
        popup.GetAttribute("role").ShouldBe("dialog");
        popup.GetAttribute("tabindex").ShouldBe("-1");
        popup.HasAttribute("data-open").ShouldBeTrue();
        popup.GetAttribute("data-swipe-direction").ShouldBe("down");
        popup.GetAttribute("style").ShouldContain("--nested-drawers: 0");
        popup.GetAttribute("style").ShouldContain("--drawer-swipe-strength: 1");
        popup.GetAttribute("style").ShouldContain("--drawer-snap-point-offset: 0px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TitleDescriptionAndContentWireAccessibilityAttributes()
    {
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            popupContent: builder =>
            {
                builder.OpenComponent<DrawerTitle>(0);
                builder.AddAttribute(1, "data-testid", "title");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Drawer title")));
                builder.CloseComponent();

                builder.OpenComponent<DrawerDescription>(10);
                builder.AddAttribute(11, "data-testid", "description");
                builder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Drawer description")));
                builder.CloseComponent();

                builder.OpenComponent<DrawerContent>(20);
                builder.AddAttribute(21, "data-testid", "content");
                builder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Drawer content")));
                builder.CloseComponent();
            }));

        var popup = cut.Find("[data-testid='popup']");
        var title = cut.Find("[data-testid='title']");
        var description = cut.Find("[data-testid='description']");
        var content = cut.Find("[data-testid='content']");

        popup.GetAttribute("aria-labelledby").ShouldBe(title.GetAttribute("id"));
        popup.GetAttribute("aria-describedby").ShouldBe(description.GetAttribute("id"));
        content.HasAttribute("data-drawer-content").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task BackdropEmitsPresentationStateAndCssVariables()
    {
        var cut = Render(CreateDrawer(defaultOpen: true));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("role").ShouldBe("presentation");
        backdrop.HasAttribute("data-open").ShouldBeTrue();
        backdrop.GetAttribute("style").ShouldContain("user-select: none");
        backdrop.GetAttribute("style").ShouldContain("-webkit-user-select: none");
        backdrop.GetAttribute("style").ShouldContain("--drawer-swipe-progress: 0");
        backdrop.GetAttribute("style").ShouldContain("--drawer-swipe-strength: 1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ViewportEmitsPresentationState()
    {
        var cut = Render(CreateDrawer(defaultOpen: true));

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.GetAttribute("role").ShouldBe("presentation");
        viewport.HasAttribute("data-open").ShouldBeTrue();
        viewport.HasAttribute("data-nested").ShouldBeFalse();
        viewport.HasAttribute("data-nested-dialog-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SwipeAreaDefaultsToOppositeRootDirectionAndDisablesPointerEventsWhenDisabled()
    {
        var cut = Render(CreateDrawer(
            open: false,
            swipeDirection: DrawerSwipeDirection.Down,
            swipeAreaDisabled: true));

        var swipeArea = cut.Find("[data-testid='swipe-area']");
        swipeArea.GetAttribute("role").ShouldBe("presentation");
        swipeArea.GetAttribute("aria-hidden").ShouldBe("true");
        swipeArea.GetAttribute("data-swipe-direction").ShouldBe("up");
        swipeArea.HasAttribute("data-disabled").ShouldBeTrue();
        swipeArea.GetAttribute("style").ShouldContain("pointer-events: none");
        swipeArea.GetAttribute("style").ShouldContain("touch-action: pan-x");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvalidSwipeDirectionDoesNotFallBackToDownAttribute()
    {
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            swipeDirection: (DrawerSwipeDirection)99));

        var popup = cut.Find("[data-testid='popup']");
        popup.HasAttribute("data-swipe-direction").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PopupInitializesInteropWhenElementAppearsAfterFirstRender()
    {
        var cut = Render<DelayedPopupElementHost>(parameters => parameters
            .Add(component => component.RenderPopupElement, false));

        JSInterop.Invocations
            .Count(invocation => invocation.Identifier == "initializePopup")
            .ShouldBe(0);

        cut.Render(parameters => parameters
            .Add(component => component.RenderPopupElement, true));

        cut.WaitForAssertion(() =>
        {
            JSInterop.Invocations
                .Count(invocation => invocation.Identifier == "initializePopup")
                .ShouldBeGreaterThanOrEqualTo(2);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task SwipeAreaReinitializesJsWhenIdChanges()
    {
        var cut = Render<SwipeAreaIdHost>(parameters => parameters
            .Add(component => component.SwipeAreaId, "swipe-area-one"));

        cut.WaitForAssertion(() =>
        {
            JSInterop.Invocations.Any(invocation =>
                invocation.Identifier == "initializeSwipeArea" &&
                Equals(invocation.Arguments[1], "swipe-area-one")).ShouldBeTrue();
        });

        cut.Render(parameters => parameters
            .Add(component => component.SwipeAreaId, "swipe-area-two"));

        cut.WaitForAssertion(() =>
        {
            JSInterop.Invocations.Any(invocation =>
                invocation.Identifier == "disposeSwipeArea" &&
                Equals(invocation.Arguments[1], "swipe-area-one")).ShouldBeTrue();
            JSInterop.Invocations.Any(invocation =>
                invocation.Identifier == "initializeSwipeArea" &&
                Equals(invocation.Arguments[1], "swipe-area-two")).ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task SnapPointOneMarksPopupExpanded()
    {
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            snapPoints: [0.5, 1],
            snapPoint: 1));

        var popup = cut.Find("[data-testid='popup']");
        popup.HasAttribute("data-expanded").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExplicitNullDefaultSnapPointDoesNotFallBackToFirstSnapPoint()
    {
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            snapPoints: [1],
            defaultSnapPoint: null,
            includeDefaultSnapPointParameter: true));

        var popup = cut.Find("[data-testid='popup']");
        popup.HasAttribute("data-expanded").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExplicitNullControlledSnapPointDoesNotFallBackToFirstSnapPoint()
    {
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            snapPoints: [1],
            snapPoint: null,
            includeSnapPointParameter: true));

        var popup = cut.Find("[data-testid='popup']");
        popup.HasAttribute("data-expanded").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentPopupCssReportsNestedDrawerCount()
    {
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            popupContent: parentBuilder =>
            {
                parentBuilder.OpenComponent<DrawerTitle>(0);
                parentBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Parent")));
                parentBuilder.CloseComponent();

                RenderNestedOpenDrawer(parentBuilder, 10, "child-one");
                RenderNestedOpenDrawer(parentBuilder, 20, "child-two");
            }));

        var popup = cut.Find("[data-testid='popup']");
        popup.GetAttribute("style").ShouldContain("--nested-drawers: 2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepMountedClosedElementsRenderHidden()
    {
        var cut = Render(CreateDrawer(open: false, keepMounted: true));

        cut.Find("[data-testid='popup']").HasAttribute("hidden").ShouldBeTrue();
        cut.Find("[data-testid='viewport']").HasAttribute("hidden").ShouldBeTrue();
        cut.Find("[data-testid='backdrop']").HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task TriggerPressAndClosePressReportDrawerReasons()
    {
        var reasons = new List<DrawerOpenChangeReason>();

        var cut = Render(CreateDrawer(
            onOpenChange: EventCallback.Factory.Create<DrawerOpenChangeEventArgs>(this, args =>
            {
                reasons.Add(args.Reason);
            })));

        cut.Find("[data-testid='trigger']").Click();
        cut.Find("[data-testid='close']").Click();

        reasons.ShouldBe([DrawerOpenChangeReason.TriggerPress, DrawerOpenChangeReason.ClosePress]);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCanCancelOpening()
    {
        var cut = Render(CreateDrawer(
            onOpenChange: EventCallback.Factory.Create<DrawerOpenChangeEventArgs>(this, args =>
            {
                if (args.Open)
                {
                    args.Cancel();
                }
            })));

        cut.Find("[data-testid='trigger']").Click();

        cut.FindAll("[data-testid='popup']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ProviderIndentAndBackgroundReflectAnyOpenDrawer()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DrawerProvider>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(providerBuilder =>
            {
                providerBuilder.OpenComponent<DrawerIndentBackground>(0);
                providerBuilder.AddAttribute(1, "data-testid", "background");
                providerBuilder.CloseComponent();

                providerBuilder.OpenComponent<DrawerIndent>(10);
                providerBuilder.AddAttribute(11, "data-testid", "indent");
                providerBuilder.AddAttribute(12, "ChildContent", CreateDrawer(defaultOpen: true));
                providerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        cut.Find("[data-testid='background']").HasAttribute("data-active").ShouldBeTrue();
        cut.Find("[data-testid='indent']").HasAttribute("data-active").ShouldBeTrue();
        cut.Find("[data-testid='indent']").GetAttribute("style").ShouldContain("--drawer-swipe-progress: 0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DetachedHandleOpensDrawerWithPayload()
    {
        var handle = DrawerHandleFactory.CreateHandle<string>();

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DrawerTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Handle", handle);
            builder.AddAttribute(2, "Id", "external");
            builder.AddAttribute(3, "Payload", "payload-value");
            builder.AddAttribute(4, "data-testid", "trigger");
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External")));
            builder.CloseComponent();

            builder.OpenComponent<DrawerRoot>(10);
            builder.AddAttribute(11, "Handle", handle);
            builder.AddAttribute(12, "ChildContent", (RenderFragment<DrawerRootPayloadContext>)(payload => innerBuilder =>
            {
                innerBuilder.OpenComponent<DrawerPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DrawerViewport>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(viewportBuilder =>
                    {
                        viewportBuilder.OpenComponent<DrawerPopup>(0);
                        viewportBuilder.AddAttribute(1, "data-testid", "popup");
                        viewportBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, payload.Payload?.ToString())));
                        viewportBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        cut.Find("[data-testid='trigger']").Click();

        cut.Find("[data-testid='popup']").TextContent.ShouldContain("payload-value");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task CloseDisposeAsyncCleansUpButtonInteropAfterElementUnmounts()
    {
        var module = JSInterop.SetupModule(ButtonMinModule);
        module.SetupVoid("sync", _ => true).SetVoidResult();
        var cut = Render(CreateDrawer(
            defaultOpen: true,
            popupContent: builder =>
            {
                builder.OpenComponent<DrawerClose>(0);
                builder.AddAttribute(1, "NativeButton", false);
                builder.AddAttribute(2, "data-testid", "close");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
                builder.CloseComponent();
            }));

        var close = cut.FindComponent<DrawerClose>();
        cut.WaitForAssertion(() =>
        {
            module.Invocations.Any(invocation => invocation.Identifier == "sync").ShouldBeTrue();
        });
        var syncCountBeforeDispose = module.Invocations.Count(invocation => invocation.Identifier == "sync");

        ClearRenderElementReference(close.Instance);
        close.Instance.Element.HasValue.ShouldBeFalse();

        await close.InvokeAsync(async () => await close.Instance.DisposeAsync());

        module.Invocations
            .Skip(syncCountBeforeDispose)
            .Any(invocation => invocation.Identifier == "sync" && Equals(invocation.Arguments[4], true))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task IndentDisposeAsyncCleansUpCapturedElementAfterElementUnmounts()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DrawerProvider>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(providerBuilder =>
            {
                providerBuilder.OpenComponent<DrawerIndent>(0);
                providerBuilder.AddAttribute(1, "data-testid", "indent");
                providerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);
        var indent = cut.FindComponent<DrawerIndent>();
        cut.WaitForAssertion(() =>
        {
            JSInterop.Invocations.Any(invocation => invocation.Identifier == "initializeIndent").ShouldBeTrue();
        });

        ClearRenderElementReference(indent.Instance);
        indent.Instance.Element.HasValue.ShouldBeFalse();

        await indent.InvokeAsync(async () => await indent.Instance.DisposeAsync());

        JSInterop.Invocations.Any(invocation => invocation.Identifier == "disposeIndent").ShouldBeTrue();
    }

    [Fact]
    public async Task TypedTriggerDisposeAsyncCleansUpButtonInteropAfterElementUnmounts()
    {
        var module = JSInterop.SetupModule(ButtonMinModule);
        module.SetupVoid("sync", _ => true).SetVoidResult();
        var handle = DrawerHandleFactory.CreateHandle<string>();
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DrawerTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Handle", handle);
            builder.AddAttribute(2, "NativeButton", false);
            builder.AddAttribute(3, "data-testid", "trigger");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open")));
            builder.CloseComponent();
        };

        var cut = Render(fragment);
        var trigger = cut.FindComponent<DrawerTypedTrigger<string>>();
        cut.WaitForAssertion(() =>
        {
            module.Invocations.Any(invocation => invocation.Identifier == "sync").ShouldBeTrue();
        });
        var syncCountBeforeDispose = module.Invocations.Count(invocation => invocation.Identifier == "sync");

        ClearRenderElementReference(trigger.Instance);
        trigger.Instance.Element.HasValue.ShouldBeFalse();

        await trigger.InvokeAsync(async () => await trigger.Instance.DisposeAsync());

        module.Invocations
            .Skip(syncCountBeforeDispose)
            .Any(invocation => invocation.Identifier == "sync" && Equals(invocation.Arguments[4], true))
            .ShouldBeTrue();
    }

    private static void ClearRenderElementReference<TComponent>(TComponent component)
    {
        var field = typeof(TComponent).GetField(
            "renderElementReference",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        field.SetValue(component, null);
    }

    private static void RenderPopup(
        RenderTreeBuilder builder,
        RenderFragment<RenderProps<DrawerPopupState>>? popupRender,
        RenderFragment? popupContent)
    {
        builder.OpenComponent<DrawerPopup>(0);
        builder.AddAttribute(1, "data-testid", "popup");

        if (popupRender is not null)
        {
            builder.AddAttribute(2, "Render", popupRender);
        }

        builder.AddAttribute(3, "ChildContent", popupContent ?? ((RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<DrawerTitle>(0);
            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Title")));
            popupBuilder.CloseComponent();

            popupBuilder.OpenComponent<DrawerContent>(10);
            popupBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
            popupBuilder.CloseComponent();

            popupBuilder.OpenComponent<DrawerClose>(20);
            popupBuilder.AddAttribute(21, "data-testid", "close");
            popupBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
            popupBuilder.CloseComponent();
        })));

        builder.CloseComponent();
    }

    private static void RenderNestedOpenDrawer(RenderTreeBuilder builder, int sequence, string testId)
    {
        builder.OpenComponent<DrawerRoot>(sequence);
        builder.AddAttribute(sequence + 1, "DefaultOpen", true);
        builder.AddAttribute(sequence + 2, "ChildContent", (RenderFragment<DrawerRootPayloadContext>)(_ => nestedBuilder =>
        {
            nestedBuilder.OpenComponent<DrawerPortal>(0);
            nestedBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
            {
                portalBuilder.OpenComponent<DrawerViewport>(0);
                portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(viewportBuilder =>
                {
                    viewportBuilder.OpenComponent<DrawerPopup>(0);
                    viewportBuilder.AddAttribute(1, "data-testid", testId);
                    viewportBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<DrawerTitle>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, testId)));
                        popupBuilder.CloseComponent();
                    }));
                    viewportBuilder.CloseComponent();
                }));
                portalBuilder.CloseComponent();
            }));
            nestedBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private sealed class DelayedPopupElementHost : ComponentBase
    {
        [Parameter]
        public bool RenderPopupElement { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DrawerRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<DrawerRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DrawerPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DrawerViewport>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(viewportBuilder =>
                    {
                        viewportBuilder.OpenComponent<DrawerPopup>(0);
                        viewportBuilder.AddAttribute(1, "data-testid", "popup");
                        viewportBuilder.AddAttribute(2, "Render", RenderPopup);
                        viewportBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<DrawerTitle>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Title")));
                            popupBuilder.CloseComponent();
                        }));
                        viewportBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private RenderFragment<RenderProps<DrawerPopupState>> RenderPopup => props => builder =>
        {
            if (!RenderPopupElement)
            {
                return;
            }

            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };
    }

    private sealed class SwipeAreaIdHost : ComponentBase
    {
        [Parameter]
        public string SwipeAreaId { get; set; } = string.Empty;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DrawerRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment<DrawerRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DrawerSwipeArea>(0);
                innerBuilder.AddAttribute(1, "Id", SwipeAreaId);
                innerBuilder.AddAttribute(2, "data-testid", "swipe-area");
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
