namespace Blazix.BaseUI.Tests.Contracts.FloatingFocusManager;

public interface IFloatingFocusManagerContract
{
    Task RendersChildContent();
    Task DoesNotRenderHtmlElement();
    Task CreatesManagerOnFirstRender();
    Task PassesModalOption();
    Task PassesInitialFocusOption();
    Task PassesInitialFocusSelectorOption();
    Task PassesReturnFocusOption();
    Task PassesRestoreFocusOptions();
    Task PassesCloseOnFocusOutOption();
    Task PassesInteractionType();
    Task DoesNothingWhenDisabled();
    Task DisposesManagerOnDispose();
    Task CallsUpdateWhenModalChangesWhileOpen();
    Task CallsUpdateWhenReturnFocusChangesWhileOpen();
    Task CallsUpdateWhenCloseOnFocusOutChangesWhileOpen();
    Task CallsUpdateWhenInsideElementsChangeWhileOpen();
    Task DoesNotCallUpdateWhenParametersUnchanged();
    Task PassesOrderOption();
    Task PassesInsideElementsOption();
    Task PassesNextFocusableElementOption();
    Task PassesPreviousFocusableElementOption();
    Task RendersFocusGuardsWhenModal();
    Task DoesNotRenderFocusGuardsWhenDisabled();
    Task ExposesBeforeContentFocusGuardElement();
    Task AcceptsGetInsideElementsParameter();
    Task AcceptsExternalTreeParameter();
    Task DefaultsOrderToContent();
    Task FocusGuardsHaveDataTypeAttribute();
    Task DoesNotRenderFocusGuardsWhenNonModalWithoutPortal();
    Task CallsHandleFocusGuardFocusWithBeforeDirection();
    Task CallsHandleFocusGuardFocusWithAfterDirection();
    Task CallsUpdateWhenOrderChangesWhileOpen();
    Task DisposesManagerWhenDisabledWhileOpen();
    Task CreatesManagerWhenEnabledWhileOpen();
    Task PassesModalArgumentToJs();
    Task PassesCloseInteractionTypeToReturnFocusCallback();
}
