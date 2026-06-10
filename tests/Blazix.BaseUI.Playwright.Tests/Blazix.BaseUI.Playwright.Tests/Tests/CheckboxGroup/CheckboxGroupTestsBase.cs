using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.CheckboxGroup;

/// <summary>
/// Playwright tests for CheckboxGroup component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: group value management, checkbox interactions within group,
/// parent checkbox behavior, disabled propagation, and form integration.
/// </summary>
public abstract class CheckboxGroupTestsBase : TestBase
{
    protected CheckboxGroupTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetCheckboxGroup() => GetByTestId("checkbox-group");

    protected ILocator GetCheckbox(string color) => GetByTestId($"checkbox-{color}");

    protected ILocator GetParentCheckbox() => GetByTestId("parent-checkbox");

    protected async Task<bool> IsCheckboxCheckedAsync(string color)
    {
        var checkbox = GetCheckbox(color);
        var ariaChecked = await checkbox.GetAttributeAsync("aria-checked");
        return ariaChecked == "true";
    }

    protected async Task<string?> GetParentAriaCheckedAsync()
    {
        var parent = GetParentCheckbox();
        return await parent.GetAttributeAsync("aria-checked");
    }

    protected async Task WaitForCheckboxStateAsync(string color, bool expected, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var checkbox = GetCheckbox(color);
        await Assertions.Expect(checkbox).ToHaveAttributeAsync(
            "aria-checked",
            expected ? "true" : "false",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    protected async Task<string[]> GetCurrentValueAsync()
    {
        var valueState = GetByTestId("value-state");
        var text = await valueState.TextContentAsync();
        if (string.IsNullOrEmpty(text))
            return [];
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    #endregion

    #region Group Value Tests

    /// <summary>
    /// Tests that clicking a checkbox adds its value to the group.
    /// </summary>
    [Fact]
    public virtual async Task Click_AddsValueToGroup()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var red = GetCheckbox("red");
        await WaitForCheckboxStateAsync("red", false);

        await red.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", true);
        var value = await GetCurrentValueAsync();
        Assert.Contains("red", value);
    }

    /// <summary>
    /// Tests that clicking a checked checkbox removes its value from the group.
    /// </summary>
    [Fact]
    public virtual async Task Click_RemovesValueFromGroup()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithGroupDefaultValue("red"));

        await WaitForCheckboxStateAsync("red", true);

        var red = GetCheckbox("red");
        await red.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", false);
        var value = await GetCurrentValueAsync();
        Assert.DoesNotContain("red", value);
    }

    /// <summary>
    /// Tests that multiple checkboxes can be selected.
    /// </summary>
    [Fact]
    public virtual async Task MultipleCheckboxes_CanBeSelected()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var red = GetCheckbox("red");
        var green = GetCheckbox("green");

        await red.ClickAsync();
        await WaitForDelayAsync(100);
        await green.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", true);
        await WaitForCheckboxStateAsync("green", true);
        await WaitForCheckboxStateAsync("blue", false);

