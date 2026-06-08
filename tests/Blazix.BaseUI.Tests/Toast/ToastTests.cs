using Blazix.BaseUI.Toast;
using Blazix.BaseUI.Tests.Infrastructure;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Tests.Toast;

public class ToastTests : BunitContext
{
    public ToastTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToastModule(JSInterop);
        JsInteropSetup.SetupFocusGuardNonSafari(JSInterop);
    }

    [Fact]
    public Task ExternalManagerAddsUpdatesLimitsAndClosesToasts()
    {
        var manager = new ToastManager();
        var closed = 0;
        var removed = 0;

        var cut = RenderProvider(manager, limit: 2);

        manager.Add(new ToastManagerAddOptions
        {
            Id = "one",
            Title = "One",
            Description = "First",
            Timeout = 0,
            OnClose = () => closed++,
            OnRemove = () => removed++
        });
        manager.Add(new ToastManagerAddOptions
        {
            Id = "two",
            Title = "Two",
            Description = "Second",
            Timeout = 0
        });
        manager.Add(new ToastManagerAddOptions
        {
            Id = "three",
            Title = "Three",
            Description = "Third",
            Timeout = 0
        });

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='toast-root']").Count.ShouldBe(3);
            cut.Find("[data-toast-id='one']").HasAttribute("data-limited").ShouldBeTrue();
            cut.Find("[data-toast-id='three']").HasAttribute("data-limited").ShouldBeFalse();
        });

        manager.Update("one", new ToastManagerUpdateOptions
        {
            Description = "Updated",
            Type = "success"
        });

        cut.WaitForAssertion(() =>
        {
            var toast = cut.Find("[data-toast-id='one']");
            toast.GetAttribute("data-type").ShouldBe("success");
            toast.TextContent.ShouldContain("Updated");
        });

        manager.Close("one");

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-toast-id='one']").HasAttribute("data-ending-style").ShouldBeTrue();
            closed.ShouldBe(1);
        });

        cut.Find("[data-toast-id='one']").KeyDown("Escape");

        closed.ShouldBe(1);
        removed.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DuplicateIdUpsertsExistingToastAndRefreshesUpdateKey()
    {
        var manager = new ToastManager();
        var cut = RenderProvider(manager);

        var firstId = manager.Add(new ToastManagerAddOptions
        {
            Id = "stable",
            Description = "Initial",
            Timeout = 0
        });
        var secondId = manager.Add(new ToastManagerAddOptions
        {
            Id = "stable",
            Description = "Updated",
            Type = "info",
            Timeout = 0
        });

        firstId.ShouldBe("stable");
        secondId.ShouldBe("stable");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-toast-id='stable']").Count.ShouldBe(1);
            cut.Find("[data-toast-id='stable']").TextContent.ShouldContain("Updated");
            cut.Find("[data-toast-id='stable']").GetAttribute("data-update-key").ShouldBe("1");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task PromiseToastTransitionsFromLoadingToSuccessAndError()
    {
        var manager = new ToastManager();
        var cut = RenderProvider(manager);

        var successSource = new TaskCompletionSource<int>();
        var successTask = manager.Promise(successSource.Task, new ToastManagerPromiseOptions<int>
        {
            Loading = "Loading",
            Success = ToastPromiseOption<int>.From(value => new ToastManagerUpdateOptions
            {
                Description = $"Saved {value}",
                Type = "success",
                Timeout = 0
            }),
            Error = "Failed"
        });

        cut.WaitForAssertion(() =>
        {
            var toast = cut.Find("[data-type='loading']");
            toast.TextContent.ShouldContain("Loading");
        });

        successSource.SetResult(42);
        (await successTask).ShouldBe(42);

        cut.WaitForAssertion(() =>
        {
            var toast = cut.Find("[data-type='success']");
            toast.TextContent.ShouldContain("Saved 42");
        });

        var errorSource = new TaskCompletionSource<int>();
        var errorTask = manager.Promise(errorSource.Task, new ToastManagerPromiseOptions<int>
        {
            Loading = "Loading error",
            Success = "Saved",
            Error = ToastPromiseOption<Exception>.From(error => new ToastManagerUpdateOptions
            {
                Description = $"Failed {error.Message}",
                Type = "error",
                Timeout = 0
            })
        });

        errorSource.SetException(new InvalidOperationException("boom"));
        await Should.ThrowAsync<InvalidOperationException>(errorTask);

        cut.WaitForAssertion(() =>
        {
            var toast = cut.Find("[data-type='error']");
            toast.TextContent.ShouldContain("Failed boom");
        });
    }

    [Fact]
    public Task TitleAndDescriptionRegistrationsAreClearedWhenPartsUnmount()
    {
        var manager = new ToastManager();
        var renderParts = true;
        var cut = RenderProvider(manager, shouldRenderLabelParts: () => renderParts);

        manager.Add(new ToastManagerAddOptions
        {
            Id = "mutable-parts",
            Title = "Mutable title",
            Description = "Mutable description",
            Timeout = 0
        });

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find("[data-toast-id='mutable-parts']");
            root.GetAttribute("aria-labelledby").ShouldBe("title-mutable-parts");
            root.GetAttribute("aria-describedby").ShouldBe("description-mutable-parts");
        });

        renderParts = false;
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find("[data-toast-id='mutable-parts']");
            root.HasAttribute("aria-labelledby").ShouldBeFalse();
            root.HasAttribute("aria-describedby").ShouldBeFalse();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task CanceledSwipeClearsHoverExpansion()
    {
        var manager = new ToastManager();
        var cut = RenderProvider(manager);

        manager.Add(new ToastManagerAddOptions
        {
            Id = "swipe-cancel",
            Description = "Swipe cancel",
            Timeout = 0
        });

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-toast-id='swipe-cancel']").ShouldNotBeNull();
        });

        var rootComponent = cut.FindComponent<ToastRoot>();
        rootComponent.Instance.OnSwipeStateChanged(true, "right");

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='toast-viewport']").HasAttribute("data-expanded").ShouldBeTrue();
        });

        rootComponent.Instance.OnSwipeStateChanged(false, null);

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='toast-viewport']").HasAttribute("data-expanded").ShouldBeFalse();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ManagerSubscribeAndEmitCanRunConcurrently()
    {
        var manager = new ToastManager();
        var errors = new ConcurrentBag<Exception>();
        var observed = 0;

        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for (var index = 0; index < 2_000; index++)
            {
                try
                {
                    if (worker % 2 == 0)
                    {
                        manager.Add(new ToastManagerAddOptions
                        {
                            Description = $"Toast {worker}-{index}",
                            Timeout = 0
                        });
                    }
                    else
                    {
                        var unsubscribe = manager.Subscribe(_ => Interlocked.Increment(ref observed));
                        manager.Update("missing", new ToastManagerUpdateOptions
                        {
                            Description = "No-op"
                        });
                        unsubscribe();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }
        }));

        await Task.WhenAll(tasks);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public Task ViewportAndPartsRenderReactParityAttributes()
    {
        var manager = new ToastManager();
        var cut = RenderProvider(manager);

        manager.Add(new ToastManagerAddOptions
        {
            Id = "high",
            Title = "System",
            Description = "High priority",
            Priority = ToastPriority.High,
            Type = "warning",
            Timeout = 0,
            ActionProps = new ToastActionOptions
            {
                ChildContent = builder => builder.AddContent(0, "Undo"),
                AdditionalAttributes = new Dictionary<string, object>
                {
                    ["aria-label"] = "Undo notification"
                }
            }
        });

        cut.WaitForAssertion(() =>
        {
            var viewport = cut.Find("[data-testid='toast-viewport']");
            viewport.GetAttribute("role").ShouldBe("region");
            viewport.GetAttribute("aria-live").ShouldBe("polite");
            viewport.GetAttribute("aria-atomic").ShouldBe("false");
            viewport.GetAttribute("aria-relevant").ShouldBe("additions text");
            viewport.GetAttribute("aria-label").ShouldBe("Notifications");
            viewport.GetAttribute("tabindex").ShouldBe("-1");
            viewport.GetAttribute("style").ShouldContain("--toast-frontmost-height");

            var root = cut.Find("[data-toast-id='high']");
            root.GetAttribute("role").ShouldBe("alertdialog");
            root.GetAttribute("aria-modal").ShouldBe("false");
            root.GetAttribute("aria-hidden").ShouldBe("true");
            root.GetAttribute("data-type").ShouldBe("warning");
            root.GetAttribute("tabindex").ShouldBe("0");
            root.GetAttribute("style").ShouldContain("--toast-index");
            root.GetAttribute("style").ShouldContain("--toast-offset-y");
            root.GetAttribute("style").ShouldContain("--toast-swipe-movement-x");
            root.GetAttribute("style").ShouldContain("--toast-swipe-movement-y");

            cut.Find("[data-testid='toast-content']").HasAttribute("data-expanded").ShouldBeFalse();
            cut.Find("h2").GetAttribute("data-type").ShouldBe("warning");
            cut.Find("p").GetAttribute("data-type").ShouldBe("warning");

            var close = cut.Find("[data-testid='toast-close']");
            close.GetAttribute("type").ShouldBe("button");
            close.GetAttribute("aria-hidden").ShouldBe("true");
            close.GetAttribute("data-type").ShouldBe("warning");

            var action = cut.Find("[data-testid='toast-action']");
            action.GetAttribute("type").ShouldBe("button");
            action.GetAttribute("aria-label").ShouldBe("Undo notification");
            action.GetAttribute("data-type").ShouldBe("warning");
            action.TextContent.ShouldBe("Undo");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseButtonClosesToastAndInvokesUserClick()
    {
        var manager = new ToastManager();
        var clicked = false;
        var cut = RenderProvider(manager, closeAdditionalAttributes: new Dictionary<string, object>
        {
            ["data-testid"] = "toast-close",
            ["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true)
        });

        manager.Add(new ToastManagerAddOptions
        {
            Id = "closable",
            Description = "Closable",
            Timeout = 0
        });

        cut.WaitForAssertion(() => cut.Find("[data-toast-id='closable']").ShouldNotBeNull());

        cut.Find("[data-testid='toast-close']").Click();

        cut.WaitForAssertion(() =>
        {
            clicked.ShouldBeTrue();
            cut.Find("[data-toast-id='closable']").HasAttribute("data-ending-style").ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task PositionerAndArrowRenderAnchoredAttributes()
    {
        var manager = new ToastManager();
        var cut = RenderProvider(manager, renderPositioner: true);

        manager.Add(new ToastManagerAddOptions
        {
            Id = "anchored",
            Description = "Anchored",
            Timeout = 0,
            PositionerProps = new ToastPositionerOptions
            {
                Side = Side.Bottom,
                Align = Align.End,
                SideOffset = 8,
                AlignOffset = 4,
                PositionMethod = PositionMethod.Fixed
            }
        });

        cut.WaitForAssertion(() =>
        {
            var positioner = cut.Find("[data-testid='toast-positioner']");
            positioner.GetAttribute("role").ShouldBe("presentation");
            positioner.GetAttribute("data-side").ShouldBe("bottom");
            positioner.GetAttribute("data-align").ShouldBe("end");
            positioner.HasAttribute("data-anchor-hidden").ShouldBeFalse();
            positioner.GetAttribute("style").ShouldContain("--toast-index");

            var arrow = cut.Find("[data-testid='toast-arrow']");
            arrow.GetAttribute("aria-hidden").ShouldBe("true");
            arrow.GetAttribute("data-side").ShouldBe("bottom");
            arrow.GetAttribute("data-align").ShouldBe("end");
            arrow.HasAttribute("data-uncentered").ShouldBeFalse();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeDisabledActionSuppressesActionCallback()
    {
        var manager = new ToastManager();
        var clicked = 0;
        var cut = RenderProvider(manager);

        manager.Add(new ToastManagerAddOptions
        {
            Id = "disabled-action",
            Description = "Disabled action",
            Timeout = 0,
            ActionProps = new ToastActionOptions
            {
                Disabled = true,
                NativeButton = false,
                ChildContent = builder => builder.AddContent(0, "Action"),
                OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)
            }
        });

        cut.WaitForAssertion(() =>
        {
            var action = cut.Find("[data-testid='toast-action']");
            action.GetAttribute("role").ShouldBe("button");
            action.GetAttribute("aria-disabled").ShouldBe("true");
        });

        cut.Find("[data-testid='toast-action']").Click();

        clicked.ShouldBe(0);

        return Task.CompletedTask;
    }

    private IRenderedComponent<ToastProvider> RenderProvider(
        ToastManager manager,
        int limit = 3,
        bool renderPositioner = false,
        IReadOnlyDictionary<string, object>? closeAdditionalAttributes = null,
        Func<bool>? shouldRenderLabelParts = null)
    {
        return Render<ToastProvider>(parameters => parameters
            .Add(p => p.Timeout, 0)
            .Add(p => p.Limit, limit)
            .Add(p => p.ToastManager, manager)
            .Add(p => p.ChildContent, context => builder =>
            {
                builder.OpenComponent<ToastViewport>(0);
                builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                {
                    ["data-testid"] = "toast-viewport"
                });
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(viewportBuilder =>
                {
                    var sequence = 0;
                    foreach (var toast in context.Toasts)
                    {
                        if (renderPositioner)
                        {
                            viewportBuilder.OpenComponent<ToastPositioner>(sequence++);
                            viewportBuilder.AddAttribute(sequence++, "Toast", toast);
                            viewportBuilder.AddAttribute(sequence++, "AdditionalAttributes", new Dictionary<string, object>
                            {
                                ["data-testid"] = "toast-positioner"
                            });
                            viewportBuilder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(positionerBuilder =>
                            {
                                RenderToast(positionerBuilder, toast, closeAdditionalAttributes, includeArrow: true);
                            }));
                            viewportBuilder.CloseComponent();
                        }
                        else
                        {
                            RenderToast(
                                viewportBuilder,
                                toast,
                                closeAdditionalAttributes,
                                includeArrow: false,
                                shouldRenderLabelParts);
                        }
                    }
                }));
                builder.CloseComponent();
            }));
    }

    private static void RenderToast(
        RenderTreeBuilder builder,
        ToastObject toast,
        IReadOnlyDictionary<string, object>? closeAdditionalAttributes,
        bool includeArrow,
        Func<bool>? shouldRenderLabelParts = null)
    {
        var sequence = 0;

        builder.OpenComponent<ToastRoot>(sequence++);
        builder.AddAttribute(sequence++, "Toast", toast);
        builder.AddAttribute(sequence++, "AdditionalAttributes", new Dictionary<string, object>
        {
            ["data-testid"] = "toast-root",
            ["data-toast-id"] = toast.Id,
            ["data-update-key"] = toast.UpdateKey.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(rootBuilder =>
        {
            rootBuilder.OpenComponent<ToastContent>(0);
            rootBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                ["data-testid"] = "toast-content"
            });
            rootBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(contentBuilder =>
            {
                if (shouldRenderLabelParts?.Invoke() ?? true)
                {
                    contentBuilder.OpenComponent<ToastTitle>(0);
                    contentBuilder.AddAttribute(1, "Id", $"title-{toast.Id}");
                    contentBuilder.CloseComponent();

                    contentBuilder.OpenComponent<ToastDescription>(10);
                    contentBuilder.AddAttribute(11, "Id", $"description-{toast.Id}");
                    contentBuilder.CloseComponent();
                }

                contentBuilder.OpenComponent<ToastAction>(20);
                contentBuilder.AddAttribute(21, "AdditionalAttributes", new Dictionary<string, object>
                {
                    ["data-testid"] = "toast-action"
                });
                contentBuilder.CloseComponent();

                contentBuilder.OpenComponent<ToastClose>(30);
                contentBuilder.AddAttribute(31, "AdditionalAttributes", closeAdditionalAttributes ?? new Dictionary<string, object>
                {
                    ["data-testid"] = "toast-close"
                });
                contentBuilder.CloseComponent();

                if (includeArrow)
                {
                    contentBuilder.OpenComponent<ToastArrow>(40);
                    contentBuilder.AddAttribute(41, "AdditionalAttributes", new Dictionary<string, object>
                    {
                        ["data-testid"] = "toast-arrow"
                    });
                    contentBuilder.CloseComponent();
                }
            }));
            rootBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
