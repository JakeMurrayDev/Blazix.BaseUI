using BlazorBaseUI.Select;
using BlazorBaseUI.Tests.Contracts.Select;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Select;

public class SelectConformanceTests : BunitContext, ISelectConformanceContract
{
    public SelectConformanceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectRoot(bool defaultOpen, RenderFragment childContent)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectWithPortal(bool defaultOpen, RenderFragment portalContent)
    {
        return CreateSelectRoot(defaultOpen, (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<SelectTrigger>(0);
            innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
            innerBuilder.CloseComponent();

            innerBuilder.OpenComponent<SelectPortal>(10);
            innerBuilder.AddAttribute(11, "KeepMounted", true);
            innerBuilder.AddAttribute(12, "ChildContent", portalContent);
            innerBuilder.CloseComponent();
        }));
    }

    private RenderFragment CreateSelectWithPositioner(bool defaultOpen, RenderFragment positionerContent)
    {
        return CreateSelectWithPortal(defaultOpen, (RenderFragment)(portalBuilder =>
        {
            portalBuilder.OpenComponent<SelectPositioner>(0);
            portalBuilder.AddAttribute(1, "ChildContent", positionerContent);
            portalBuilder.CloseComponent();
        }));
    }

    private RenderFragment CreateSelectWithPopup(bool defaultOpen, RenderFragment popupContent)
    {
        return CreateSelectWithPositioner(defaultOpen, (RenderFragment)(posBuilder =>
        {
            posBuilder.OpenComponent<SelectPopup>(0);
            posBuilder.AddAttribute(1, "ChildContent", popupContent);
            posBuilder.CloseComponent();
        }));
    }

    [Fact]
    public Task SelectArrow_RendersAsDiv()
    {
        // Arrow is intentionally suppressed when AlignItemWithTriggerActive is
        // true (matches React's `if (alignItemWithTriggerActive) return null`),
        // so this test opts out of align-item mode.
        var cut = Render(CreateSelectRoot(defaultOpen: true, (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<SelectTrigger>(0);
            innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
            innerBuilder.CloseComponent();

            innerBuilder.OpenComponent<SelectPortal>(10);
            innerBuilder.AddAttribute(11, "KeepMounted", true);
            innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
            {
                portalBuilder.OpenComponent<SelectPositioner>(0);
                portalBuilder.AddAttribute(1, "AlignItemWithTrigger", false);
                portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectArrow>(0);
                    posBuilder.CloseComponent();
                }));
                portalBuilder.CloseComponent();
            }));
            innerBuilder.CloseComponent();
        })));

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectBackdrop_RendersAsDiv()
    {
        var cut = Render(CreateSelectRoot(defaultOpen: false, (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<SelectBackdrop>(0);
            innerBuilder.CloseComponent();
        })));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectGroup_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPopup(defaultOpen: true, (RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<SelectGroup>(0);
            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Group")));
            popupBuilder.CloseComponent();
        })));

        var group = cut.Find("[role='group']");
        group.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectGroupLabel_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPopup(defaultOpen: true, (RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<SelectGroup>(0);
            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<SelectGroupLabel>(0);
                groupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Label")));
                groupBuilder.CloseComponent();
            }));
            popupBuilder.CloseComponent();
        })));

        // SelectGroupLabel has role="presentation"
        // Find the presentation element inside the group
        var group = cut.Find("[role='group']");
        var label = group.QuerySelector("[role='presentation']");
        label.ShouldNotBeNull();
        label!.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectIcon_RendersAsSpan()
    {
        var cut = Render(CreateSelectRoot(defaultOpen: false, (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<SelectIcon>(0);
            innerBuilder.CloseComponent();
        })));

        var icon = cut.Find("span[aria-hidden='true']");
        icon.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectItem_RendersAsDivWithOptionRole()
    {
        var cut = Render(CreateSelectWithPopup(defaultOpen: true, (RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<SelectItem<string>>(0);
            popupBuilder.AddAttribute(1, "Value", "a");
            popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item A")));
            popupBuilder.CloseComponent();
        })));

        var item = cut.Find("[role='option']");
        item.ShouldNotBeNull();
        item.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectItemIndicator_RendersAsSpan()
    {
        var cut = Render(CreateSelectWithPopup(defaultOpen: true, (RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<SelectItem<string>>(0);
            popupBuilder.AddAttribute(1, "Value", "a");
            popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
            {
                itemBuilder.OpenComponent<SelectItemIndicator>(0);
                itemBuilder.AddAttribute(1, "KeepMounted", true);
                itemBuilder.CloseComponent();
            }));
            popupBuilder.CloseComponent();
        })));

        // SelectItemIndicator renders as span with aria-hidden="true"
        // Find it inside the option element
        var item = cut.Find("[role='option']");
        var indicator = item.QuerySelector("span[aria-hidden='true']");
        indicator.ShouldNotBeNull();
        indicator!.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectItemText_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPopup(defaultOpen: true, (RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<SelectItem<string>>(0);
            popupBuilder.AddAttribute(1, "Value", "a");
            popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
            {
                itemBuilder.OpenComponent<SelectItemText>(0);
                itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Text")));
                itemBuilder.CloseComponent();
            }));
            popupBuilder.CloseComponent();
        })));

        // SelectItemText renders as a plain div inside the option
        var item = cut.Find("[role='option']");
        var textDiv = item.QuerySelector("div");
        textDiv.ShouldNotBeNull();
        textDiv!.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectList_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPopup(defaultOpen: true, (RenderFragment)(popupBuilder =>
        {
            popupBuilder.OpenComponent<SelectList>(0);
            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "List")));
            popupBuilder.CloseComponent();
        })));

        var list = cut.Find("[role='listbox']");
        list.ShouldNotBeNull();
        list.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectPopup_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPositioner(defaultOpen: true, (RenderFragment)(posBuilder =>
        {
            posBuilder.OpenComponent<SelectPopup>(0);
            posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup")));
            posBuilder.CloseComponent();
        })));

        // SelectPopup without a SelectList child gets role="listbox"
        var popup = cut.Find("div[role='listbox']");
        popup.ShouldNotBeNull();
        popup.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectPortal_RendersAsDiv()
    {
        var cut = Render(CreateSelectRoot(defaultOpen: true, (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<SelectTrigger>(0);
            innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
            innerBuilder.CloseComponent();

            innerBuilder.OpenComponent<SelectPortal>(10);
            innerBuilder.AddAttribute(11, "KeepMounted", true);
            innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Portal content")));
            innerBuilder.CloseComponent();
        })));

        // Portal renders a div with data-blazor-base-ui-portal
        var portal = cut.Find("[data-blazor-base-ui-portal]");
        portal.ShouldNotBeNull();
        portal.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectPositioner_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPortal(defaultOpen: true, (RenderFragment)(portalBuilder =>
        {
            portalBuilder.OpenComponent<SelectPositioner>(0);
            portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Positioner")));
            portalBuilder.CloseComponent();
        })));

        // SelectPositioner renders with role="presentation"
        var positioner = cut.Find("[role='presentation']");
        positioner.ShouldNotBeNull();
        positioner.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectScrollDownArrow_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPositioner(defaultOpen: true, (RenderFragment)(posBuilder =>
        {
            posBuilder.OpenComponent<SelectScrollDownArrow>(0);
            posBuilder.AddAttribute(1, "KeepMounted", true);
            posBuilder.CloseComponent();
        })));

        var arrow = cut.Find("div[data-direction='down']");
        arrow.ShouldNotBeNull();
        arrow.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectScrollUpArrow_RendersAsDiv()
    {
        var cut = Render(CreateSelectWithPositioner(defaultOpen: true, (RenderFragment)(posBuilder =>
        {
            posBuilder.OpenComponent<SelectScrollUpArrow>(0);
            posBuilder.AddAttribute(1, "KeepMounted", true);
            posBuilder.CloseComponent();
        })));

        var arrow = cut.Find("div[data-direction='up']");
        arrow.ShouldNotBeNull();
        arrow.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectValue_RendersAsSpan()
    {
        var cut = Render(CreateSelectRoot(defaultOpen: false, (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<SelectValue<string>>(0);
            innerBuilder.AddAttribute(1, "Placeholder", "Choose...");
            innerBuilder.CloseComponent();
        })));

        var value = cut.Find("span");
        value.ShouldNotBeNull();
        value.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }
}
