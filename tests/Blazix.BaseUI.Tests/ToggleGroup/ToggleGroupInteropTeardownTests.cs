namespace Blazix.BaseUI.Tests.ToggleGroup;

public class ToggleGroupInteropTeardownTests : BunitContext
{
    private const string ToggleModule = "./_content/Blazix.BaseUI/blazix-baseui-toggle.min.js";

    public ToggleGroupInteropTeardownTests()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
    }

    [Fact]
    public void GroupedToggle_IgnoresDisposedModuleDuringTabIndexSync()
    {
        SetupToggleModule(
            syncGroupTabIndexesException:
                new ObjectDisposedException("Microsoft.JSInterop.Implementation.JSObjectReference"));

        var cut = Render(RenderToggleGroupWithToggles());

        cut.FindAll("button[aria-pressed]").Count.ShouldBe(2);
    }

    [Fact]
    public async Task ToggleGroup_IgnoresDisposedModuleDuringDispose()
    {
        SetupToggleModule(
            disposeGroupException:
                new ObjectDisposedException("Microsoft.JSInterop.Implementation.JSObjectReference"));

        var cut = Render(RenderToggleGroup());
        var group = cut.FindComponent<Blazix.BaseUI.ToggleGroup.ToggleGroup>();

        await group.Instance.DisposeAsync();
    }

    private BunitJSModuleInterop SetupToggleModule(
        Exception? syncGroupTabIndexesException = null,
        Exception? disposeGroupException = null)
    {
        var module = JSInterop.SetupModule(ToggleModule);
        module.SetupVoid("initialize", _ => true).SetVoidResult();
        module.SetupVoid("updateState", _ => true).SetVoidResult();
        module.SetupVoid("dispose", _ => true).SetVoidResult();
        module.SetupVoid("initializeGroup", _ => true).SetVoidResult();
        module.SetupVoid("updateGroup", _ => true).SetVoidResult();
        SetVoidResultOrException(module.SetupVoid("disposeGroup", _ => true), disposeGroupException);
        module.SetupVoid("registerToggle", _ => true).SetVoidResult();
        module.SetupVoid("unregisterToggle", _ => true).SetVoidResult();
        module.SetupVoid("navigateToPrevious", _ => true).SetVoidResult();
        module.SetupVoid("navigateToNext", _ => true).SetVoidResult();
        module.SetupVoid("navigateToFirst", _ => true).SetVoidResult();
        module.SetupVoid("navigateToLast", _ => true).SetVoidResult();
        SetVoidResultOrException(module.SetupVoid("syncGroupTabIndexes", _ => true), syncGroupTabIndexesException);
        module.SetupVoid("initializeGroupItem", _ => true).SetVoidResult();
        module.SetupVoid("updateGroupItemOrientation", _ => true).SetVoidResult();
        module.SetupVoid("disposeGroupItem", _ => true).SetVoidResult();
        return module;
    }

    private static void SetVoidResultOrException(
        JSRuntimeInvocationHandler handler,
        Exception? exception)
    {
        if (exception is null)
        {
            handler.SetVoidResult();
            return;
        }

        handler.SetException(exception);
    }

    private static RenderFragment RenderToggleGroup()
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.ToggleGroup.ToggleGroup>(0);
            builder.CloseComponent();
        };
    }

    private static RenderFragment RenderToggleGroupWithToggles()
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.ToggleGroup.ToggleGroup>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<Blazix.BaseUI.Toggle.Toggle>(0);
                innerBuilder.AddAttribute(1, "Value", "one");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "One")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<Blazix.BaseUI.Toggle.Toggle>(10);
                innerBuilder.AddAttribute(11, "Value", "two");
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Two")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }
}
