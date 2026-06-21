# OTP Field Source Docs Comparison

Date: 2026-06-21

## React Documentation Inputs

Compared against:

- `.base-ui/docs/src/app/(docs)/react/components/otp-field/page.mdx`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/types.md`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/demos/hero/*`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/demos/alphanumeric/*`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/demos/grouped/*`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/demos/focused-placeholder/*`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/demos/custom-sanitize/*`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/demos/password/*`

## Blazor Documentation Additions

- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/otp-field.md`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/OtpFieldPage.razor`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/OtpFieldApi.cs`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/OtpField/Hero/Css/OtpFieldHeroCss.razor`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/OtpField/Hero/Tailwind/OtpFieldHeroTailwind.razor`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/otp-field.css`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/otp-field.min.css`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DocsNav.cs`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/_Imports.razor`

## Demo Additions

- `demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo.Client/Shared/Sections/OtpFieldSection.razor`
- `demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo/Components/Pages/OtpFieldPage.razor`
- `demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo.Client/Components/Pages/OtpFieldPage.razor`
- `demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo/Components/Layout/NavMenu.razor`

## Documentation Parity

| React docs topic | Blazor docs/demo status |
| --- | --- |
| Component summary: one-time password input composed of slots. | Covered in docs content and docs page description. |
| Accessible name requirement. | Covered in docs content and examples using labels. |
| Anatomy: `Root`, `Input`, shared `Separator`. | Covered through Blazor examples and API metadata. |
| Native label plus description. | Covered in docs content and test coverage. |
| Field/Form integration. | Covered in docs content; verified through bUnit and Playwright form tests. |
| `autoSubmit` completion behavior. | Covered in API docs and Playwright form test. |
| Alphanumeric verification codes. | Covered by docs hero/API and demo controlled alphanumeric section. |
| Grouped layouts and Separator. | Covered by demo controlled layout with a shared Separator. |
| Placeholder hints. | Accounted for as normal input unmatched attributes; supported through `AdditionalAttributes`. |
| Custom normalization. | Covered by API docs and normalization tests. |
| Masked entry. | Covered by docs/demo masked section and bUnit type override test. |
| Root props table. | Added to `OtpFieldApi.cs`. |
| Root state table. | Added to `OtpFieldApi.cs`. |
| Root data attributes table. | Added to `OtpFieldApi.cs`. |
| Input props/state/data attributes. | Added to `OtpFieldApi.cs`. |
| Validation type/event reason docs. | Added through API metadata and audit matrix. |

## Source Docs Verification

The source docs were inspected from the checked-out `.base-ui` workspace and validated with PNPM commands recorded in `docs/audits/otp-field-verification-report.md`.
