# CSP Provider

Configures Content Security Policy behavior for the inline style elements Blazix.BaseUI components inject at runtime.

Rendered docs: `/utils/csp-provider`

Some components inject an inline `<style>` element for functionality such as hiding native scrollbars. In Blazix.BaseUI this affects the `Select` popup and list. Under a strict Content Security Policy these elements are blocked unless they carry a matching `nonce`.

`CspProvider` configures this behavior for every Blazix.BaseUI component in its subtree.

## Anatomy

Wrap it around your app or a group of components:

```razor
@using Blazix.BaseUI.Csp

<CspProvider Nonce="@nonce">
    @* Your app or a group of components *@
</CspProvider>
```

## Supplying a nonce

Generate a random nonce per request, include it in your CSP header (`style-src-elem`), and pass the same value to `CspProvider`. Injected `<style>` elements then carry the nonce and are allowed.

## Disable inline style elements

If you would rather not supply a nonce, set `DisableStyleElements` to skip injecting the style elements entirely and rely on your own CSS instead.

```razor
<CspProvider DisableStyleElements="true">
    @* ... *@
</CspProvider>
```
