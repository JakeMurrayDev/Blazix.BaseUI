using Microsoft.AspNetCore.Components;
using TreeContext = Blazix.BaseUI.FloatingTree.FloatingTreeContext;
using NodeContext = Blazix.BaseUI.FloatingTree.FloatingNodeContext;
using Tree = Blazix.BaseUI.FloatingTree.FloatingTree;
using Node = Blazix.BaseUI.FloatingTree.FloatingNode;

namespace Blazix.BaseUI.Tests.FloatingTree;

public class FloatingNodeTests : BunitContext, IFloatingNodeContract
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

    public FloatingNodeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateNodeInTree(RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<Node>(0);
                if (childContent is not null)
                {
                    treeBuilder.AddAttribute(1, "ChildContent", childContent);
                }
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNodeInTree(builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "node-child");
            builder.AddContent(2, "Node content");
            builder.CloseElement();
        }));

        var child = cut.Find("[data-testid='node-child']");
        child.TextContent.ShouldBe("Node content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersWithTreeOnInit()
    {
        TreeContext? captured = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                treeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, ctx => captured = ctx));
                treeBuilder.CloseComponent();

                treeBuilder.OpenComponent<Node>(2);
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        captured.ShouldNotBeNull();
        captured!.Nodes.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task UnregistersOnDispose()
    {
        TreeContext? captured = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                treeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, ctx => captured = ctx));
                treeBuilder.CloseComponent();

                treeBuilder.OpenComponent<Node>(2);
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        captured.ShouldNotBeNull();
        captured!.Nodes.Count.ShouldBe(1);

        Dispose();

        captured.Nodes.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasNullParentIdWhenTopLevel()
    {
        TreeContext? captured = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                treeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, ctx => captured = ctx));
                treeBuilder.CloseComponent();

                treeBuilder.OpenComponent<Node>(2);
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        captured.ShouldNotBeNull();
        captured!.Nodes[0].ParentId.ShouldBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PicksUpParentIdFromOuterNode()
    {
        TreeContext? captured = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                treeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, ctx => captured = ctx));
                treeBuilder.CloseComponent();

                treeBuilder.OpenComponent<Node>(2);
                treeBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(outerBuilder =>
                {
                    outerBuilder.OpenComponent<Node>(0);
                    outerBuilder.CloseComponent();
                }));
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        captured.ShouldNotBeNull();
        captured!.Nodes.Count.ShouldBe(2);

        var outerNode = captured.Nodes[0];
        var innerNode = captured.Nodes[1];

        outerNode.ParentId.ShouldBeNull();
        innerNode.ParentId.ShouldBe(outerNode.Id);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ProvidesNodeContext()
    {
        NodeContext? captured = null;

        Render(CreateNodeInTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.NodeId.ShouldNotBeNullOrEmpty();
        captured.TreeId.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesSetContextCallback()
    {
        NodeContext? captured = null;

        Render(CreateNodeInTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.SetContext.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PassesTreeContextReferenceToNodeContext()
    {
        NodeContext? captured = null;

        Render(CreateNodeInTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.TreeContext.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DelegatesEventSubscriptionToTree()
    {
        NodeContext? captured = null;

        Render(CreateNodeInTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
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

        captured!.On("test-event", handler);
        await captured.EmitAsync("test-event", "hello");

        received.ShouldBe("hello");
    }

    [Fact]
    public Task RegistersNodeWithJsOnFirstRender()
    {
        var module = JSInterop.SetupModule("./_content/Blazix.BaseUI/blazix-baseui-floating.min.js");
        module.SetupVoid("getFloatingTree", _ => true).SetVoidResult();
        module.SetupVoid("addTreeNode", _ => true).SetVoidResult();
        module.SetupVoid("removeTreeNode", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingTree", _ => true).SetVoidResult();

        Render(CreateNodeInTree());

        module.Invocations
            .Any(i => i.Identifier == "addTreeNode")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemovesNodeFromJsOnDispose()
    {
        var module = JSInterop.SetupModule("./_content/Blazix.BaseUI/blazix-baseui-floating.min.js");
        module.SetupVoid("getFloatingTree", _ => true).SetVoidResult();
        module.SetupVoid("addTreeNode", _ => true).SetVoidResult();
        module.SetupVoid("removeTreeNode", _ => true).SetVoidResult();
        module.SetupVoid("disposeFloatingTree", _ => true).SetVoidResult();

        Render(CreateNodeInTree());

        Dispose();

        module.Invocations
            .Any(i => i.Identifier == "removeTreeNode")
            .ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesConsumerProvidedId()
    {
        NodeContext? captured = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<Node>(0);
                treeBuilder.AddAttribute(1, "Id", "my-custom-id");
                treeBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(nodeBuilder =>
                {
                    nodeBuilder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
                    nodeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
                        this, ctx => captured = ctx));
                    nodeBuilder.CloseComponent();
                }));
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        captured.ShouldNotBeNull();
        captured!.NodeId.ShouldBe("my-custom-id");

        return Task.CompletedTask;
    }

    [Fact]
    public Task FallsBackToGeneratedIdWhenIdNotProvided()
    {
        NodeContext? captured = null;

        Render(CreateNodeInTree(builder =>
        {
            builder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
            builder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
                this, ctx => captured = ctx));
            builder.CloseComponent();
        }));

        captured.ShouldNotBeNull();
        captured!.NodeId.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReportsOpenStateFromContext()
    {
        TreeContext? treeCtx = null;
        NodeContext? nodeCtx = null;

        Render(builder =>
        {
            builder.OpenComponent<Tree>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(treeBuilder =>
            {
                treeBuilder.OpenComponent<CascadingValueCapture<TreeContext>>(0);
                treeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<TreeContext?>(
                    this, ctx => treeCtx = ctx));
                treeBuilder.CloseComponent();

                treeBuilder.OpenComponent<Node>(2);
                treeBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(nodeBuilder =>
                {
                    nodeBuilder.OpenComponent<CascadingValueCapture<NodeContext>>(0);
                    nodeBuilder.AddAttribute(1, "OnCaptured", EventCallback.Factory.Create<NodeContext?>(
                        this, ctx => nodeCtx = ctx));
                    nodeBuilder.CloseComponent();
                }));
                treeBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        treeCtx.ShouldNotBeNull();
        nodeCtx.ShouldNotBeNull();
        treeCtx!.Nodes.Count.ShouldBe(1);

        (treeCtx.Nodes[0].Context?.GetOpen() ?? false).ShouldBeFalse();

        nodeCtx!.SetContext?.Invoke(new FakeRootContext(true));
        (treeCtx.Nodes[0].Context?.GetOpen() ?? false).ShouldBeTrue();

        return Task.CompletedTask;
    }
}
