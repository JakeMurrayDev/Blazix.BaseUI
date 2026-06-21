# OTP Field

`OtpFieldRoot` groups one-time password slots, normalizes the submitted value, and renders a hidden native text input for form integration.

Use one `OtpFieldInput` per slot in `Length`. The first slot receives the field label through `Id`/`FieldLabel`; later slots should receive per-character `aria-label` values.

```razor
@using Blazix.BaseUI.OtpField

<label for="verification-code">Verification code</label>
<OtpFieldRoot Id="verification-code" Length="6" Name="code">
    <OtpFieldInput aria-label="" />
    <OtpFieldInput aria-label="Character 2 of 6" />
    <OtpFieldInput aria-label="Character 3 of 6" />
    <OtpFieldInput aria-label="Character 4 of 6" />
    <OtpFieldInput aria-label="Character 5 of 6" />
    <OtpFieldInput aria-label="Character 6 of 6" />
</OtpFieldRoot>
```
