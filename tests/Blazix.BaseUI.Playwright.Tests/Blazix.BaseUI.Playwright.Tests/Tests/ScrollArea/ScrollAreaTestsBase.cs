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

        await Page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2 + 55);
        await Page.Mouse.UpAsync();

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
            buttons = 1,
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
    public virtual async Task ThumbDrag_MissedRelease_EndsDragInsteadOfHoverScrolling()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        var box = await VerticalThumb.BoundingBoxAsync();
        Assert.NotNull(box);

        var clientX = PointAcross(box!, 0.5);
        var clientY = PointDown(box, 0.5);

        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 1
        });
        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            buttons = 1,
            clientX,
            clientY = clientY + 40,
            pointerId = 1
        });

        await WaitForScrollTopGreaterThanAsync(0);
        var scrollTopAfterFirstMove = await GetScrollTopAsync();

        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            buttons = 0,
            clientX,
            clientY = clientY + 60,
            pointerId = 2
        });

        Assert.Equal(scrollTopAfterFirstMove, await GetScrollTopAsync());

        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            buttons = 1,
            clientX,
            clientY = clientY + 60,
            pointerId = 1
        });

        await WaitForScrollTopGreaterThanAsync(scrollTopAfterFirstMove);
        var scrollTopBeforeMissedRelease = await GetScrollTopAsync();

        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            buttons = 0,
            clientX,
            clientY = clientY + 80,
            pointerId = 1
        });

        Assert.Equal(scrollTopBeforeMissedRelease, await GetScrollTopAsync());
        await Assertions.Expect(VerticalScrollbar).Not.ToHaveAttributeAsync("data-scrolling", "");

        await VerticalThumb.DispatchEventAsync("pointermove", new
        {
            buttons = 0,
            clientX,
            clientY = clientY + 120,
            pointerId = 1
        });

        Assert.Equal(scrollTopBeforeMissedRelease, await GetScrollTopAsync());
    }

    [Fact]
    public virtual async Task ThumbDrag_SuspendsScrollSnapWhileDragging()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        await Viewport.EvaluateAsync("el => { el.style.scrollSnapType = 'y mandatory'; }");

        var box = await VerticalThumb.BoundingBoxAsync();
        Assert.NotNull(box);

        var clientX = PointAcross(box!, 0.5);
        var clientY = PointDown(box, 0.5);

        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        await VerticalThumb.DispatchEventAsync("pointerup", new
        {
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("y mandatory", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        await VerticalThumb.DispatchEventAsync("pointercancel", new
        {
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("y mandatory", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));
    }

    [Fact]
    public virtual async Task ThumbDrag_SecondPointer_CannotClobberSavedSnapState()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        await Viewport.EvaluateAsync("el => { el.style.scrollSnapType = 'y mandatory'; }");
        await VerticalThumb.EvaluateAsync(
            """
            el => {
                let capturedId = null;
                el.dropPointerCapture = () => { capturedId = null; };
                Object.defineProperties(el, {
                    setPointerCapture: { configurable: true, value: (id) => { capturedId = id; } },
                    hasPointerCapture: { configurable: true, value: (id) => id === capturedId },
                    releasePointerCapture: { configurable: true, value: (id) => { if (id === capturedId) { capturedId = null; } } }
                });
            }
            """);

        var box = await VerticalThumb.BoundingBoxAsync();
        Assert.NotNull(box);

        var clientX = PointAcross(box!, 0.5);
        var clientY = PointDown(box, 0.5);

        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        // While the first pointer still holds capture, a second pointer is ignored outright.
        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 2
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        await VerticalThumb.DispatchEventAsync("pointerup", new
        {
            clientX,
            clientY,
            pointerId = 2
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        // Drop capture silently, the way a browser can mid-drag. The second pointer now
        // takes over the latch and re-enters the snap-disable path with a drag already in
        // flight, which is the only way to reach the saved-state guard: without it the
        // takeover would save the live 'none' and the release would strand the viewport
        // unsnapped.
        await VerticalThumb.EvaluateAsync("el => el.dropPointerCapture()");

        await VerticalThumb.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 2
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        // The superseded pointer's release must not restore anything.
        await VerticalThumb.DispatchEventAsync("pointerup", new
        {
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        await VerticalThumb.DispatchEventAsync("pointerup", new
        {
            clientX,
            clientY,
            pointerId = 2
        });

        Assert.Equal("y mandatory", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));
    }

    [Fact]
    public virtual async Task TrackPress_DisablesSnapForJumpToClickAndRestoresOnRelease()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area"));
        await WaitForMeasuredOverflowAsync();

        await Viewport.EvaluateAsync("el => { el.style.scrollSnapType = 'y mandatory'; }");
        var initialScrollTop = await GetScrollTopAsync();

        var box = await VerticalScrollbar.BoundingBoxAsync();
        Assert.NotNull(box);

        var clientX = PointAcross(box!, 0.5);
        var clientY = PointDown(box, 0.85);

        await VerticalScrollbar.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 1
        });

        await WaitForScrollTopGreaterThanAsync(initialScrollTop);
        Assert.True(await GetScrollTopAsync() > initialScrollTop);
        Assert.Equal("none", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));

        await VerticalScrollbar.DispatchEventAsync("pointerup", new
        {
            clientX,
            clientY,
            pointerId = 1
        });

        Assert.Equal("y mandatory", await Viewport.EvaluateAsync<string>("el => el.style.scrollSnapType"));
    }

    [Fact]
    public virtual async Task RtlTrackPress_AssignsNegativeScrollLeft()
    {
        await NavigateAsync(CreateUrl("/tests/scroll-area").WithScrollAreaDirection("rtl"));
        await WaitForMeasuredOverflowAsync();

        var box = await HorizontalScrollbar.BoundingBoxAsync();
        Assert.NotNull(box);

        var scrollRange = await Viewport.EvaluateAsync<double>("el => el.scrollWidth - el.clientWidth");
        Assert.True(scrollRange > 0);

        // In RTL, `scrollLeft` runs from `-scrollRange` (content end) up to 0 (content
        // start), and the track is mirrored: pressing the left of the track jumps to the
        // end, pressing the right jumps back to the start. Asserting only that a single
        // press produces a negative value cannot distinguish the RTL mapping from the LTR
        // one, because a press left of the thumb's half-width yields a negative ratio and
        // so goes negative under either formula.
        var atTrackEnd = await PressRtlTrackAsync(box!, 0.15);
        Assert.True(
            atTrackEnd <= -scrollRange * 0.9,
            $"Pressing the end of an RTL track should scroll to about {-scrollRange}, but scrollLeft was {atTrackEnd}.");

        var atTrackMiddle = await PressRtlTrackAsync(box, 0.5);
        Assert.True(
            Math.Abs(atTrackMiddle - (-scrollRange / 2)) <= scrollRange * 0.2,
            $"Pressing the middle of an RTL track should scroll to about {-scrollRange / 2}, but scrollLeft was {atTrackMiddle}.");

        var atTrackStart = await PressRtlTrackAsync(box, 0.85);
        Assert.True(
            atTrackStart >= -scrollRange * 0.1,
            $"Pressing the start of an RTL track should scroll to about 0, but scrollLeft was {atTrackStart}.");
    }

    private async Task<double> PressRtlTrackAsync(LocatorBoundingBoxResult box, double trackFraction)
    {
        var clientX = PointAcross(box, trackFraction);
        var clientY = PointDown(box, 0.5);

        await HorizontalScrollbar.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            clientX,
            clientY,
            pointerId = 1
        });

        var scrollLeft = await Viewport.EvaluateAsync<double>("el => el.scrollLeft");

        await HorizontalScrollbar.DispatchEventAsync("pointerup", new
        {
            clientX,
            clientY,
            pointerId = 1
        });

        return scrollLeft;
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

    // Coordinates dispatched into the browser must be `double`. Playwright's argument
    // serializer has no case for `float`: it walks one as a property-less object, so the
    // value arrives as `{}` and `new PointerEvent(...)` rejects it as non-finite. Every
    // `BoundingBox` member is a `float`, so arithmetic over them stays `float` unless a
    // `double` is involved — which is why these return `double` rather than inferring.
    private static double PointAcross(LocatorBoundingBoxResult box, double fraction) =>
        box.X + box.Width * fraction;

    private static double PointDown(LocatorBoundingBoxResult box, double fraction) =>
        box.Y + box.Height * fraction;

    private async Task<double> GetScrollTopAsync()
    {
        return await Viewport.EvaluateAsync<double>("el => el.scrollTop");
    }

    private async Task WaitForScrollTopGreaterThanAsync(double previousValue)
    {
        await Page.WaitForFunctionAsync(
            "(value) => document.querySelector('[data-testid=\"scroll-viewport\"]').scrollTop > value",
            previousValue,
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
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
