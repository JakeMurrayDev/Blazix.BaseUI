using Microsoft.JSInterop;
using FocusManager = Blazix.BaseUI.Utilities.FloatingFocusManager.FloatingFocusManager;

namespace Blazix.BaseUI.Tests.FloatingFocusManager;

public class FloatingFocusManagerTests : BunitContext, IFloatingFocusManagerContract
{
    private const string FloatingModule = "./_content/Blazix.BaseUI/blazix-baseui-floating.js";

    public FloatingFocusManagerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateFocusManager(
        IFloatingRootContext? rootContext = null,
        bool modal = true,
        bool initialFocus = true,
        string? initialFocusSelector = null,
        bool returnFocus = true,
        bool restoreFocus = false,
        string? restoreFocusMode = null,
        bool closeOnFocusOut = true,
        string? interactionType = null,
        bool disabled = false,
        RenderFragment? childContent = null,
        IReadOnlyList<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>? order = null,
        IReadOnlyList<ElementReference>? insideElements = null,
        ElementReference? nextFocusableElement = null,
        ElementReference? previousFocusableElement = null)
    {
        return builder =>
        {
            if (rootContext is not null)
            {
                builder.OpenComponent<CascadingValue<IFloatingRootContext>>(0);
                builder.AddAttribute(1, "Value", rootContext);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    RenderFocusManager(innerBuilder, modal, initialFocus, initialFocusSelector, returnFocus,
                        restoreFocus, restoreFocusMode, closeOnFocusOut, interactionType, disabled,
                        childContent, order, insideElements, nextFocusableElement, previousFocusableElement);
                }));
                builder.CloseComponent();
            }
            else
            {
                RenderFocusManager(builder, modal, initialFocus, initialFocusSelector, returnFocus,
                    restoreFocus, restoreFocusMode, closeOnFocusOut, interactionType, disabled,
                    childContent, order, insideElements, nextFocusableElement, previousFocusableElement);
            }
        };
    }

    private static void RenderFocusManager(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        bool modal, bool initialFocus, string? initialFocusSelector,
        bool returnFocus, bool restoreFocus, string? restoreFocusMode,
        bool closeOnFocusOut, string? interactionType, bool disabled,
        RenderFragment? childContent,
        IReadOnlyList<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>? order,
        IReadOnlyList<ElementReference>? insideElements,
        ElementReference? nextFocusableElement, ElementReference? previousFocusableElement)
    {
        builder.OpenComponent<FocusManager>(0);
        builder.AddAttribute(1, "Modal", modal);
        builder.AddAttribute(2, "InitialFocus", initialFocus);
        builder.AddAttribute(3, "InitialFocusSelector", initialFocusSelector);
        builder.AddAttribute(4, "ReturnFocus", returnFocus);
        builder.AddAttribute(5, "RestoreFocus", restoreFocus);
        builder.AddAttribute(6, "RestoreFocusMode", restoreFocusMode);
        builder.AddAttribute(7, "CloseOnFocusOut", closeOnFocusOut);
        builder.AddAttribute(8, "InteractionType", interactionType);
        builder.AddAttribute(9, "Disabled", disabled);
        var actualChildContent = childContent ?? ((RenderFragment)(childBuilder =>
        {
            childBuilder.OpenElement(0, "div");
            childBuilder.AddAttribute(1, "data-testid", "fm-default-child");
            childBuilder.CloseElement();
        }));
        builder.AddAttribute(10, "ChildContent", actualChildContent);
        if (order is not null)
        {
            builder.AddAttribute(11, "Order", order);
        }
        if (insideElements is not null)
        {
            builder.AddAttribute(12, "InsideElements", insideElements);
        }
        if (nextFocusableElement.HasValue)
        {
            builder.AddAttribute(13, "NextFocusableElement", nextFocusableElement);
        }
        if (previousFocusableElement.HasValue)
        {
            builder.AddAttribute(14, "PreviousFocusableElement", previousFocusableElement);
        }
        builder.CloseComponent();
    }

    private static TestFloatingRootContext CreateOpenRootContext() => new()
    {
        IsOpen = true,
        PopupElement = new ElementReference("popup-el"),
        TriggerElement = new ElementReference("trigger-el")
    };

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateFocusManager(
            childContent: builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "fm-child");
                builder.AddContent(2, "Focus content");
                builder.CloseElement();
            }));

        var child = cut.Find("[data-testid='fm-child']");
        child.TextContent.ShouldBe("Focus content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderHtmlElement()
    {
        var cut = Render(CreateFocusManager(
            childContent: builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "fm-child");
                builder.CloseElement();
            }));

        cut.FindAll("[data-testid='fm-child']").Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task CreatesManagerOnFirstRender()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var rootContext = CreateOpenRootContext();

        Render(CreateFocusManager(rootContext: rootContext));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesModalOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: false));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesInitialFocusOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), initialFocus: false));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesInitialFocusSelectorOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), initialFocusSelector: "#my-input"));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesReturnFocusOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), returnFocus: false));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesRestoreFocusOptions()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), restoreFocus: true, restoreFocusMode: "popup"));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesCloseOnFocusOutOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), closeOnFocusOut: false));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesInteractionType()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), interactionType: "keyboard"));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNothingWhenDisabled()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), disabled: true));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisposesManagerOnDispose()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext()));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        Dispose();

        module.Invocations
            .Any(i => i.Identifier == "disposeFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsUpdateWhenModalChangesWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var rootContext = CreateOpenRootContext();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", rootContext);
            builder.AddAttribute(2, "Modal", true);
            builder.CloseComponent();
        });

        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.Modal = false;
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "updateFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsUpdateWhenReturnFocusChangesWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "ReturnFocus", true);
            builder.CloseComponent();
        });

        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.ReturnFocus = false;
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "updateFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsUpdateWhenCloseOnFocusOutChangesWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "CloseOnFocusOut", true);
            builder.CloseComponent();
        });

        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.CloseOnFocusOut = false;
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "updateFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsUpdateWhenInsideElementsChangeWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var elements1 = new List<ElementReference>();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "InsideElements", (IReadOnlyList<ElementReference>)elements1);
            builder.CloseComponent();
        });

        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.InsideElements = new List<ElementReference> { new ElementReference("el-1") };
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "updateFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotCallUpdateWhenParametersUnchanged()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "Modal", true);
            builder.CloseComponent();
        });

        // Re-render with same params (no change)
        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "updateFloatingFocusManager")
            .ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesOrderOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var order = new List<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>
        {
            Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem.Reference,
            Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem.Content
        };

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), order: order));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesInsideElementsOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var elements = new List<ElementReference> { new ElementReference("inside-el") };

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), insideElements: elements));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesNextFocusableElementOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var nextEl = new ElementReference("next-el");

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), nextFocusableElement: nextEl));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesPreviousFocusableElementOption()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var prevEl = new ElementReference("prev-el");

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), previousFocusableElement: prevEl));

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersFocusGuardsWhenModal()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: true));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards.Count.ShouldBe(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderFocusGuardsWhenDisabled()
    {
        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), disabled: true));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesBeforeContentFocusGuardElement()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: true));

        var fmComponent = cut.FindComponent<FocusManager>();
        fmComponent.Instance.BeforeContentFocusGuardElement.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AcceptsGetInsideElementsParameter()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var extraElements = new List<ElementReference> { new ElementReference("extra-el") };
        Func<IReadOnlyList<ElementReference>> getInsideElements = () => extraElements;

        Render(builder =>
        {
            builder.OpenComponent<CascadingValue<IFloatingRootContext>>(0);
            builder.AddAttribute(1, "Value", (IFloatingRootContext)CreateOpenRootContext());
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<FocusManager>(0);
                innerBuilder.AddAttribute(1, "GetInsideElements", getInsideElements);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenElement(0, "div");
                    childBuilder.AddAttribute(1, "data-testid", "fm-child");
                    childBuilder.CloseElement();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AcceptsExternalTreeParameter()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var externalTree = new Blazix.BaseUI.Utilities.FloatingTree.FloatingTreeContext("ext-tree-id");

        Render(builder =>
        {
            builder.OpenComponent<CascadingValue<IFloatingRootContext>>(0);
            builder.AddAttribute(1, "Value", (IFloatingRootContext)CreateOpenRootContext());
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<FocusManager>(0);
                innerBuilder.AddAttribute(1, "ExternalTree", externalTree);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenElement(0, "div");
                    childBuilder.AddAttribute(1, "data-testid", "fm-child");
                    childBuilder.CloseElement();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultsOrderToContent()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext()));

        var fmComponent = cut.FindComponent<FocusManager>();
        fmComponent.Instance.Order.ShouldNotBeNull();
        fmComponent.Instance.Order!.Count.ShouldBe(1);
        fmComponent.Instance.Order![0].ShouldBe(Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem.Content);

        return Task.CompletedTask;
    }

    [Fact]
    public Task FocusGuardsHaveDataTypeAttribute()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: true));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards.Count.ShouldBe(2);

        foreach (var guard in guards)
        {
            guard.GetAttribute("data-blazix-base-ui-focus-guard-type").ShouldBe("inside");
        }

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderFocusGuardsWhenNonModalWithoutPortal()
    {
        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: false));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsHandleFocusGuardFocusWithBeforeDirection()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("handleFocusGuardFocus", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: true));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards[0].TriggerEvent("onfocus", new FocusEventArgs());

        module.Invocations
            .Any(i => i.Identifier == "handleFocusGuardFocus"
                && (string)i.Arguments[1]! == "before")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsHandleFocusGuardFocusWithAfterDirection()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("handleFocusGuardFocus", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: true));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards[1].TriggerEvent("onfocus", new FocusEventArgs());

        module.Invocations
            .Any(i => i.Identifier == "handleFocusGuardFocus"
                && (string)i.Arguments[1]! == "after")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsUpdateWhenOrderChangesWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var order1 = new List<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>
        {
            Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem.Content
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "Order", (IReadOnlyList<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>)order1);
            builder.CloseComponent();
        });

        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.Order = new List<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>
        {
            Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem.Reference,
            Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem.Content
        };
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "updateFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisposesManagerWhenDisabledWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "Disabled", false);
            builder.CloseComponent();
        });

        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.Disabled = true;
        wrapper.Render();

        module.Invocations
            .Count(i => i.Identifier == "disposeFloatingFocusManager")
            .ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisposesManagerWhenPopupClosesDuringManagerCreation()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        var createManager = module.Setup<string>("createFloatingFocusManager", _ => true);
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();
        module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();

        var rootContext = CreateOpenRootContext();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", rootContext);
            builder.CloseComponent();
        });

        createManager.Invocations.Count.ShouldBe(1);

        rootContext.IsOpen = false;
        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Render();

        createManager.SetResult("fm-1");

        cut.WaitForAssertion(() =>
        {
            module.Invocations
                .Count(i => i.Identifier == "disposeFloatingFocusManager")
                .ShouldBe(1);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task CreatesManagerWhenEnabledWhileOpen()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", CreateOpenRootContext());
            builder.AddAttribute(2, "Disabled", true);
            builder.CloseComponent();
        });

        // Manager should NOT have been created yet
        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeFalse();

        // Enable — should create on next render
        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Instance.Disabled = false;
        wrapper.Render();

        module.Invocations
            .Any(i => i.Identifier == "createFloatingFocusManager")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesModalArgumentToJs()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();

        Render(CreateFocusManager(rootContext: CreateOpenRootContext(), modal: false));

        var invocation = module.Invocations
            .First(i => i.Identifier == "createFloatingFocusManager");
        var arg = invocation.Arguments[0]!;
        var modalProp = arg.GetType().GetProperty("modal");
        modalProp.ShouldNotBeNull();
        ((bool)modalProp.GetValue(arg)!).ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesCloseInteractionTypeToReturnFocusCallback()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.Setup<string>("createFloatingFocusManager", _ => true).SetResult("fm-1");
        module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();
        module.Setup<string>("getLastInteractionType", _ => true).SetResult("keyboard");

        string? receivedType = null;
        Func<string?, bool> callback = type =>
        {
            receivedType = type;
            return true;
        };

        var rootContext = CreateOpenRootContext();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FocusManagerWrapper>(0);
            builder.AddAttribute(1, "RootContext", (IFloatingRootContext)rootContext);
            builder.AddAttribute(2, "ReturnFocusCallback", callback);
            builder.CloseComponent();
        });

        // Close the popup to trigger the dispose path
        rootContext.IsOpen = false;
        var wrapper = cut.FindComponent<FocusManagerWrapper>();
        wrapper.Render();

        receivedType.ShouldBe("keyboard");

        return Task.CompletedTask;
    }
}

