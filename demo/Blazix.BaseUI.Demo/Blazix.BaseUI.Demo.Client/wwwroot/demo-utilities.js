export function clickElement(element) {
    if (element) {
        element.click();
    }
}

export function getFormValue(formId, name) {
    const form = document.getElementById(formId);
    if (!form) {
        return null;
    }

    const value = new FormData(form).get(name);
    return value === null ? null : String(value);
}
