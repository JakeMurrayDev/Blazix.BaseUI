using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.ScrollArea;

public abstract class ScrollAreaTestsBase : TestBase
{
    protected ScrollAreaTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    private ILocator Root => GetByTestId("scroll-root");

    private ILocator Viewport => GetByTestId("scroll-viewport");

    private ILocator Content => GetByTestId("scroll-content");

    private ILocator VerticalScrollbar => GetByTestId("vertical-scrollbar");

    private ILocator HorizontalScrollbar => GetByTestId("horizontal-scrollbar");

    private ILocator VerticalThumb => GetByTestId("vertical-thumb");

    private ILocator HorizontalThumb => GetByTestId("horizontal-thumb");

    private ILocator Corner => GetByTestId("scroll-corner");

    [Fact]
    public virtual async Task InitialMeasurement_AppliesOverflowAttributesAndThumbGeometry()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        await Assertions.Expect(Root).ToHaveAttributeAsync("data-has-overflow-x", "");
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-has-overflow-y", "");
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-overflow-x-start", "");
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-overflow-x-end", "");
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-overflow-y-start", "");
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-overflow-y-end", "");

        await Assertions.Expect(Viewport).ToHaveAttributeAsync("tabindex", "0");
        await Assertions.Expect(Content).ToHaveAttributeAsync("data-has-overflow-x", "");
        await Assertions.Expect(VerticalScrollbar).ToHaveAttributeAsync("data-orientation", "vertical");
        await Assertions.Expect(HorizontalScrollbar).ToHaveAttributeAsync("data-orientation", "horizontal");
        await Assertions.Expect(Corner).ToBeVisibleAsync();

        var thumbSizes = await Page.EvaluateAsync<ThumbSizes>(
            """
            () => {
                const vertical = document.querySelector('[data-testid="vertical-thumb"]');
                const horizontal = document.querySelector('[data-testid="horizontal-thumb"]');
                return {
                    verticalHeight: vertical.getBoundingClientRect().height,
                    horizontalWidth: horizontal.getBoundingClientRect().width
                };
            }
            """);

        Assert.True(thumbSizes.VerticalHeight >= 16);
        Assert.True(thumbSizes.HorizontalWidth >= 16);

        var geometryIsAligned = await Page.EvaluateAsync<bool>(
            """
            () => {
                const verticalThumb = document.querySelector('[data-testid="vertical-thumb"]');
                const horizontalThumb = document.querySelector('[data-testid="horizontal-thumb"]');
                const verticalTrack = document.querySelector('[data-testid="vertical-scrollbar"]');
                const horizontalTrack = document.querySelector('[data-testid="horizontal-scrollbar"]');
                const corner = document.querySelector('[data-testid="scroll-corner"]');

                const verticalThumbRect = verticalThumb.getBoundingClientRect();
                const horizontalThumbRect = horizontalThumb.getBoundingClientRect();
                const verticalTrackRect = verticalTrack.getBoundingClientRect();
                const horizontalTrackRect = horizontalTrack.getBoundingClientRect();
                const cornerRect = corner.getBoundingClientRect();
                const verticalTrackStyle = getComputedStyle(verticalTrack);
                const horizontalTrackStyle = getComputedStyle(horizontalTrack);
                const verticalPaddingStart = parseFloat(verticalTrackStyle.paddingBlockStart) || 0;
                const horizontalPaddingStart = parseFloat(horizontalTrackStyle.paddingInlineStart) || 0;
                const tolerance = 0.5;

                const thumbsStayInsideTracks = verticalThumbRect.top >= verticalTrackRect.top - tolerance
                    && verticalThumbRect.bottom <= verticalTrackRect.bottom + tolerance
                    && horizontalThumbRect.left >= horizontalTrackRect.left - tolerance
                    && horizontalThumbRect.right <= horizontalTrackRect.right + tolerance;

                const thumbsStartAtScrollOrigin =
                    Math.abs(verticalThumbRect.top - (verticalTrackRect.top + verticalPaddingStart)) <= tolerance
                    && Math.abs(horizontalThumbRect.left - (horizontalTrackRect.left + horizontalPaddingStart)) <= tolerance;

                const tracksMeetCorner = Math.abs(horizontalTrackRect.right - verticalTrackRect.left) <= tolerance
                    && Math.abs(verticalTrackRect.bottom - horizontalTrackRect.top) <= tolerance
                    && Math.abs(cornerRect.left - horizontalTrackRect.right) <= tolerance
                    && Math.abs(cornerRect.top - verticalTrackRect.bottom) <= tolerance
                    && Math.abs(cornerRect.right - verticalTrackRect.right) <= tolerance
                    && Math.abs(cornerRect.bottom - horizontalTrackRect.bottom) <= tolerance;

                return thumbsStayInsideTracks && thumbsStartAtScrollOrigin && tracksMeetCorner;
            }
            """);

