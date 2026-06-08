using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Toolbar;

public class ToolbarTestsWasm : ToolbarTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public ToolbarTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    // WASM keyboard navigation tests are skipped because the .NET WASM runtime's
    // JIT warmup causes the first EventCallback invocation to take 15-20+ seconds,
    // making arrow key navigation unreliable under concurrent test load.
    // These scenarios are fully covered by the Server mode tests.

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_ArrowRight_MovesFocusToNextItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_ArrowLeft_MovesFocusToPreviousItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_ArrowRight_LoopsToFirstItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_ArrowLeft_LoopsToLastItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_ArrowDown_DoesNotMoveFocus() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_Home_DoesNotMoveFocus() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_End_DoesNotMoveFocus() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_Rtl_ArrowLeft_MovesFocusToNextItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_Rtl_ArrowRight_MovesFocusToPreviousItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Vertical_ArrowDown_MovesFocusToNextItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Vertical_ArrowUp_MovesFocusToPreviousItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Vertical_ArrowDown_LoopsToFirst() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Vertical_ArrowRight_DoesNotMoveFocus() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Tab_MovesFocusOutOfToolbar() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Tab_MovesFocusIntoToolbar() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Tab_SkipsFirstDisabledNonFocusableItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task LoopDisabled_ArrowRight_StopsAtLastItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task LoopDisabled_ArrowLeft_StopsAtFirstItem() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task SkipsDisabledNonFocusableItems() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task FocusesDisabledFocusableItems() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task DisabledFocusableButton_DoesNotInvokeActivationHandlers() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Vertical_DisabledFocusableInput_SuppressesArrowUpDown() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task NavigatesThroughButtonsLinksAndInputs() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_InputArrowRight_UsesTextCursorUntilEnd() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Horizontal_InputArrowLeft_UsesTextCursorUntilStart() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task Vertical_InputArrowKeys_UseTextCursorBoundary() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task NavigatesThroughGroups() => Task.CompletedTask;

    [Fact(Skip = "WASM JIT warmup causes unreliable keyboard event processing")]
    public override Task DynamicOrientationChange_UpdatesKeyboardNavigation() => Task.CompletedTask;
}
