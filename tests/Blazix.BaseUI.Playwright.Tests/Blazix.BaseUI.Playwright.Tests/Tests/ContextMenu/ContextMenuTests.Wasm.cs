using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.ContextMenu;

public class ContextMenuTestsWasm : ContextMenuTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public ContextMenuTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    protected override async Task HoverSubmenuTriggerAsync()
    {
        await Page.EvaluateAsync("""
            () => {
                const trigger = document.querySelector('[data-testid="submenu-trigger"]');
                const rect = trigger.getBoundingClientRect();
                const eventInit = {
                    bubbles: true,
                    cancelable: true,
                    view: window,
                    clientX: rect.left + rect.width / 2,
                    clientY: rect.top + rect.height / 2,
                    relatedTarget: document.body
                };
                trigger.dispatchEvent(new MouseEvent('mouseover', eventInit));
                trigger.dispatchEvent(new MouseEvent('mouseenter', { ...eventInit, bubbles: false }));
            }
            """);

        await Task.Delay(300 * TimeoutMultiplier);
    }

    protected override async Task WaitForSubmenuPopupVisibleAsync()
    {
        await Page.WaitForFunctionAsync(
            """
            () => {
                const popup = document.querySelector('[data-testid="submenu-popup"]');
                if (!popup) return false;
                const style = getComputedStyle(popup);
                const rect = popup.getBoundingClientRect();
                return style.display !== 'none'
                    && style.visibility !== 'hidden'
                    && rect.width > 0
                    && rect.height > 0;
            }
            """,
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }
}
