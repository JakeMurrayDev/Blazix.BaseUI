# Avatar

Avatar displays a user image with fallback content for loading and error states.

## Import

```razor
@using Blazix.BaseUI.Avatar
```

## Anatomy

```razor
<AvatarRoot>
    <AvatarImage src="/favicon.png" alt="Blazix" />
    <AvatarFallback>BX</AvatarFallback>
</AvatarRoot>
```

## Notes

- The fallback renders when the image cannot load.
- `Delay` on `AvatarFallback` can prevent flicker.
- Keep `alt` text useful when an image is present.