        Assert.True(geometryIsAligned);
    }

    [Fact]
    public virtual async Task ViewportScroll_UpdatesOverflowCssVarsAndScrollingState()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        await Viewport.HoverAsync();
        await Viewport.EvaluateAsync(
            """
            el => {
                el.scrollTop = 120;
                el.scrollLeft = 140;
                el.dispatchEvent(new Event('scroll', { bubbles: true }));
            }
            """);

        await Assertions.Expect(Root).ToHaveAttributeAsync("data-scrolling", "");
        await Assertions.Expect(Viewport).ToHaveAttributeAsync("data-overflow-x-start", "");
        await Assertions.Expect(Viewport).ToHaveAttributeAsync("data-overflow-y-start", "");

        var metrics = await Viewport.EvaluateAsync<OverflowMetrics>(
            """
            el => ({
                xStart: el.style.getPropertyValue('--scroll-area-overflow-x-start'),
                yStart: el.style.getPropertyValue('--scroll-area-overflow-y-start')
            })
            """);

        Assert.Equal("140px", metrics.XStart);
        Assert.Equal("120px", metrics.YStart);

        await WaitForDelayAsync(700);
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-scrolling", "");
    }

    [Fact]
    public virtual async Task TrackClickAndThumbDrag_UpdateViewportScrollPosition()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        var initialScrollTop = await GetScrollTopAsync();

        await VerticalScrollbar.ClickAsync(new LocatorClickOptions
        {
            Position = new Position { X = 7, Y = 90 }
        });

        await WaitForScrollTopGreaterThanAsync(initialScrollTop);
        var afterTrackClick = await GetScrollTopAsync();

        var box = await VerticalThumb.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = (double)box!.X + (double)box.Width / 2;
        var startY = (double)box.Y + (double)box.Height / 2;
        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            buttons = 1,
            clientX = startX,
            clientY = startY,
            pointerId = 1,
            pointerType = "mouse"
        });
        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            buttons = 1,
            clientX = startX,
            clientY = startY + 55,
            pointerId = 1,
            pointerType = "mouse"
        });
        await VerticalThumb.DispatchEventAsync("pointerup", new
        {
            button = 0,
            clientX = startX,
            clientY = startY + 55,
            pointerId = 1,
            pointerType = "mouse"
        });

        await WaitForScrollTopGreaterThanAsync(afterTrackClick);
    }

    [Fact]
    public virtual async Task ThumbDrag_WithZeroTrackTravel_DoesNotJumpViewport()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        var metrics = await Page.EvaluateAsync<ThumbTravelMetrics>(
            """
            () => {
                const track = document.querySelector('[data-testid="vertical-scrollbar"]');
                const thumb = document.querySelector('[data-testid="vertical-thumb"]');
                const thumbHeight = thumb.getBoundingClientRect().height;

                track.style.bottom = 'auto';
                track.style.height = `${thumbHeight}px`;

                const trackStyle = getComputedStyle(track);
                const thumbStyle = getComputedStyle(thumb);
                const scrollbarYOffset =
                    (parseFloat(trackStyle.paddingBlockStart) || 0) +
                    (parseFloat(trackStyle.paddingBlockEnd) || 0);
                const thumbYOffset =
                    (parseFloat(thumbStyle.marginBlockStart) || 0) +
                    (parseFloat(thumbStyle.marginBlockEnd) || 0);
                const thumbRect = thumb.getBoundingClientRect();

                return {
                    clientX: thumbRect.left + thumbRect.width / 2,
                    clientY: thumbRect.top + thumbRect.height / 2,
                    maxThumbOffset:
                        track.offsetHeight - thumb.offsetHeight - scrollbarYOffset - thumbYOffset
                };
            }
            """);

        Assert.True(metrics.MaxThumbOffset <= 0);

        var initialScrollTop = await GetScrollTopAsync();

        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX = metrics.ClientX,
            clientY = metrics.ClientY,
            pointerId = 1
        });
        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            clientX = metrics.ClientX,
            clientY = metrics.ClientY + 40,
            pointerId = 1
        });
        await VerticalThumb.DispatchEventAsync("pointerup", new
        {
            clientX = metrics.ClientX,
            clientY = metrics.ClientY + 40,
            pointerId = 1
        });

        var scrollTopAfterDrag = await GetScrollTopAsync();
        Assert.Equal(initialScrollTop, scrollTopAfterDrag);
    }

    [Fact]
    public virtual async Task KeepMountedWithoutOverflow_RendersTracksWithoutOverflowState()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area")
            .WithScrollAreaSmallContent(true)
            .WithScrollAreaKeepMounted(true));

        await Assertions.Expect(VerticalScrollbar).ToBeVisibleAsync();
        await Assertions.Expect(HorizontalScrollbar).ToBeVisibleAsync();
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-has-overflow-x", "");
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-has-overflow-y", "");
        await Assertions.Expect(Corner).ToHaveCountAsync(0);
    }

    [Fact]
    public virtual async Task Focus_LeavesScrollableViewportInTabOrder()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        await Viewport.FocusAsync();

        var activeTestId = await Page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-testid')");
        Assert.Equal("scroll-viewport", activeTestId);
    }

    [Fact]
    public virtual async Task OverflowEdgeThreshold_DelaysStartEdgeAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area").WithScrollAreaThreshold(30));
        await WaitForMeasuredOverflowAsync();

        await Viewport.HoverAsync();
        await Viewport.EvaluateAsync(
            """
            el => {
                el.scrollTop = 20;
                el.scrollLeft = 20;
                el.dispatchEvent(new Event('scroll', { bubbles: true }));
            }
            """);

        await Assertions.Expect(Viewport).Not.ToHaveAttributeAsync("data-overflow-x-start", "");
        await Assertions.Expect(Viewport).Not.ToHaveAttributeAsync("data-overflow-y-start", "");

        await Viewport.EvaluateAsync(
            """
            el => {
                el.scrollTop = 40;
                el.scrollLeft = 40;
                el.dispatchEvent(new Event('scroll', { bubbles: true }));
            }
            """);

        await Assertions.Expect(Viewport).ToHaveAttributeAsync("data-overflow-x-start", "");
        await Assertions.Expect(Viewport).ToHaveAttributeAsync("data-overflow-y-start", "");
    }

    [Fact]
    public virtual async Task RtlHorizontalScrolling_UsesNegativeScrollLeftEdges()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area").WithScrollAreaDirection("rtl"));
        await WaitForMeasuredOverflowAsync();

        await Viewport.EvaluateAsync(
            """
            el => {
                const max = el.scrollWidth - el.clientWidth;
                el.scrollLeft = -max / 2;
                el.dispatchEvent(new Event('scroll', { bubbles: true }));
            }
            """);

        await Assertions.Expect(Root).ToHaveAttributeAsync("data-overflow-x-start", "");
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-overflow-x-end", "");

        await Viewport.EvaluateAsync(
            """
            el => {
                const max = el.scrollWidth - el.clientWidth;
                el.scrollLeft = -max;
                el.dispatchEvent(new Event('scroll', { bubbles: true }));
            }
            """);

        await Assertions.Expect(Root).ToHaveAttributeAsync("data-overflow-x-start", "");
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-overflow-x-end", "");
    }

    private async Task WaitForMeasuredOverflowAsync()
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-has-overflow-x", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 10000 * TimeoutMultiplier
        });
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-has-overflow-y", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 10000 * TimeoutMultiplier
        });
        await Assertions.Expect(VerticalThumb).ToBeVisibleAsync();
        await Assertions.Expect(HorizontalThumb).ToBeVisibleAsync();
    }

    private async Task<double> GetScrollTopAsync()
    {
        return await Viewport.EvaluateAsync<double>("el => el.scrollTop");
    }

    private async Task WaitForScrollTopGreaterThanAsync(double previousValue)
    {
        await Page.WaitForFunctionAsync(
            "(value) => document.querySelector('[data-testid=\"scroll-viewport\"]').scrollTop > value",
            previousValue,
            new PageWaitForFunctionOptions { Timeout = 10000 * TimeoutMultiplier });
    }

    private sealed class ThumbSizes
    {
        public double VerticalHeight { get; set; }

        public double HorizontalWidth { get; set; }
    }

    private sealed class ThumbTravelMetrics
    {
        public double ClientX { get; set; }

        public double ClientY { get; set; }

        public double MaxThumbOffset { get; set; }
    }

    private sealed class OverflowMetrics
    {
        public string XStart { get; set; } = string.Empty;

        public string YStart { get; set; } = string.Empty;
    }
}
