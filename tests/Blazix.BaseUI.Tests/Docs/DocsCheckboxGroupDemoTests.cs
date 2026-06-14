namespace Blazix.BaseUI.Tests.Docs;

public class DocsCheckboxGroupDemoTests : BunitContext
{
    public DocsCheckboxGroupDemoTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCheckboxModule(JSInterop);
    }

    [Fact]
    public void NestedDemo_MarksOuterParentMixedWhenNestedChildrenArePartiallySelected()
    {
        var cut = Render<Blazix.BaseUI.Docs.Client.Components.Demos.CheckboxGroup.Nested.Css.CheckboxGroupNestedCss>();

        AssertNestedDemoMarksOuterParentMixed(cut);
    }

    [Fact]
    public void NestedTailwindDemo_MarksOuterParentMixedWhenNestedChildrenArePartiallySelected()
    {
        var cut = Render<Blazix.BaseUI.Docs.Client.Components.Demos.CheckboxGroup.Nested.Tailwind.CheckboxGroupNestedTailwind>();

        AssertNestedDemoMarksOuterParentMixed(cut);
    }

    private static void AssertNestedDemoMarksOuterParentMixed<TComponent>(IRenderedComponent<TComponent> cut)
        where TComponent : IComponent
    {
        var createUserLabel = cut.FindAll("label")
            .Single(label => label.TextContent.Contains("Create user", StringComparison.Ordinal));
        createUserLabel.QuerySelector("input[type='checkbox']")!.Change(true);

        var outerParent = cut.FindAll("[data-parent]")[0];
        outerParent.GetAttribute("aria-checked").ShouldBe("mixed");
    }
}
