using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Parity.Tests.Tests;
using Shouldly;
using SkiaSharp;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Verifies that a manifest entry round-trips through a real browser into a capture bundle.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
// Capture tests drive timing-sensitive Server/WASM circuits through the one assembly server and
// take repeated screenshots. Running them beside other browser collections can starve a dispatched
// action past its consequence deadline, which is a load artifact rather than fixture evidence.
[Collection(ParityTimingCollection.Name)]
public sealed class ParityCapturerTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>, IDisposable
{
    /// <summary>
    /// Installs a twenty-second animation inside the fixture root once the root exists.
    /// </summary>
    /// <remarks>
    /// Injected from the test rather than added to <c>CaptureProbe.razor</c>, so that the
    /// probe every other capture test reads keeps the markup those tests were written
    /// against and only this test sees a page with something to seek.
    /// </remarks>
    private const string DriftScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            if (!root) { requestAnimationFrame(install); return; }
            const style = document.createElement('style');
            style.textContent =
              '@keyframes parity-drift { from { opacity: 0 } to { opacity: 1 } }' +
              '#parity-drift { animation: parity-drift 20s linear infinite; }';
            document.head.appendChild(style);
            const drift = document.createElement('div');
            drift.id = 'parity-drift';
            drift.textContent = 'drift';
            root.appendChild(drift);
          };
          install();
        })();
        """;

    /// <summary>
    /// Installs the same twenty-second animation as <see cref="DriftScript"/>, but only when
    /// the probe's button is clicked.
    /// </summary>
    /// <remarks>
    /// This is what lets one fixture hold an animation step with nothing to seek followed by
    /// one with something to seek: the first step finds <c>getAnimations()</c> empty, the
    /// second finds it populated. A step that has to wait for a click is the ordinary shape
    /// of an animated fixture — a popup opens on a trigger — so this is the sequence the
    /// corpus will actually produce, not a contrived one.
    /// </remarks>
    private const string DeferredDriftScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            const button = root?.querySelector('button');
            if (!button) { requestAnimationFrame(install); return; }
            const style = document.createElement('style');
            style.textContent =
              '@keyframes parity-drift { from { opacity: 0 } to { opacity: 1 } }' +
              '#parity-drift { animation: parity-drift 20s linear infinite; }';
            document.head.appendChild(style);
            button.addEventListener('click', () => {
              const drift = document.createElement('div');
              drift.id = 'parity-drift';
              drift.textContent = 'drift';
              root.appendChild(drift);
            });
          };
          install();
        })();
        """;

    /// <summary>
    /// Appends a body child with no size, which <c>capture.js</c> counts as a capture root
    /// and Playwright cannot photograph. Appended before Blazix moves its own portal
    /// container out, so this is <c>portal(1)</c> and the real one is <c>portal(2)</c>.
    /// </summary>
    private const string EmptyPortalScript = """
        (() => {
          const install = () => {
            if (!document.body) { requestAnimationFrame(install); return; }
            const empty = document.createElement('div');
            empty.style.width = '0';
            empty.style.height = '0';
            document.body.appendChild(empty);
          };
          install();
        })();
        """;

    private const string ZeroBoxPortalWithAbsoluteChildScript = """
        (() => {
          const install = () => {
            if (!document.body) { requestAnimationFrame(install); return; }
            const portal = document.createElement('div');
            portal.style.cssText = 'position:relative;width:0;height:0';
            const popup = document.createElement('div');
            popup.textContent = 'absolute popup';
            popup.style.cssText = 'position:absolute;left:240px;top:160px;width:80px;height:40px;background:red';
            portal.appendChild(popup);
            document.body.appendChild(portal);
          };
          install();
        })();
        """;

    private const string OffViewportPortalAnimationScript = """
        (() => {
          const install = () => {
            if (!document.body) { requestAnimationFrame(install); return; }
            const portal = document.createElement('div');
            portal.id = 'parity-off-viewport-portal';
            portal.textContent = 'off viewport then visible';
            portal.style.cssText =
              'position:fixed;left:24px;top:24px;width:160px;height:40px;background:red';
            document.body.appendChild(portal);
            portal.animate(
              [
                { transform: 'translateY(calc(100vh + 100px))' },
                { transform: 'translateY(0)' }
              ],
              { duration: 60000, fill: 'both' });
          };
          install();
        })();
        """;

    private const string ReplacementAnimationScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            const button = root?.querySelector('button');
            if (!root || !button) { requestAnimationFrame(install); return; }

            const style = document.createElement('style');
            style.textContent =
              '@keyframes parity-replacement { from { opacity: 0 } to { opacity: 1 } }' +
              '.parity-replacement { animation: parity-replacement 120ms linear; }';
            document.head.appendChild(style);

            button.addEventListener('click', () => {
              const first = document.createElement('div');
              first.id = 'parity-first';
              first.textContent = 'first phase';
              root.appendChild(first);

              requestAnimationFrame(() => {
                first.className = 'parity-replacement';
                first.getAnimations()[0].finished.then(() => {
                  first.remove();
                  setTimeout(() => {
                    const second = document.createElement('div');
                    second.id = 'parity-second';
                    second.className = 'parity-replacement';
                    second.textContent = 'second phase';
                    root.appendChild(second);

                    second.getAnimations()[0].finished.then(() => {
                      second.remove();
                      const terminal = document.createElement('div');
                      terminal.id = 'parity-terminal';
                      terminal.textContent = 'terminal state';
                      root.appendChild(terminal);
                    });
                  }, 0);
                });
              });
            }, { once: true });
          };
          install();
        })();
        """;

    private const string ConsecutiveAnimationScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            const button = root?.querySelector('button');
            if (!root || !button) { requestAnimationFrame(install); return; }

            const style = document.createElement('style');
            style.textContent =
              '@keyframes parity-open { from { opacity: 0 } to { opacity: 1 } }' +
              '@keyframes parity-close { from { opacity: 1 } to { opacity: 0 } }';
            document.head.appendChild(style);
            root.dataset.parityCompletions = '0';

            button.addEventListener('click', () => {
              const existing = root.querySelector('#parity-panel');
              if (!existing) {
                const panel = document.createElement('div');
                panel.id = 'parity-panel';
                panel.textContent = 'animated panel';
                panel.style.animation = 'parity-open 120ms linear';
                root.appendChild(panel);
                panel.getAnimations()[0].finished.then(() => {
                  panel.style.animation = 'none';
                  panel.dataset.phase = 'open';
                  root.dataset.parityCompletions =
                    String(Number(root.dataset.parityCompletions) + 1);
                });
                return;
              }

              existing.style.animation = 'parity-close 120ms linear';
              existing.getAnimations()[0].finished.then(() => {
                existing.remove();
                root.dataset.parityCompletions =
                  String(Number(root.dataset.parityCompletions) + 1);
              });
            });
          };
          install();
        })();
        """;

    private const string EarlierActionAnimationScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            const button = root?.querySelector('button');
            if (!root || !button) { requestAnimationFrame(install); return; }

            let clicks = 0;
            button.addEventListener('click', () => {
              clicks += 1;
              if (clicks === 1) {
                const moving = document.createElement('div');
                moving.id = 'parity-earlier-action';
                moving.textContent = 'earlier action motion';
                root.appendChild(moving);
                moving.animate(
                  [{ opacity: 0 }, { opacity: 1 }],
                  { duration: 180 }).finished.then(() => {
                    moving.dataset.phase = 'finished';
                  });
                return;
              }

              root.dataset.quietAction = 'complete';
            });
          };
          install();
        })();
        """;

    private const string DeferredPortalAnimationScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            const button = root?.querySelector('button');
            if (!root || !button) { requestAnimationFrame(install); return; }

            button.addEventListener('click', () => {
              setTimeout(() => {
                const portal = document.createElement('div');
                portal.id = 'parity-deferred-portal';
                portal.textContent = 'deferred portal motion';
                document.body.appendChild(portal);
                portal.animate(
                  [{ opacity: 0 }, { opacity: 1 }],
                  { duration: 100 }).finished.then(() => {
                    root.dataset.portalAnimation = 'finished';
                  });
              }, 250);
            }, { once: true });
          };
          install();
        })();
        """;

    private const string RelocatedStartingStyleAnimationScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            const button = root?.querySelector('button');
            if (!root || !button) { requestAnimationFrame(install); return; }

            const style = document.createElement('style');
            style.textContent =
              '#parity-relocated-popup {' +
              '  width: 160px; height: 40px; opacity: 1; transform: scale(1);' +
              '  transition: opacity 120ms linear, transform 120ms linear;' +
              '}' +
              '#parity-relocated-popup[data-starting-style] {' +
              '  opacity: 0; transform: scale(0.98);' +
              '}';
            document.head.appendChild(style);

            button.addEventListener('click', () => {
              const portal = document.createElement('div');
              portal.id = 'parity-relocated-portal';
              const popup = document.createElement('div');
              popup.id = 'parity-relocated-popup';
              popup.setAttribute('role', 'dialog');
              popup.setAttribute('data-starting-style', '');
              popup.textContent = 'relocated popup motion';
              portal.appendChild(popup);

              // Match the Blazor portal lifecycle: render under the fixture root first,
              // relocate to body asynchronously, then expose positioned/visible completion.
              root.appendChild(portal);
              requestAnimationFrame(() => {
                document.body.appendChild(portal);
                portal.dataset.positioned = 'true';
                root.dataset.relocatedExpanded = 'true';

                // Completion is stable before the component roundtrip removes its starting
                // style. Registration must keep observing through that gap.
                let frames = 0;
                const releaseStartingStyle = () => {
                  if (++frames < 5) {
                    requestAnimationFrame(releaseStartingStyle);
                    return;
                  }
                  popup.removeAttribute('data-starting-style');
                };
                requestAnimationFrame(releaseStartingStyle);
              });
            }, { once: true });
          };
          install();
        })();
        """;

    private readonly string screenshots = Path.Combine(
        Path.GetTempPath(), "blazix-parity-capture", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(screenshots))
        {
            Directory.Delete(screenshots, recursive: true);
        }
    }

    [Fact]
    public async Task CapturesEveryStepOfSwitchHeroOnTheServerLeg()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "switch/hero");

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        bundle.CaptureSchemaVersion.ShouldBe(CaptureSchema.CurrentVersion);
        bundle.Steps.Count.ShouldBe(2);
        bundle.Steps[0].Actions.ShouldBeEmpty();
        bundle.Steps[0].Styles.ShouldNotBeEmpty();
        bundle.Steps[0].Aria.ShouldContain("switch");

        bundle.Steps[1].NonActionableSelectors.ShouldBeEmpty();
        bundle.Steps[1].Actions.ShouldBe(
        [
            new ActionExecution
            {
                ActionIndex = 0,
                Verb = "click",
                ExpandedSelector = "[role='switch']",
                Status = ActionExecutionStatus.Completed
            }
        ], bundle.Steps[1].ActionCompletionFailures.FirstOrDefault()?.Observed);

        bundle.Steps[1].UnresolvedSelectors.ShouldBeEmpty();
        Checked(bundle.Steps[0]).ShouldBe("true");
        Checked(bundle.Steps[1]).ShouldBe("false");
    }

    [Fact]
    public async Task SwitchServerFramesMatchWasmAfterDelayedCheckedPhaseRegistration()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "switch/hero");

        await using var context = await playwright.Browser.NewContextAsync();
        var serverPage = await context.NewPageAsync();
        var wasmPage = await context.NewPageAsync();
        var capturer = new ParityCapturer(screenshots);

        var server = await capturer.CaptureAsync(
            serverPage, fixture, ParityLeg.BlazorServer, "light");
        var wasm = await capturer.CaptureAsync(
            wasmPage, fixture, ParityLeg.BlazorWasm, "light");

        var serverFrames = FrameHashes(server.Steps[1]);
        var wasmFrames = FrameHashes(wasm.Steps[1]);

        serverFrames.Keys.ShouldBe([
            "frame000.00", "frame025.00", "frame050.00", "frame075.00", "frame100.00"
        ]);
        wasmFrames.Keys.ShouldBe(serverFrames.Keys);
        serverFrames.ShouldBe(wasmFrames);
    }

    [Fact]
    public async Task PopoverWasmPreservesEveryOpenAndCloseRootAtEveryFraction()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "popover/hero");

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorWasm, "light");

        foreach (var stepName in new[] { "open", "close" })
        {
            var step = bundle.Steps.Single(item => item.Step == stepName);
            var frames = step.ScreenshotObservations
                .Where(item => item.Shot.StartsWith("frame", StringComparison.Ordinal))
                .ToList();
            frames.Count.ShouldBe(10);
            frames.ShouldAllBe(item => item.State == ScreenshotObservationState.Captured);
            foreach (var fraction in ScreenshotSet.Fractions)
            {
                var prefix = $"frame{Math.Round(fraction * 100).ToString("000", CultureInfo.InvariantCulture)}";
                frames.Where(item => item.Shot.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(item => item.RootLabel)
                    .ShouldBe(["root", "portal(1)"]);
            }
        }
    }

    [Fact]
    public async Task PopoverServerCloseFractionsMatchReactAtEveryRootWithoutCollapsingPortalScale()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "popover/hero");

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            await using var context = await playwright.Browser.NewContextAsync();
            var capturer = new ParityCapturer(screenshots);

            var reactPage = await context.NewPageAsync();
            var react = await capturer.CaptureAsync(
                reactPage, fixture, ParityLeg.React, "light");
            await reactPage.CloseAsync();

            var serverPage = await context.NewPageAsync();
            var server = await capturer.CaptureAsync(
                serverPage, fixture, ParityLeg.BlazorServer, "light");
            await serverPage.CloseAsync();

            var serverClose = server.Steps.Single(item => item.Step == "close");
            var reactClose = react.Steps.Single(item => item.Step == "close");
            var serverFrames = FrameHashes(serverClose);
            var reactFrames = FrameHashes(reactClose);

            serverFrames.Keys.ShouldBe(reactFrames.Keys, $"attempt {attempt}");
            foreach (var fraction in new[] { "frame000", "frame025", "frame050" })
            {
                var portalFrame = serverClose.ScreenshotObservations.Single(
                    item => item.Shot == $"{fraction}.01");
                var path = Path.Combine(screenshots, portalFrame.FileName!);
                var (contentWidth, imageWidth) = RenderedContentWidth(path);
                contentWidth.ShouldBeGreaterThanOrEqualTo(
                    checked((int)Math.Floor(imageWidth * 0.98) - 1),
                    $"attempt {attempt} {fraction}.01");

                serverFrames[$"{fraction}.00"].ShouldBe(
                    reactFrames[$"{fraction}.00"], $"attempt {attempt}");
                serverFrames[$"{fraction}.01"].ShouldBe(
                    reactFrames[$"{fraction}.01"],
                    $"attempt {attempt}; actual matches React " +
                    string.Join(",", reactFrames
                        .Where(item => item.Value == serverFrames[$"{fraction}.01"])
                        .Select(item => item.Key)));
            }

            var wasmPage = await context.NewPageAsync();
            _ = await capturer.CaptureAsync(
                wasmPage, fixture, ParityLeg.BlazorWasm, "light");
            await wasmPage.CloseAsync();
        }
    }

    [Fact]
    public async Task CapturesAndRecordsTheSingleEmulatedDarkTheme()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(new StepEntry { Name = "initial" }) with
        {
            Themes = ["dark"]
        };

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "dark");

        bundle.Theme.ShouldBe("dark");
        (await page.EvaluateAsync<bool>("matchMedia('(prefers-color-scheme: dark)').matches"))
            .ShouldBeTrue();
    }

    [Theory]
    [InlineData("sepia")]
    public async Task RejectsThemesThatAreNotDeclaredAndSupported(string theme)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(new StepEntry { Name = "initial" }) with
        {
            Themes = ["light", "dark"]
        };

        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            new ParityCapturer(screenshots)
                .CaptureAsync(page, fixture, ParityLeg.BlazorServer, theme));

        exception.Message.ShouldContain(fixture.Id);
    }

    [Fact]
    public async Task PerformsStepActionsAndCapturesTheirEffect()
    {
        // Built here rather than added to the manifest: the manifest is the fixture
        // corpus, and this exercises the capturer's action loop against markup that is
        // actually actionable, which switch/hero's placeholder is not.
        var fixture = new FixtureEntry
        {
            Id = "harness/capture-probe",
            Component = "harness",
            React = "internal:none",
            Blazor = "Harness/CaptureProbe",
            Steps =
            [
                new StepEntry { Name = "initial" },
                new StepEntry
                {
                    Name = "focused",
                    Do =
                    [
                        new StepAction
                        {
                            Focus = "button",
                            Complete = [FocusEquals("button")]
                        }
                    ]
                }
            ]
        };

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        bundle.Steps[0].Focus.ShouldBeNull();
        bundle.Steps[1].Focus.ShouldBe("root > button");
        bundle.Steps[1].Actions.ShouldBe(
        [
            new ActionExecution
            {
                ActionIndex = 0,
                Verb = "focus",
                ExpandedSelector = "button",
                Status = ActionExecutionStatus.Completed
            }
        ]);
        bundle.Steps[1].UnresolvedSelectors.ShouldBeEmpty();
        bundle.Steps[1].NonActionableSelectors.ShouldBeEmpty();
    }

    [Fact]
    public async Task PhotographsTheFixtureRootAndEveryPortalContainer()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(
                page,
                ProbeFixture(new StepEntry { Name = "initial" }),
                ParityLeg.BlazorServer,
                "light");

        // The probe portals one container out to <body>, so the step is photographed twice:
        // shot 00 is the fixture root and shot 01 is portal(1), the label capture.js gives
        // that same element. A run that only photographed the root would compare every
        // popup in the corpus against nothing.
        bundle.Steps[0].Screenshots.ShouldBe([
            "harness__capture-probe.light.BlazorServer.initial.00.png",
            "harness__capture-probe.light.BlazorServer.initial.01.png"
        ]);

        foreach (var name in bundle.Steps[0].Screenshots)
        {
            File.Exists(Path.Combine(screenshots, name)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task RecordsAnExplicitNotVisibleObservationForAnEmptyPortal()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(EmptyPortalScript);

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(
                page,
                ProbeFixture(new StepEntry { Name = "initial" }),
                ParityLeg.BlazorServer,
                "light");

        // The run continues past the container that could not be photographed, and the
        // shot is left out rather than listed: a name recorded for a file that was never
        // written turns a harness hiccup into a pixel finding on the next comparison, and
        // throwing would have ended the fixture over one empty div.
        bundle.Steps[0].Screenshots.ShouldBe([
            "harness__capture-probe.light.BlazorServer.initial.00.png",
            "harness__capture-probe.light.BlazorServer.initial.02.png"
        ]);
        bundle.Steps[0].ScreenshotObservations.Select(item =>
            (item.RootLabel, item.Shot, item.State)).ShouldBe([
                ("root", "00", ScreenshotObservationState.Captured),
                ("portal(1)", "01", ScreenshotObservationState.NotVisible),
                ("portal(2)", "02", ScreenshotObservationState.Captured)
            ]);

        foreach (var name in bundle.Steps[0].Screenshots)
        {
            File.Exists(Path.Combine(screenshots, name)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task PhotographsAZeroBoxPortalFromItsVisibleDescendantUnion()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(ZeroBoxPortalWithAbsoluteChildScript);

        var bundle = await new ParityCapturer(screenshots).CaptureAsync(
            page,
            ProbeFixture(new StepEntry { Name = "initial" }),
            ParityLeg.BlazorServer,
            "light");

        var portal = bundle.Steps[0].ScreenshotObservations
            .Single(item => item.RootLabel == "portal(1)");
        portal.State.ShouldBe(ScreenshotObservationState.Captured);
        portal.Shot.ShouldBe("01");
        File.Exists(Path.Combine(screenshots, portal.FileName!)).ShouldBeTrue();
    }

    [Fact]
    public async Task RecordsOffViewportAnimationFractionAsNotVisibleWithoutLosingItsShotSlot()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(OffViewportPortalAnimationScript);
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync(
            $"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");
        await page.Locator("[data-parity-root]").WaitForAsync();
        (await CaptureScript.SelectCurrentAnimationsAsync(page)).ShouldBeGreaterThan(0);

        var frames = await ScreenshotSet.CaptureFramesAsync(
            page,
            screenshots,
            "harness/off-viewport-animation",
            "light",
            ParityLeg.BlazorServer,
            "moving");
        var outside = frames.Single(item => item.Shot == "frame000.01");
        outside.RootLabel.ShouldBe("portal(1)");
        outside.State.ShouldBe(ScreenshotObservationState.NotVisible);
        outside.FileName.ShouldBeNull();

        var visible = frames.Single(item => item.Shot == "frame100.01");
        visible.RootLabel.ShouldBe(outside.RootLabel);
        visible.State.ShouldBe(ScreenshotObservationState.Captured);
        File.Exists(Path.Combine(screenshots, visible.FileName!)).ShouldBeTrue();
    }

    [Fact]
    public async Task PhotographsNoFramesWhenNothingOnTheLegAnimated()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var fixture = ProbeFixture(new StepEntry { Name = "still", Settle = "animation" });
        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        // Nothing on the probe animates, so there is no frame to seek to. Five copies of
        // the settled shot would compare equal to the other leg's five real frames and
        // report parity where a whole animation is missing; an absent frame does not.
        bundle.Steps[0].Screenshots.ShouldBe([
            "harness__capture-probe.light.BlazorServer.still.00.png",
            "harness__capture-probe.light.BlazorServer.still.01.png"
        ]);
    }

    [Fact]
    public async Task PhotographsFiveSeekedFramesOfEveryRootOfAnAnimationStep()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(DriftScript);
        var page = await context.NewPageAsync();

        var fixture = ProbeFixture(new StepEntry { Name = "drifting", Settle = "animation" });
        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        var frames = bundle.Steps[0].Screenshots
            .Where(name => name.Contains(".frame", StringComparison.Ordinal))
            .ToList();

        // Five fractions across two roots, in the order the capturer took them. Both
        // numbers are zero-padded so that this — the chronological order — is also the
        // order an ordinal sort puts the names in, which is the order the comparator
        // reports the shots in and therefore the order a reader meets them in. Unpadded,
        // frame100 would be listed second, between frame0 and frame25.
        frames.ShouldBe([
            "harness__capture-probe.light.BlazorServer.drifting.frame000.00.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame000.01.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame025.00.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame025.01.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame050.00.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame050.01.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame075.00.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame075.01.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame100.00.png",
            "harness__capture-probe.light.BlazorServer.drifting.frame100.01.png"
        ]);

        // The list above is the one the capturer built, which is chronological by
        // construction and so cannot show that the names sort the same way. Sorted here
        // with the comparison the comparator uses.
        frames.Order(StringComparer.Ordinal).ShouldBe(frames);

        foreach (var name in frames)
        {
            File.Exists(Path.Combine(screenshots, name)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task LeavesTheStepAfterAnAnimationStepUnaffectedByTheSeeking()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(DriftScript);
        var page = await context.NewPageAsync();

        var fixture = ProbeFixture(
            new StepEntry { Name = "drifting", Settle = "animation" },
            new StepEntry { Name = "after" });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        // Asserted first so nothing below can hold vacuously: the frames were taken, so
        // the animation really was paused and seeked to its end.
        bundle.Steps[0].Screenshots
            .ShouldContain("harness__capture-probe.light.BlazorServer.drifting.frame100.00.png");

        var after = Drift(bundle.Steps[1]);

        // The final seek parks the animation on its last keyframe, where opacity is 1. An
        // animation left there stays there for every remaining step of the fixture, and
        // every screenshot and computed style after it is of a frozen page. A few seconds
        // into a twenty-second run, a live one is nowhere near.
        after.ShouldBeLessThan(0.5);
        after.ShouldBeGreaterThanOrEqualTo(Drift(bundle.Steps[0]));

        var paused = await page.EvaluateAsync<int>(
            "() => document.getAnimations().filter((a) => a.playState === 'paused').length");
        paused.ShouldBe(0);

        // Nothing the frame loop did reached the record. Seeking an animation to its end
        // and back is two phase transitions, which the browser reports as an animationend
        // and an animationstart; recorded, they would attribute the harness's own
        // bookkeeping to the component, on the one comparator whose entire subject is
        // animation — and asymmetrically, since the two legs animate different numbers of
        // things.
        //
        // Every step now owns a fresh recording. This quiet non-animation step therefore
        // reports no events instead of inheriting the previous animation step's timeline.
        bundle.Steps[1].Timeline.ShouldBeEmpty();
    }

    [Fact]
    public async Task TearsDownTheRecordingOfAnAnimationStepThatFollowsOneWithNothingToSeek()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(DeferredDriftScript);
        var page = await context.NewPageAsync();

        var fixture = ProbeFixture(
            new StepEntry { Name = "still", Settle = "animation" },
            new StepEntry
            {
                Name = "drifting",
                Settle = "animation",
                Do =
                [
                    new StepAction
                    {
                        Click = "button",
                        Complete = [State("#parity-drift", "attached")]
                    }
                ]
            },
            new StepEntry { Name = "after" });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        // Asserted first, both of them, so nothing below can hold vacuously. The first step
        // is an animation step that found nothing to seek — the case the whole test is about
        // — and the second one really did seek, so the frame loop that follows it really did
        // run.
        bundle.Steps[0].Screenshots.ShouldNotContain(
            name => name.Contains(".frame", StringComparison.Ordinal));
        bundle.Steps[1].Screenshots
            .ShouldContain("harness__capture-probe.light.BlazorServer.drifting.frame100.00.png");

        // The signature of the leak, and the one event that cannot be the component's: the
        // drift animation runs for twenty seconds and the fixture takes about one, so it
        // never reaches its own end. The only thing that ends it is the frame loop seeking
        // it to fraction 1, which the browser reports as an animationend — and the resume
        // that follows reports an animationstart on the way back. Both belong to the
        // harness, and both land in the NEXT step's record, because capture() has already
        // read this step's.
        //
        // They are recorded at all only when the recording was never torn down: a step that
        // finds nothing to seek must leave the resume state as it found it, or this step's
        // seek believes the teardown has already happened and skips it. Asserted on
        // animationend rather than on the two steps' timelines being equal, because a
        // genuine animationstart from the click can legitimately land in the window between
        // capture() and the seek, which is a race and not a leak.
        bundle.Steps[2].Timeline.ShouldNotContain(e => e.Kind == "animationend");
    }

    [Fact]
    public async Task KeepsItsOwnResumeOutOfTheNextAnimationStepsRecord()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(DeferredDriftScript);
        var page = await context.NewPageAsync();

        // Two consecutive animation steps is the canonical popup shape — one opens it, one
        // closes it — so this is the sequence the corpus will actually produce. The second
        // starts nothing of its own, which is what makes the assertion below exact.
        var fixture = ProbeFixture(
            new StepEntry
            {
                Name = "drifting",
                Settle = "animation",
                Do =
                [
                    new StepAction
                    {
                        Click = "button",
                        Complete = [State("#parity-drift", "attached")]
                    }
                ]
            },
            new StepEntry { Name = "quiet", Settle = "animation" });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer, "light");

        // Asserted first so nothing below can hold vacuously: the frames were taken, so the
        // frame loop really did seek the drift to its end and really did resume it.
        bundle.Steps[0].Screenshots
            .ShouldContain("harness__capture-probe.light.BlazorServer.drifting.frame100.00.png");

        // The quiet step drives nothing and the drift it inherits started in the previous
        // step, so the only thing that can start an animation inside its recording is the
        // previous step's resume putting the drift back from the end the frame loop seeked
        // it to — a phase crossing the browser reports as an animationstart. Recorded, it
        // attributes the harness's own bookkeeping to the component, on the one comparator
        // whose entire subject is animation, and asymmetrically: the two legs seek
        // different numbers of animations whenever one uses two keyframe animations where
        // the other uses one.
        bundle.Steps[1].Timeline.ShouldNotContain(e => e.Kind == "animationstart");
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task CapturesCanonicalStateAfterReplacementAnimationsAndUnmount(
        ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(ReplacementAnimationScript);
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(new StepEntry
        {
            Name = "replace",
            Settle = "animation",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete = [State("#parity-first", "attached")]
                }
            ]
        });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, leg, "light");

        var step = bundle.Steps.ShouldHaveSingleItem();
        step.Dom.Descendants().ShouldContain(node => node.Text == "terminal state");
        step.Dom.Descendants().ShouldNotContain(node => node.Text == "first phase");
        step.Dom.Descendants().ShouldNotContain(node => node.Text == "second phase");
        step.Timeline.Count(item => item.Kind == "animationstart").ShouldBe(2);
        step.Timeline.Count(item => item.Kind == "removed").ShouldBe(2);
        step.Timeline.ShouldNotContain(item => item.Kind == "animationend");
        step.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, step.Step, "frame100.00"));
        context.Pages.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task ConsecutiveAnimationStepsOwnCompleteNaturalTimelinesWithoutSeekLeakage(
        ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(ConsecutiveAnimationScript);
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(
            new StepEntry
            {
                Name = "open",
                Settle = "animation",
                Do =
                [
                    new StepAction
                    {
                        Click = "button",
                        Complete = [State("#parity-panel", "attached")]
                    }
                ]
            },
            new StepEntry
            {
                Name = "close",
                Settle = "animation",
                Do =
                [
                    new StepAction
                    {
                        Click = "button",
                        Complete = [State("#parity-panel", "detached")]
                    }
                ]
            });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, leg, "light");

        var open = bundle.Steps[0];
        var close = bundle.Steps[1];
        open.Dom.Descendants().ShouldContain(node =>
            node.Attributes.GetValueOrDefault("data-phase") == "open");
        close.Dom.Descendants().ShouldNotContain(node => node.Text == "animated panel");

        var openStart = open.Timeline.FindIndex(item =>
            item.Kind == "animationstart" && item.Attr == "parity-open");
        var openEnd = open.Timeline.FindIndex(item =>
            item.Kind == "animationend" && item.Attr == "parity-open");
        openStart.ShouldBeGreaterThanOrEqualTo(0);
        openEnd.ShouldBeGreaterThan(openStart);

        var closeStart = close.Timeline.FindIndex(item =>
            item.Kind == "animationstart" && item.Attr == "parity-close");
        var closeRemoval = close.Timeline.FindIndex(item => item.Kind == "removed");
        closeStart.ShouldBeGreaterThanOrEqualTo(0);
        closeRemoval.ShouldBeGreaterThan(closeStart);
        close.Timeline.Take(closeStart).ShouldNotContain(item => item.Kind == "animationend");

        (await page.Locator("[data-parity-root]")
                .GetAttributeAsync("data-parity-completions"))
            .ShouldBe("2");
        open.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, open.Step, "frame100.00"));
        close.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, close.Step, "frame100.00"));
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task ReplaysTheActionThatActuallyAnimatedWhenALaterActionWasQuiet(ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(EarlierActionAnimationScript);
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(new StepEntry
        {
            Name = "multi-action",
            Settle = "animation",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete = [AttributeEquals("#parity-earlier-action", "data-phase", "finished")]
                },
                new StepAction
                {
                    Click = "button",
                    Complete = [AttributeEquals("[data-parity-root]", "data-quiet-action", "complete")]
                }
            ]
        });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, leg, "light");

        var step = bundle.Steps.ShouldHaveSingleItem();
        step.Actions.Select(action => action.Status).ShouldBe([
            ActionExecutionStatus.Completed,
            ActionExecutionStatus.Completed
        ]);
        step.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, step.Step, "frame000.00"));
        step.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, step.Step, "frame100.00"));
        context.Pages.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task TracksAnAnimationInAPortalCreatedAfterTheActionDispatch(ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(DeferredPortalAnimationScript);
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(new StepEntry
        {
            Name = "deferred-portal",
            Settle = "animation",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete = [AttributeEquals(
                        "[data-parity-root]", "data-portal-animation", "finished")]
                }
            ]
        });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, leg, "light");

        var step = bundle.Steps.ShouldHaveSingleItem();
        step.Actions.ShouldHaveSingleItem().Status.ShouldBe(ActionExecutionStatus.Completed);
        step.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, step.Step, "frame000.02"));
        step.Screenshots.ShouldContain(
            ScreenshotSet.Name(fixture.Id, "light", leg, step.Step, "frame100.02"));
        context.Pages.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task TracksARelocatedStartingStyleAnimationWithoutLeakingFrames(ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(RelocatedStartingStyleAnimationScript);
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(
            new StepEntry
            {
                Name = "relocated-open",
                Settle = "animation",
                Do =
                [
                    new StepAction
                    {
                        Click = "button",
                        Complete =
                        [
                            State("[role='dialog']", "visible"),
                            AttributeEquals(
                                "[data-parity-root]",
                                "data-relocated-expanded",
                                "true")
                        ]
                    }
                ]
            },
            new StepEntry { Name = "quiet-after-relocation", Settle = "animation" });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, leg, "light");

        var openFrames = bundle.Steps[0].Screenshots
            .Where(name => name.Contains(".frame", StringComparison.Ordinal))
            .ToList();
        openFrames.Count.ShouldBe(15);
        openFrames.ShouldContain(
            ScreenshotSet.Name(
                fixture.Id, "light", leg, "relocated-open", "frame000.02"));
        openFrames.ShouldContain(
            ScreenshotSet.Name(
                fixture.Id, "light", leg, "relocated-open", "frame100.02"));
        ScreenshotSet.Fractions.Select(fraction =>
            ScreenshotSet.Name(
                fixture.Id,
                "light",
                leg,
                "relocated-open",
                $"frame{Math.Round(fraction * 100).ToString("000", CultureInfo.InvariantCulture)}.02"))
            .Select(name => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(Path.Combine(screenshots, name)))))
            .Distinct(StringComparer.Ordinal)
            .Count().ShouldBeGreaterThan(1);
        bundle.Steps[1].Screenshots.ShouldNotContain(
            name => name.Contains(".frame", StringComparison.Ordinal));
        context.Pages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PreservesCanonicalStepWhenTheDisposableFrameReplayCannotNavigate()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        await context.AddInitScriptAsync(RelocatedStartingStyleAnimationScript);
        var fixtureNavigations = 0;
        await context.RouteAsync("**/fixture/**", async route =>
        {
            if (Interlocked.Increment(ref fixtureNavigations) == 2)
            {
                await route.AbortAsync();
            }
            else
            {
                await route.ContinueAsync();
            }
        });
        var page = await context.NewPageAsync();
        var fixture = ProbeFixture(new StepEntry
        {
            Name = "relocated-open",
            Settle = "animation",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete = [State("[role='dialog']", "visible")]
                }
            ]
        });

        var bundle = await new ParityCapturer(screenshots).CaptureAsync(
            page, fixture, ParityLeg.BlazorServer, "light");

        var step = bundle.Steps.ShouldHaveSingleItem();
        step.Dom.ShouldNotBeNull();
        step.ScreenshotObservations.ShouldContain(item => item.Shot == "00");
        var failure = step.AnimationFrameCaptureFailures.ShouldHaveSingleItem();
        failure.Stage.ShouldBe("navigate");
        failure.ActionIndex.ShouldBeNull();
        failure.Detail.ShouldNotBeNullOrWhiteSpace();
        context.Pages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TargetActionSelectionPreservesPrimaryAndCleanupFailures()
    {
        var primary = await Should.ThrowAsync<InvalidOperationException>(() =>
            ParityCapturer.PreservePrimaryDuringCleanupAsync<int>(
                () => throw new InvalidOperationException("target selection probe"),
                () => throw new IOException("target selection cleanup probe")));

        primary.Message.ShouldBe("target selection probe");
        var cleanup = primary.Data["ParityAnimationTargetProbeCloseFailure"]
            .ShouldBeOfType<IOException>();
        cleanup.Message.ShouldBe("target selection cleanup probe");
    }

    private static double Drift(StepCapture step)
    {
        var node = step.Dom.Descendants().Single(n => n.Text == "drift");

        return double.Parse(step.Styles[node.Path]["opacity"], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The capture probe as a manifest entry. Built here rather than added to the manifest:
    /// the manifest is the fixture corpus, and this is a harness probe.
    /// </summary>
    private static FixtureEntry ProbeFixture(params StepEntry[] steps) => new()
    {
        Id = "harness/capture-probe",
        Component = "harness",
        React = "internal:none",
        Blazor = "Harness/CaptureProbe",
        Steps = steps
    };

    private static string Checked(StepCapture step)
        => step.Dom.Descendants().Single(node => node.Attributes.ContainsKey("aria-checked"))
            .Attributes["aria-checked"];

    private Dictionary<string, string> FrameHashes(StepCapture step)
        => step.ScreenshotObservations
            .Where(item =>
                item.State == ScreenshotObservationState.Captured &&
                item.Shot.StartsWith("frame", StringComparison.Ordinal))
            .ToDictionary(
                item => item.Shot,
                item => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    File.ReadAllBytes(Path.Combine(screenshots, item.FileName!)))),
                StringComparer.Ordinal);

    private static (int ContentWidth, int ImageWidth) RenderedContentWidth(string path)
    {
        using var bitmap = SKBitmap.Decode(path);
        bitmap.ShouldNotBeNull();

        var left = bitmap.Width;
        var right = -1;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red >= 250 && pixel.Green >= 250 && pixel.Blue >= 250)
                {
                    continue;
                }

                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
        }

        right.ShouldBeGreaterThanOrEqualTo(left);
        return (right - left + 1, bitmap.Width);
    }

    private static CompletionPredicate FocusEquals(string selector) => new()
    {
        Selector = selector,
        Focus = "equals"
    };

    private static CompletionPredicate State(string selector, string state) => new()
    {
        Selector = selector,
        State = state
    };

    private static CompletionPredicate AttributeEquals(
        string selector,
        string attribute,
        string expected) => new()
    {
        Selector = selector,
        Attribute = attribute,
        Expected = expected
    };
}

file static class TimelineEventListExtensions
{
    public static int FindIndex(
        this IReadOnlyList<TimelineEvent> events,
        Func<TimelineEvent, bool> predicate)
    {
        for (var index = 0; index < events.Count; index++)
        {
            if (predicate(events[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
