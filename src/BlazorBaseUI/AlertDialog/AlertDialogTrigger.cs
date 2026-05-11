namespace BlazorBaseUI.AlertDialog;

using BlazorBaseUI.Dialog;

/// <summary>
/// A button that opens the alert dialog.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class AlertDialogTrigger : DialogTypedTrigger<object?>;
