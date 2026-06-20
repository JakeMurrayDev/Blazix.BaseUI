# Toast

Toast displays brief notifications with stacking, actions, promises, custom content, and optional anchored positioning.

## Anatomy

```razor
@using Blazix.BaseUI.Toast

<ToastProvider ToastManager="@toastManager" Context="toasts">
    <ToastPortal>
        <ToastViewport>
            @foreach (var toast in toasts.Toasts)
            {
                <ToastRoot Toast="toast">
                    <ToastTitle>@toast.Title</ToastTitle>
                    <ToastDescription>@toast.Description</ToastDescription>
                    <ToastClose />
                </ToastRoot>
            }
        </ToastViewport>
    </ToastPortal>
</ToastProvider>

@code {
    private readonly ToastManager toastManager = new();
}
```

## Examples

### Anchored toasts

Use `ToastPositioner` to place a toast relative to an element.

```razor
<button @ref="anchor" @onclick="ShowAnchoredToast">Notify near me</button>

<ToastProvider ToastManager="@toastManager" Context="toasts">
    <ToastPortal>
        @foreach (var toast in toasts.Toasts)
        {
            <ToastPositioner Toast="toast" Anchor="anchor" Side="Side.Top" SideOffset="8">
                <ToastRoot Toast="toast">
                    <ToastTitle>@toast.Title</ToastTitle>
                    <ToastDescription>@toast.Description</ToastDescription>
                    <ToastArrow />
                </ToastRoot>
            </ToastPositioner>
        }
    </ToastPortal>
</ToastProvider>
```

### Custom position

Place `ToastViewport` wherever notifications should appear.

```razor
<ToastPortal>
    <ToastViewport class="notifications-top-center">
        @foreach (var toast in toasts.Toasts)
        {
            <ToastRoot Toast="toast">
                <ToastTitle>@toast.Title</ToastTitle>
            </ToastRoot>
        }
    </ToastViewport>
</ToastPortal>
```

### Undo action

Add `ActionProps` when the toast can reverse an operation.

```razor
string? toastId = null;
toastId = toastManager.Add(new ToastManagerAddOptions
{
    Title = "Action performed",
    Description = "You can undo this action.",
    Type = "success",
    ActionProps = new ToastActionOptions
    {
        ChildContent = @<span>Undo</span>,
        OnClick = EventCallback.Factory.Create<MouseEventArgs>(
            this,
            () => toastManager.Close(toastId))
    }
});
```

### Promise

Use the manager promise API to show loading, success, and error states for one task.

```razor
toastManager.Promise(
    SaveAsync(),
    new ToastManagerPromiseOptions<string>
    {
        Loading = new ToastManagerUpdateOptions
        {
            Title = "Saving...",
            HasTitle = true
        },
        Success = ToastPromiseOption<string>.From(result => new ToastManagerUpdateOptions
        {
            Title = "Saved",
            Description = result,
            HasTitle = true,
            HasDescription = true
        }),
        Error = ToastPromiseOption<Exception>.From(error => new ToastManagerUpdateOptions
        {
            Title = "Could not save",
            Description = error.Message,
            HasTitle = true,
            HasDescription = true
        })
    });
```

### Custom

Wrap custom layouts in `ToastContent` while keeping root lifecycle and accessibility behavior.

```razor
<ToastRoot Toast="toast">
    <ToastContent>
        <strong>@toast.Title</strong>
        <span>@toast.Description</span>
        <ToastClose>Dismiss</ToastClose>
    </ToastContent>
</ToastRoot>
```

### Deduplicated toast

Use a stable id or update key when repeat events should update an existing toast instead of stacking duplicates.

```razor
toastManager.Add(new ToastManagerAddOptions
{
    Id = "save-status",
    Title = "Draft saved",
    Description = "Click again while it is visible to replay the pulse."
});

toastManager.Update("save-status", new ToastManagerUpdateOptions
{
    Title = "Draft saved again",
    HasTitle = true,
    Description = "The existing toast is updated instead of duplicated.",
    HasDescription = true
});
```

### Varying heights

Toast roots measure their height and expose stack variables so rows with different content heights still animate correctly.

## Parts

Parts: `ToastProvider`, `ToastPortal`, `ToastViewport`, `ToastRoot`, `ToastContent`, `ToastTitle`, `ToastDescription`, `ToastAction`, `ToastClose`, `ToastPositioner`, `ToastArrow`, and `ToastManager`.

Toast exposes stack variables such as `--toast-index`, `--toast-offset-y`, `--toast-height`, `--toast-swipe-movement-x`, and `--toast-swipe-movement-y`. Anchored toasts also expose floating variables such as `--anchor-width`, `--available-height`, and `--transform-origin`.