internal sealed class FocusManagerWrapper : ComponentBase
{
    [Parameter] public IFloatingRootContext? RootContext { get; set; }
    [Parameter] public bool Modal { get; set; } = true;
    [Parameter] public bool ReturnFocus { get; set; } = true;
    [Parameter] public bool CloseOnFocusOut { get; set; } = true;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public IReadOnlyList<ElementReference>? InsideElements { get; set; }
    [Parameter] public IReadOnlyList<Blazix.BaseUI.Utilities.FloatingFocusManager.FocusManagerOrderItem>? Order { get; set; }
    [Parameter] public Func<string?, bool>? ReturnFocusCallback { get; set; }

    public void TriggerReRender() => InvokeAsync(StateHasChanged);

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        if (RootContext is not null)
        {
            builder.OpenComponent<CascadingValue<IFloatingRootContext>>(0);
            builder.AddAttribute(1, "Value", RootContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<Blazix.BaseUI.Utilities.FloatingFocusManager.FloatingFocusManager>(0);
                inner.AddAttribute(1, "Modal", Modal);
                inner.AddAttribute(2, "ReturnFocus", ReturnFocus);
                inner.AddAttribute(3, "CloseOnFocusOut", CloseOnFocusOut);
                inner.AddAttribute(4, "Disabled", Disabled);
                if (InsideElements is not null)
                {
                    inner.AddAttribute(5, "InsideElements", InsideElements);
                }
                if (Order is not null)
                {
                    inner.AddAttribute(6, "Order", Order);
                }
                if (ReturnFocusCallback is not null)
                {
                    inner.AddAttribute(8, "ReturnFocusCallback", ReturnFocusCallback);
                }
                inner.AddAttribute(7, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenElement(0, "div");
                    childBuilder.AddAttribute(1, "data-testid", "fm-wrapper-child");
                    childBuilder.CloseElement();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}

internal sealed class TestFloatingRootContext : IFloatingRootContext
{
    public string FloatingId { get; set; } = "test-floating-id";
    public bool IsOpen { get; set; }
    public ElementReference? PopupElement { get; set; }
    public ElementReference? TriggerElement { get; set; }

    public bool GetOpen() => IsOpen;
    public ElementReference? GetTriggerElement() => TriggerElement;
    public ElementReference? GetPopupElement() => PopupElement;
    public void SetPopupElement(ElementReference element) => PopupElement = element;
    public Task SetOpenAsync(bool open)
    {
        IsOpen = open;
        return Task.CompletedTask;
    }
}
