# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). While the version is below
`1.0.0`, breaking changes may land in any release.

## [0.1.0-preview.1] — Unreleased

First packaged release. Blazix.BaseUI has been developed in-repo up to this point; this is the
first version intended for publication to NuGet. Replace `Unreleased` with the release date once
the `v0.1.0-preview.1` tag has published successfully.

### Added

- NuGet package metadata, MIT license expression, and a consumer-facing README
  ([#216](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/216)).
- XML documentation is now generated and shipped, so component parameters surface in IntelliSense
  ([#216](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/216)).
- SourceLink, embedded untracked sources, and a `.snupkg` symbol package for debugging into the
  library ([#216](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/216)).
- Test page coverage for viewport hover retargeting
  ([#200](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/200)) and Shift+Tab focus restoration
  in the toast viewport ([#212](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/212)).

### Changed

- **Breaking:** `MenuSubmenuRoot` no longer exposes `Handle`, `TriggerId`, or `DefaultTriggerId`,
  and `ContextMenuRoot` no longer exposes `Handle`, `TriggerId`, `DefaultTriggerId`, or
  `PayloadChildContent`, aligning both surfaces with upstream Base UI
  ([#203](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/203)).

  These parameters were inert — both components forwarded them to the inner `MenuRoot`, where
  `ParentType.Menu` resolution suppressed the handle logic — so no behavior is lost. **Migration:**
  delete the attribute. Detached triggers remain fully supported on `MenuRoot` and the other popup
  roots. This landed deliberately before the first publish; after `1.0.0` it would be a major bump.

- **Breaking:** `LabelableContext.SetLabelId` now takes the registering component instance
  alongside the id (`Action<object, string?>` instead of `Action<string?>`)
  ([#225](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/225)).

  Blazor initializes a replacement component before disposing the outgoing one, so a label that
  cleared its registration on dispose could drop its replacement's registration and leave the
  control without an accessible name. Ownership is tracked by instance, which an id-equality check
  cannot do when both instances resolve the same id. **Migration:** only code that constructs a
  `LabelableContext` directly is affected — pass `(_, id) => ...` where it passed `id => ...`. This
  landed deliberately before the first publish; after `1.0.0` it would be a major bump.

### Fixed

Cycle-1 upstream parity sweep — behavioral gaps against upstream Base UI, by component family:

- **Progress** — status, `aria-valuenow`, and value text now derive from the clamped value
  ([#201](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/201)).
- **ScrollArea** — drags end when the pointer is lost, and scroll snapping is suppressed mid-drag
  ([#202](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/202)).
- **Toast** — paused timers no longer accumulate, and recycled roots reset correctly
  ([#204](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/204)).
- **Slider** — NaN-safe minimum distance and a thumb-to-thumb blur guard
  ([#205](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/205)).
- **Field / Fieldset / Form** — document-order focus, null-value dirty state, and revalidation
  ([#206](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/206)).
- **Shared floating layer** — scroll-lock handoff, pinch-zoom, viewport origin, auto-resize, and
  canceled exit transitions ([#207](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/207)).
- **Select** — items inherit the root disabled state
  ([#208](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/208)).
- **Menu family** — root disabled propagation and VoiceOver submenu announcement
  ([#209](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/209)).
- **Combobox / Autocomplete** — six named upstream changes ported
  ([#211](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/211)).
- **All components with registered ids** — replacing a label, a toast title or description, an
  accordion trigger, or a collapsible panel no longer drops the accessible name or leaves
  `aria-controls` pointing at a removed element
  ([#225](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/225)).
- **Menu** — a submenu trigger now opens on an Android TalkBack press instead of waiting for hover
  that a screen reader never produces
  ([#225](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/225)).
- **Select / Combobox / Autocomplete / Menu** — in Safari, scrolling a list under a stationary
  pointer no longer drags the highlight onto whichever item slides under the cursor; a read-only
  Select no longer highlights on hover
  ([#225](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/225)).
- **Shared floating layer / Select** — a popup no longer returns focus with a visible focus ring
  left over from a previous open session
  ([#225](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/225)).

### Known issues

- Six Playwright end-to-end tests fail deterministically in both render modes (five ScrollArea, one
  Toast), tracked in [#214](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/214). Those
  behaviors ship unvalidated at browser level in this preview.
