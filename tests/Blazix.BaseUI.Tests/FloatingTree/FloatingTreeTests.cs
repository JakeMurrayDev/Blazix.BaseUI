using Microsoft.AspNetCore.Components;
using TreeContext = Blazix.BaseUI.FloatingTree.FloatingTreeContext;
using Tree = Blazix.BaseUI.FloatingTree.FloatingTree;

namespace Blazix.BaseUI.Tests.FloatingTree;

public class FloatingTreeTests : BunitContext, IFloatingTreeContract
{
    private sealed class FakeRootContext(bool open) : IFloatingRootContext
    {
        public string FloatingId => string.Empty;
        public bool GetOpen() => open;
        public ElementReference? GetTriggerElement() => null;
        public ElementReference? GetPopupElement() => null;
        public void SetPopupElement(ElementReference element) { }
        public Task SetOpenAsync(bool open) => Task.CompletedTask;
    }

    private const string FloatingModule = "./_content/Blazix.BaseUI/blazix-baseui-floating.min.js";

    public FloatingTreeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateTree(RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<Tree>(0);
            if (childContent is not null)
            {
                builder.AddAttribute(1, "ChildContent", childContent);
            }
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateTree(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-testid", "child");
            builder.AddContent(2, "Hello");
            builder.CloseElement();
        }));

        var child = cut.Find("[data-testid='child']");
        child.TextContent.ShouldBe("Hello");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ProvidesTreeContext()
    {
        TreeContext? captured = null;

        Render(CreateTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.TreeId.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task GeneratesUniqueTreeId()
    {
        TreeContext? captured1 = null;
        TreeContext? captured2 = null;

        Render(CreateTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                this, ctx => captured1 = ctx));
            builder.CloseComponent();
        }));

        Render(CreateTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                this, ctx => captured2 = ctx));
            builder.CloseComponent();
        }));

        captured1.ShouldNotBeNull();
        captured2.ShouldNotBeNull();
        captured1!.TreeId.ShouldNotBe(captured2!.TreeId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitializesJsTreeOnFirstRender()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.SetupVoid("getFloatingTree", _ => true).SetVoidResult();

        Render(CreateTree());

        var invocations = module.Invocations
            .Where(i => i.Identifier == "getFloatingTree")
            .ToList();

        invocations.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisposesJsTreeOnDispose()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.SetupVoid("getFloatingTree", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingTree", _ => true).SetVoidResult();

        Render(CreateTree());

        Dispose();

        var invocations = module.Invocations
            .Where(i => i.Identifier == "disposeFloatingTree")
            .ToList();

        invocations.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task EmitsEventToRegisteredHandlers()
    {
        TreeContext? captured = null;

        Render(CreateTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        object? received = null;
        Func<object?, Task> handler = payload =>
        {
            received = payload;
            return Task.CompletedTask;
        };

        captured!.On("openchange", handler);
        await captured.EmitAsync("openchange", "test-payload");

        received.ShouldBe("test-payload");
    }

    [Fact]
    public async Task RemovesHandlerOnOff()
    {
        TreeContext? captured = null;

        Render(CreateTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        var invokeCount = 0;
        Func<object?, Task> handler = _ =>
        {
            invokeCount++;
            return Task.CompletedTask;
        };

        captured!.On("test", handler);
        await captured.EmitAsync("test", null);
        invokeCount.ShouldBe(1);

        captured.Off("test", handler);
        await captured.EmitAsync("test", null);
        invokeCount.ShouldBe(1);
    }

    [Fact]
    public async Task EmitDoesNothingWithNoHandlers()
    {
        TreeContext? captured = null;

        Render(CreateTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();

        await captured!.EmitAsync("nonexistent", "data");

        // Should complete without error
        true.ShouldBeTrue();
    }

    [Fact]
    public Task UsesExternalTreeWhenProvided()
    {
        var module = JSInterop.SetupModule(FloatingModule);
        module.SetupVoid("getFloatingTree", _ => true).SetVoidResult();

        var externalContext = new TreeContext("external-tree-id");

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ExternalTree", externalContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                childBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, _ => { }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        // Should NOT have called getFloatingTree for the external tree
        var invocations = module.Invocations
            .Where(i => i.Identifier == "getFloatingTree")
            .ToList();

        invocations.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetNodeChildrenReturnsRecursiveOpenChildren()
    {
        var context = new TreeContext("test-tree");

        var root = new FloatingTreeNode("root", null);
        var child1 = new FloatingTreeNode("child1", "root") { Context = new FakeRootContext(true) };
        var grandchild1 = new FloatingTreeNode("grandchild1", "child1") { Context = new FakeRootContext(true) };
        var child2 = new FloatingTreeNode("child2", "root") { Context = new FakeRootContext(false) };

        context.RegisterNode(root);
        context.RegisterNode(child1);
        context.RegisterNode(grandchild1);
        context.RegisterNode(child2);

        var result = context.GetNodeChildren("root");

        result.Count.ShouldBe(2);
        result.ShouldContain(child1);
        result.ShouldContain(grandchild1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetNodeChildrenReturnsAllChildrenWhenOnlyOpenChildrenIsFalse()
    {
        var context = new TreeContext("test-tree");

        var root = new FloatingTreeNode("root", null);
        var child1 = new FloatingTreeNode("child1", "root") { Context = new FakeRootContext(true) };
        var grandchild1 = new FloatingTreeNode("grandchild1", "child1") { Context = new FakeRootContext(true) };
        var child2 = new FloatingTreeNode("child2", "root") { Context = new FakeRootContext(false) };

        context.RegisterNode(root);
        context.RegisterNode(child1);
        context.RegisterNode(grandchild1);
        context.RegisterNode(child2);

        var result = context.GetNodeChildren("root", onlyOpenChildren: false);

        result.Count.ShouldBe(3);
        result.ShouldContain(child1);
        result.ShouldContain(grandchild1);
        result.ShouldContain(child2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetDeepestNodeReturnsDeepestOpenDescendant()
    {
        var context = new TreeContext("test-tree");

        var root = new FloatingTreeNode("root", null) { Context = new FakeRootContext(true) };
        var child = new FloatingTreeNode("child", "root") { Context = new FakeRootContext(true) };
        var grandchild = new FloatingTreeNode("grandchild", "child") { Context = new FakeRootContext(true) };

        context.RegisterNode(root);
        context.RegisterNode(child);
        context.RegisterNode(grandchild);

        var result = context.GetDeepestNode("root");

        result.ShouldNotBeNull();
        result!.Id.ShouldBe("grandchild");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetDeepestNodeReturnsSelfWhenNoChildren()
    {
        var context = new TreeContext("test-tree");

        var lone = new FloatingTreeNode("lone", null) { Context = new FakeRootContext(true) };
        context.RegisterNode(lone);

        var result = context.GetDeepestNode("lone");

        result.ShouldNotBeNull();
        result!.Id.ShouldBe("lone");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AcceptsTypedExternalTree()
    {
        var externalTree = new TreeContext("typed-external-tree");
        TreeContext? captured = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ExternalTree", externalTree);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                childBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, ctx => captured = ctx));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        captured.ShouldNotBeNull();
        captured!.TreeId.ShouldBe("typed-external-tree");
        return Task.CompletedTask;
    }

    [Fact]
    public Task GetNodeAncestorsReturnsParentToRoot()
    {
        var context = new TreeContext("test-tree");

        var root = new FloatingTreeNode("root", null);
        var child = new FloatingTreeNode("child", "root");
        var grandchild = new FloatingTreeNode("grandchild", "child");

        context.RegisterNode(root);
        context.RegisterNode(child);
        context.RegisterNode(grandchild);

        var ancestors = context.GetNodeAncestors("grandchild").ToList();

        ancestors.Count.ShouldBe(2);
        ancestors[0].Id.ShouldBe("child");
        ancestors[1].Id.ShouldBe("root");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetNodeAncestorsReturnsEmptyForRootNode()
    {
        var context = new TreeContext("test-tree");

        var root = new FloatingTreeNode("root", null);
        context.RegisterNode(root);

        var ancestors = context.GetNodeAncestors("root").ToList();

        ancestors.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetNodeAncestorsStopsAtMissingParent()
    {
        var context = new TreeContext("test-tree");

        var orphan = new FloatingTreeNode("orphan", "nonexistent-parent");
        context.RegisterNode(orphan);

        var ancestors = context.GetNodeAncestors("orphan").ToList();

        ancestors.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
