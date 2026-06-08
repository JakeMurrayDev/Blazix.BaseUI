using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;

namespace Blazix.BaseUI.Playwright.Tests.Tests.ToggleGroup;

public class ToggleGroupTestsWasm : ToggleGroupTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public ToggleGroupTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    // WASM keyboard navigation tests are skipped because the .NET WASM runtime's
    // JIT warmup causes the first EventCallback invocation to take 15-20+ seconds,
    // making arrow key navigation unreliable under concurrent test load.
    // These scenarios are fully covered by the Server mode tests.

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task ArrowRight_MovesToNextToggle() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task ArrowLeft_MovesToPreviousToggle() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task ArrowRight_WrapsToFirst() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task ArrowLeft_WrapsToLast() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task VerticalOrientation_ArrowDownUp() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task HorizontalOrientation_IgnoresVerticalArrowKeys() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task HomeEnd_MoveFocusToFirstAndLastToggle() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task LoopFocusFalse_DoesNotWrapPastLastToggle() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task ArrowNavigation_SkipsDisabledToggle() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task RtlHorizontal_ArrowLeftMovesToNextToggle() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task ToolbarMode_ArrowNavigationUsesToolbarRoot() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Tab_FocusesFirstEnabledToggle() => Task.CompletedTask;
}
