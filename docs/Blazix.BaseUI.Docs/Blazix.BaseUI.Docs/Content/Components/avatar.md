# Avatar

An image with a textual fallback.

Rendered docs: `/components/avatar`

The avatar shows an image once it loads, and falls back to its child content (initials or an icon) when the image is missing or fails to load.

## Anatomy

```razor
@using Blazix.BaseUI.Avatar

<AvatarRoot>
    <AvatarImage src="" alt="" />
    <AvatarFallback>LT</AvatarFallback>
</AvatarRoot>
```

## API reference

### Root

Displays a user's profile picture, initials, or fallback icon. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AvatarRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AvatarRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AvatarRootState, string?>?` | `null` | Returns a CSS style based on state. |

### Image

The image to be displayed in the avatar. Standard `<img>` attributes such as `src` and `alt` are forwarded to the element. Renders an `<img>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `OnLoadingStatusChange` | `EventCallback<ImageLoadingStatus>` | — | Callback fired when the image loading status changes (`Idle`, `Loading`, `Loaded`, `Error`). |
| `Render` | `RenderFragment<RenderProps<AvatarRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AvatarRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AvatarRootState, string?>?` | `null` | Returns a CSS style based on state. |

### Fallback

Rendered when the image fails to load or when no image is provided. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Delay` | `int?` | `null` | How long to wait before showing the fallback, in milliseconds. Prevents a flash of fallback content while the image loads. |
| `Render` | `RenderFragment<RenderProps<AvatarRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AvatarRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AvatarRootState, string?>?` | `null` | Returns a CSS style based on state. |
