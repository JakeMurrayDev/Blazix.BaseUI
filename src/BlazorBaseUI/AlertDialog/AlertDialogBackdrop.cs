namespace BlazorBaseUI.AlertDialog;

using BlazorBaseUI.Dialog;

/// <summary>
/// An overlay displayed beneath the alert dialog popup.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class AlertDialogBackdrop : DialogBackdrop;
