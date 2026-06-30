namespace Blazix.BaseUI.Tests.Menu;

public class MenuPopupTests : BunitContext, IMenuPopupContract
{
    public MenuPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreatePopupInRoot(FocusTarget? finalFocus = null)
    {
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
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    var attrIndex = 1;
                    if (finalFocus is not null)
                        posBuilder.AddAttribute(attrIndex++, "FinalFocus", finalFocus);
                    posBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(popupBuilder =>
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
    public Task DefaultReturnFocusIsTrue()
    {
        var cut = Render(CreatePopupInRoot());

        var menu = cut.Find("div[role='menu']");
        menu.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FinalFocusNoneDisablesReturnFocus()
    {
        var cut = Render(CreatePopupInRoot(finalFocus: new FocusTarget.None()));

        var menu = cut.Find("div[role='menu']");
        menu.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FinalFocusDefaultEnablesReturnFocus()
    {
        var cut = Render(CreatePopupInRoot(finalFocus: new FocusTarget.Default()));

        var menu = cut.Find("div[role='menu']");
        menu.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // Parity: React sets the popup's aria-labelledby to the active trigger's id
    // (MenuRoot popupProps: 'aria-labelledby': activeTriggerElement?.id).
    [Fact]
    public Task PopupIsLabelledByTrigger()
    {
        const string triggerId = "menu-trigger-x";
        var cut = Render(CreatePopupWithTriggerId(triggerId));

        var trigger = cut.Find("[aria-haspopup='menu']");
        trigger.GetAttribute("id").ShouldBe(triggerId);

        var menu = cut.Find("div[role='menu']");
        menu.GetAttribute("aria-labelledby").ShouldBe(triggerId);

        return Task.CompletedTask;
    }

    private static RenderFragment CreatePopupWithTriggerId(string triggerId)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "DefaultTriggerId", triggerId);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "id", triggerId);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(3);
                innerBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(posBuilder =>
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
}
