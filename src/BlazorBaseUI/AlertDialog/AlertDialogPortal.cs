namespace BlazorBaseUI.AlertDialog;

using BlazorBaseUI.Dialog;

/// <summary>
/// A portal element that moves the alert dialog popup to a different part of the DOM.
/// By default, the portal element is appended to <c>&lt;body&gt;</c>.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class AlertDialogPortal : DialogPortal;
