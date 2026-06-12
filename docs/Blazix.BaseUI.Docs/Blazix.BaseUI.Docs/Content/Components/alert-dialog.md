# Alert Dialog

Alert Dialog presents a modal decision that the user must resolve explicitly. It uses dialog primitives with alert-dialog role behavior and disables pointer dismissal.

## Import

```razor
@using Blazix.BaseUI.AlertDialog
```

## Anatomy

```razor
<AlertDialogRoot>
    <AlertDialogTrigger>Delete file</AlertDialogTrigger>
    <AlertDialogPortal>
        <AlertDialogBackdrop />
        <AlertDialogPopup>
            <AlertDialogTitle>Delete file?</AlertDialogTitle>
            <AlertDialogDescription>This action cannot be undone.</AlertDialogDescription>
            <AlertDialogClose>Cancel</AlertDialogClose>
        </AlertDialogPopup>
    </AlertDialogPortal>
</AlertDialogRoot>
```

## Notes

- Include a clear title and description.
- Provide at least one explicit close action.
- Focus is trapped while the modal is open.
