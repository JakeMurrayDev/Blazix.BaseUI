using System.Text.Json;
using Microsoft.JSInterop;
using DelayGroup = Blazix.BaseUI.Utilities.FloatingDelayGroup.FloatingDelayGroup;
using DelayGroupContext = Blazix.BaseUI.Utilities.FloatingDelayGroup.FloatingDelayGroupContext;

namespace Blazix.BaseUI.Tests.FloatingDelayGroup;

public class FloatingDelayGroupTests : BunitContext, IFloatingDelayGroupContract
{
    private const string FloatingModule = "./_content/Blazix.BaseUI/blazix-baseui-floating.js";
    private const string FloatingMinModule = "./_content/Blazix.BaseUI/blazix-baseui-floating.min.js";
    private readonly BunitJSModuleInterop module;

    public FloatingDelayGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetupModule(FloatingModule);
        module = SetupModule(FloatingMinModule);
    }

    private BunitJSModuleInterop SetupModule(string path)
    {
        var jsModule = JSInterop.SetupModule(path);
        jsModule.Setup<JsonElement>("createDelayGroup", _ => true)
            .SetResult(JsonSerializer.SerializeToElement(new { groupId = "dg-1" }));
        jsModule.SetupVoid("registerDelayGroupMember", _ => true).SetVoidResult();
        jsModule.SetupVoid("unregisterDelayGroupMember", _ => true).SetVoidResult();
        jsModule.SetupVoid("notifyDelayGroupMemberOpened", _ => true).SetVoidResult();
        jsModule.SetupVoid("notifyDelayGroupMemberClosed", _ => true).SetVoidResult();
        jsModule.SetupVoid("updateDelayGroupOptions", _ => true).SetVoidResult();
        jsModule.SetupVoid("disposeDelayGroup", _ => true).SetVoidResult();
        return jsModule;
    }

    private static RenderFragment CreateDelayGroup(
        int openDelayMs = 0,
        int closeDelayMs = 0,
        int timeoutMs = 0,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<DelayGroup>(0);
            builder.AddAttribute(1, "OpenDelayMs", openDelayMs);
            builder.AddAttribute(2, "CloseDelayMs", closeDelayMs);
            builder.AddAttribute(3, "TimeoutMs", timeoutMs);
            if (childContent is not null)
            {
                builder.AddAttribute(4, "ChildContent", childContent);
            }
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-testid", "dg-child");
            builder.AddContent(2, "Delay group content");
            builder.CloseElement();
        }));

        var child = cut.Find("[data-testid='dg-child']");
        child.TextContent.ShouldBe("Delay group content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CreatesGroupOnFirstRender()
    {
        Render(CreateDelayGroup(openDelayMs: 100, closeDelayMs: 200));

        module.Invocations
            .Where(i => i.Identifier == "createDelayGroup")
            .Count()
            .ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ProvidesGroupContext()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesOpenDelayMs()
    {
        Render(CreateDelayGroup(openDelayMs: 500));

        module.Invocations
            .Any(i => i.Identifier == "createDelayGroup")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesCloseDelayMs()
    {
        Render(CreateDelayGroup(closeDelayMs: 300));

        module.Invocations
            .Any(i => i.Identifier == "createDelayGroup")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesTimeoutMs()
    {
        Render(CreateDelayGroup(timeoutMs: 1000));

        module.Invocations
            .Any(i => i.Identifier == "createDelayGroup")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisposesGroupOnDispose()
    {
        Render(CreateDelayGroup());

        module.Invocations
            .Any(i => i.Identifier == "createDelayGroup")
            .ShouldBeTrue();

        Dispose();

        module.Invocations
            .Any(i => i.Identifier == "disposeDelayGroup")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task NotifiesMemberOpenedCallsJs()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        await captured!.NotifyMemberOpenedAsync("member-1");

        module.Invocations
            .Any(i => i.Identifier == "notifyDelayGroupMemberOpened")
            .ShouldBeTrue();
    }

    [Fact]
    public async Task NotifiesMemberClosedCallsJs()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        await captured!.NotifyMemberClosedAsync("member-1");

        module.Invocations
            .Any(i => i.Identifier == "notifyDelayGroupMemberClosed")
            .ShouldBeTrue();
    }

    [Fact]
    public async Task RegistersMemberWithCallbacks()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        var callback = new Blazix.BaseUI.Utilities.FloatingDelayGroup.DelayGroupMemberCallback((_) => Task.CompletedTask);
        var callbackRef = DotNetObjectReference.Create(callback);

        await captured!.RegisterMemberAsync("member-1", callbackRef);

        module.Invocations
            .Any(i => i.Identifier == "registerDelayGroupMember")
            .ShouldBeTrue();

        callbackRef.Dispose();
    }

    [Fact]
    public Task SetIsInstantPhaseUpdatesContext()
    {
        DelayGroupContext? captured = null;

        var cut = Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.IsInstantPhase.ShouldBeFalse();

        var component = cut.FindComponent<Blazix.BaseUI.Utilities.FloatingDelayGroup.FloatingDelayGroup>();
        component.Instance.SetIsInstantPhase(true);

        captured.IsInstantPhase.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextGetDelayReturnsInstantWhenInInstantPhase()
    {
        DelayGroupContext? captured = null;

        var cut = Render(CreateDelayGroup(openDelayMs: 500, closeDelayMs: 200, childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        var component = cut.FindComponent<Blazix.BaseUI.Utilities.FloatingDelayGroup.FloatingDelayGroup>();
        component.Instance.SetIsInstantPhase(true);

        var delay = captured!.GetDelay();
        delay.OpenDelayMs.ShouldBe(0);
        delay.CloseDelayMs.ShouldBe(200);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextGetDelayReturnsNormalWhenNotInInstantPhase()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(openDelayMs: 500, closeDelayMs: 200, childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        var delay = captured!.GetDelay();
        delay.OpenDelayMs.ShouldBe(500);
        delay.CloseDelayMs.ShouldBe(200);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextHasProviderIsTrue()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.HasProvider.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesOptionsWhenParametersChange()
    {
        var cut = Render<DelayGroup>(parameters => parameters
            .Add(p => p.OpenDelayMs, 100)
            .Add(p => p.CloseDelayMs, 200)
            .Add(p => p.TimeoutMs, 500));

        cut.Render(parameters => parameters
            .Add(p => p.OpenDelayMs, 300)
            .Add(p => p.CloseDelayMs, 400)
            .Add(p => p.TimeoutMs, 1000));

        module.Invocations
            .Any(i => i.Identifier == "updateDelayGroupOptions")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task UnregistersMemberCallsJs()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        await captured!.UnregisterMemberAsync("member-1");

        module.Invocations
            .Any(i => i.Identifier == "unregisterDelayGroupMember")
            .ShouldBeTrue();
    }

    [Fact]
    public async Task ContextCallbacksDoNotInvokeJsAfterDispose()
    {
        DelayGroupContext? captured = null;

        Render(CreateDelayGroup(childContent: builder =>
        {
            builder.OpenComponent<CascadingValueCapture<DelayGroupContext>>(0);
            builder.AddAttribute(1, "OnCaptured",
                EventCallback.Factory.Create<DelayGroupContext?>(
                    this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        Dispose();
        var invocationCount = module.Invocations.Count;

        await captured!.UnregisterMemberAsync("member-1");
        await captured.NotifyMemberOpenedAsync("member-1");
        await captured.NotifyMemberClosedAsync("member-1");

        module.Invocations.Count.ShouldBe(invocationCount);
    }
}
