namespace Blazix.BaseUI.Tests.Menu;

public class MenuPositionerTests : BunitContext, IMenuPositionerContract
{
    public MenuPositionerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreatePositionerInRoot(Align? align = null, bool nested = false)
    {
        if (nested)
        {
            return builder =>
            {
                // Outer MenuRoot (parent menu, open)
                builder.OpenComponent<MenuRoot>(0);
                builder.AddAttribute(1, "DefaultOpen", true);
                builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => outerBuilder =>
                {
                    outerBuilder.OpenComponent<MenuTrigger>(0);
                    outerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Outer Trigger")));
                    outerBuilder.CloseComponent();

                    outerBuilder.OpenComponent<MenuPositioner>(2);
                    outerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(outerPosBuilder =>
                    {
                        outerPosBuilder.OpenComponent<MenuPopup>(0);
                        outerPosBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(outerPopupBuilder =>
                        {
                            outerPopupBuilder.OpenComponent<MenuItem>(0);
                            outerPopupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Outer Item")));
                            outerPopupBuilder.CloseComponent();

                            // MenuSubmenuRoot wraps the inner menu
                            outerPopupBuilder.OpenComponent<MenuSubmenuRoot>(2);
                            outerPopupBuilder.AddAttribute(3, "DefaultOpen", true);
                            outerPopupBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(submenuBuilder =>
                            {
                                submenuBuilder.OpenComponent<MenuSubmenuTrigger>(0);
                                submenuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Submenu")));
                                submenuBuilder.CloseComponent();

                                submenuBuilder.OpenComponent<MenuPositioner>(2);
                                var attrIndex = 3;
                                if (align.HasValue)
                                    submenuBuilder.AddAttribute(attrIndex++, "Align", align.Value);
                                submenuBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerPosBuilder =>
                                {
                                    innerPosBuilder.OpenComponent<MenuPopup>(0);
                                    innerPosBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(innerPopupBuilder =>
                                    {
                                        innerPopupBuilder.OpenComponent<MenuItem>(0);
                                        innerPopupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Inner Item")));
                                        innerPopupBuilder.CloseComponent();
                                    }));
                                    innerPosBuilder.CloseComponent();
                                }));
                                submenuBuilder.CloseComponent();
                            }));
                            outerPopupBuilder.CloseComponent();
                        }));
                        outerPosBuilder.CloseComponent();
                    }));
                    outerBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            };
        }

        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                var attrIndex = 3;
                if (align.HasValue)
                    innerBuilder.AddAttribute(attrIndex++, "Align", align.Value);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task DefaultsAlignToCenter()
    {
        var cut = Render(CreatePositionerInRoot());

        var positioner = cut.Find("div[data-align]");
        positioner.GetAttribute("data-align")!.ShouldBe("center");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NestedMenuDefaultsAlignToStart()
    {
        var cut = Render(CreatePositionerInRoot(nested: true));

        var nestedPositioner = cut.Find("div[data-align][data-side='inline-end']");
        nestedPositioner.GetAttribute("data-align")!.ShouldBe("start");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExplicitAlignOverridesNestedDefault()
    {
        var cut = Render(CreatePositionerInRoot(align: Align.End, nested: true));

        var nestedPositioner = cut.Find("div[data-align][data-side='inline-end']");
        nestedPositioner.GetAttribute("data-align")!.ShouldBe("end");

        return Task.CompletedTask;
    }
}