        var value = await GetCurrentValueAsync();
        Assert.Contains("red", value);
        Assert.Contains("green", value);
        Assert.DoesNotContain("blue", value);
    }

    /// <summary>
    /// Tests that defaultValue initializes the correct checkboxes.
    /// </summary>
    [Fact]
    public virtual async Task DefaultValue_InitializesCheckboxes()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithGroupDefaultValue("red", "blue"));

        await WaitForCheckboxStateAsync("red", true);
        await WaitForCheckboxStateAsync("green", false);
        await WaitForCheckboxStateAsync("blue", true);
    }

    #endregion

    #region OnValueChange Tests

    /// <summary>
    /// Tests that onValueChange is called when a checkbox is clicked.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_CalledOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        var red = GetCheckbox("red");
        await red.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that onValueChange is called for each checkbox interaction.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_CalledForEachInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var changeCount = GetByTestId("change-count");

        var red = GetCheckbox("red");
        var green = GetCheckbox("green");

        await red.ClickAsync();
        await WaitForDelayAsync(100);
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");

        await green.ClickAsync();
        await WaitForDelayAsync(100);
        await Assertions.Expect(changeCount).ToHaveTextAsync("2");

        await red.ClickAsync();
        await WaitForDelayAsync(100);
        await Assertions.Expect(changeCount).ToHaveTextAsync("3");
    }

    #endregion

    #region Disabled Tests

    /// <summary>
    /// Tests that disabled group prevents checkbox interactions.
    /// </summary>
    [Fact]
    public virtual async Task DisabledGroup_PreventsInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithDisabled(true));

        var red = GetCheckbox("red");
        await red.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", false);
    }

    /// <summary>
    /// Tests that disabled group propagates to all checkboxes.
    /// </summary>
    [Fact]
    public virtual async Task DisabledGroup_PropagatesDataAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithDisabled(true));

        var red = GetCheckbox("red");
        var green = GetCheckbox("green");
        var blue = GetCheckbox("blue");

        await Assertions.Expect(red).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(green).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(blue).ToHaveAttributeAsync("data-disabled", "");
    }

    /// <summary>
    /// Tests that disabled group has data-disabled attribute.
    /// </summary>
    [Fact]
    public virtual async Task DisabledGroup_HasDataDisabledAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithDisabled(true));

        var group = GetCheckboxGroup();
        await Assertions.Expect(group).ToHaveAttributeAsync("data-disabled", "");
    }

    #endregion

    #region Parent Checkbox Tests

    /// <summary>
    /// Tests that parent checkbox is unchecked when no children are checked.
    /// </summary>
    [Fact]
    public virtual async Task ParentCheckbox_UncheckedWhenNoneSelected()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowParent(true));

        var parentAriaChecked = await GetParentAriaCheckedAsync();
        Assert.Equal("false", parentAriaChecked);
    }

    /// <summary>
    /// Tests that parent checkbox is indeterminate when some children are checked.
    /// </summary>
    [Fact]
    public virtual async Task ParentCheckbox_IndeterminateWhenSomeSelected()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowParent(true)
            .WithGroupDefaultValue("red"));

        var parentAriaChecked = await GetParentAriaCheckedAsync();
        Assert.Equal("mixed", parentAriaChecked);
    }

    /// <summary>
    /// Tests that parent checkbox is checked when all children are checked.
    /// </summary>
    [Fact]
    public virtual async Task ParentCheckbox_CheckedWhenAllSelected()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowParent(true)
            .WithGroupDefaultValue("red", "green", "blue"));

        var parentAriaChecked = await GetParentAriaCheckedAsync();
        Assert.Equal("true", parentAriaChecked);
    }

    /// <summary>
    /// Tests that clicking parent checkbox selects all children.
    /// </summary>
    [Fact]
    public virtual async Task ParentCheckbox_ClickSelectsAll()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowParent(true));

        var parent = GetParentCheckbox();
        await parent.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", true);
        await WaitForCheckboxStateAsync("green", true);
        await WaitForCheckboxStateAsync("blue", true);
    }

    /// <summary>
    /// Tests that clicking parent checkbox when all selected deselects all.
    /// </summary>
    [Fact]
    public virtual async Task ParentCheckbox_ClickDeselectsAll()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowParent(true)
            .WithGroupDefaultValue("red", "green", "blue"));

        var parent = GetParentCheckbox();
        await parent.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", false);
        await WaitForCheckboxStateAsync("green", false);
        await WaitForCheckboxStateAsync("blue", false);
    }

    /// <summary>
    /// Tests that parent checkbox updates when child is toggled.
    /// </summary>
    [Fact]
    public virtual async Task ParentCheckbox_UpdatesOnChildToggle()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowParent(true)
            .WithGroupDefaultValue("red", "green"));

        // Initially indeterminate (2 of 3)
        var parentAriaChecked = await GetParentAriaCheckedAsync();
        Assert.Equal("mixed", parentAriaChecked);

        // Select the third one
        var blue = GetCheckbox("blue");
        await blue.ClickAsync();
        await WaitForDelayAsync(100);

        // Should now be fully checked
        parentAriaChecked = await GetParentAriaCheckedAsync();
        Assert.Equal("true", parentAriaChecked);
    }

    #endregion

    #region External State Control Tests

    /// <summary>
    /// Tests that external toggle button updates the group value.
    /// </summary>
    [Fact]
    public virtual async Task ExternalToggle_UpdatesGroupValue()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        await WaitForCheckboxStateAsync("red", false);

        var toggleButton = GetByTestId("toggle-red");
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", true);
    }

    /// <summary>
    /// Tests that select all button selects all checkboxes.
    /// </summary>
    [Fact]
    public virtual async Task SelectAll_SelectsAllCheckboxes()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var selectAllButton = GetByTestId("select-all");
        await selectAllButton.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", true);
        await WaitForCheckboxStateAsync("green", true);
        await WaitForCheckboxStateAsync("blue", true);
    }

    /// <summary>
    /// Tests that clear all button deselects all checkboxes.
    /// </summary>
    [Fact]
    public virtual async Task ClearAll_DeselectsAllCheckboxes()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithGroupDefaultValue("red", "green", "blue"));

        var clearAllButton = GetByTestId("clear-all");
        await clearAllButton.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", false);
        await WaitForCheckboxStateAsync("green", false);
        await WaitForCheckboxStateAsync("blue", false);
    }

    #endregion

    #region Form Integration Tests

    /// <summary>
    /// Tests that group values are included in form submission.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesGroupValues()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowForm(true)
            .WithGroupName("colors")
            .WithGroupDefaultValue("red", "blue"));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        var text = await formData.TextContentAsync();
        Assert.Contains("colors=red", text);
        Assert.Contains("colors=blue", text);
    }

    /// <summary>
    /// Tests that empty group submits nothing.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_EmptyGroupSubmitsNothing()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup")
            .WithShowForm(true)
            .WithGroupName("colors"));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        var text = await formData.TextContentAsync();
        Assert.True(string.IsNullOrEmpty(text));
    }

    #endregion

    #region Keyboard Navigation Tests

    /// <summary>
    /// Tests that Space key toggles checkbox in group.
    /// </summary>
    [Fact]
    public virtual async Task Space_TogglesCheckboxInGroup()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var red = GetCheckbox("red");
        await red.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", true);
    }

    /// <summary>
    /// Tests that Enter key does not toggle checkbox in group.
    /// </summary>
    [Fact]
    public virtual async Task Enter_DoesNotToggleCheckboxInGroup()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var red = GetCheckbox("red");
        await red.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckboxStateAsync("red", false);
    }

    #endregion

    #region ARIA Tests

    /// <summary>
    /// Tests that group has role="group".
    /// </summary>
    [Fact]
    public virtual async Task Group_HasRoleGroup()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var group = GetCheckboxGroup();
        await Assertions.Expect(group).ToHaveAttributeAsync("role", "group");
    }

    /// <summary>
    /// Tests that checkboxes in group have role="checkbox".
    /// </summary>
    [Fact]
    public virtual async Task Checkboxes_HaveRoleCheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var red = GetCheckbox("red");
        var green = GetCheckbox("green");
        var blue = GetCheckbox("blue");

        await Assertions.Expect(red).ToHaveAttributeAsync("role", "checkbox");
        await Assertions.Expect(green).ToHaveAttributeAsync("role", "checkbox");
        await Assertions.Expect(blue).ToHaveAttributeAsync("role", "checkbox");
    }

    #endregion

    #region State Display Tests

    /// <summary>
    /// Tests that value state display updates correctly.
    /// </summary>
    [Fact]
    public virtual async Task ValueStateDisplay_UpdatesOnChange()
    {
        await NavigateAsync(CreateUrl("/tests/checkboxgroup"));

        var valueState = GetByTestId("value-state");
        await Assertions.Expect(valueState).ToHaveTextAsync("");

        var red = GetCheckbox("red");
        await red.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(valueState).ToContainTextAsync("red");
    }

    #endregion
}
