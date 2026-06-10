using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Playwright.Tests.Components.Pages.Tests;

public static class RenderModeHelper
{
    public static IComponentRenderMode? GetRenderMode(string? mode)
    {
        return mode?.ToLowerInvariant() switch
        {
            "server" => RenderMode.InteractiveServer,
            "wasm" => RenderMode.InteractiveWebAssembly,
            _ => RenderMode.InteractiveServer
        };
    }
}
