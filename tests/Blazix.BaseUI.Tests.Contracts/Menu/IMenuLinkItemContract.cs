namespace Blazix.BaseUI.Tests.Contracts.Menu;

/// <summary>
/// Defines the test contract for <see cref="Blazix.BaseUI.Menu.MenuLinkItem"/>.
/// </summary>
public interface IMenuLinkItemContract
{
    // Rendering
    Task RendersAsAnchorElement();
    Task RendersHrefViaAdditionalAttributes();
    Task RendersRoleMenuitem();
    Task RendersChildContent();

    // Data attributes
    Task DataHighlightedFalseByDefault();
    Task RendersLabelAsDataAttribute();
    Task RendersIdAttribute();

    // Defaults
    Task CloseOnClickDefaultsFalse();

    // Behavior
    Task HighlightsOnMouseEnter();
    Task UnhighlightsOnMouseLeave();
    Task CloseOnClickTrueClosesMenu();

    // Rendering
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();

    // ClassValue / StyleValue
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task ClassValueReceivesState();
}
