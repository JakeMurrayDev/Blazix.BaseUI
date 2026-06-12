# Blazor Demo Fluent Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the demo shell and shared showcase surfaces as a Fluent 2 inspired BlazorBaseUI demo with light and dark modes.

**Architecture:** Apply the redesign through shared demo surfaces first: layout, navigation, showcase wrapper, section wrapper, home page, and global CSS tokens. Keep individual component examples intact where possible so their behavior remains the same while inheriting the new shell.

**Tech Stack:** .NET 10, Blazor Web App, BlazorBaseUI primitives, Razor components, CSS custom properties, bUnit/xUnit, browser verification.

---

## File Structure

- Modify `tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj` to reference the demo client components for focused bUnit smoke coverage.
- Create `tests/BlazorBaseUI.Tests/Demo/DemoShellTests.cs` to assert the shared demo wrappers expose the Fluent shell contract.
- Modify `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Layout/MainLayout.razor` for theme state, command bar, mobile panel, and BlazorBaseUI theme switch.
- Modify `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Layout/NavMenu.razor` for grouped component navigation.
- Modify `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/ComponentShowcase.razor` for header metadata and BlazorBaseUI tabs render-mode control.
- Modify `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/DemoSection.razor` for Fluent section cards and code disclosure.
- Modify `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Pages/Home.razor` for the new overview/catalog page.
- Modify `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/wwwroot/app.css` for theme tokens and shared Fluent styling.

## Tasks

### Task 1: Add Demo Wrapper Smoke Tests

**Files:**
- Modify: `tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj`
- Create: `tests/BlazorBaseUI.Tests/Demo/DemoShellTests.cs`

- [ ] **Step 1: Add demo client test reference**

Add this project reference:

```xml
<ProjectReference Include="..\..\demo\BlazorBaseUI.Demo\BlazorBaseUI.Demo.Client\BlazorBaseUI.Demo.Client.csproj" />
```

- [ ] **Step 2: Write failing tests**

Create bUnit tests that render `ComponentShowcase` and `DemoSection`, then assert for the new classes and BlazorBaseUI tab markup.

- [ ] **Step 3: Run tests and verify RED**

Run:

```bash
dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~DemoShellTests"
```

Expected: tests fail because the Fluent shell classes and tabs are not implemented yet.

### Task 2: Implement Shared Fluent Wrappers

**Files:**
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/ComponentShowcase.razor`
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/DemoSection.razor`

- [ ] **Step 1: Replace the old showcase markup**

Use semantic classes: `demo-showcase`, `demo-showcase__header`, `demo-render-tabs`, `demo-chip`, and `demo-showcase__content`.

- [ ] **Step 2: Replace the old section markup**

Use semantic classes: `demo-section`, `demo-section__header`, `demo-section__preview`, `demo-section__code`, and `demo-disclosure`.

- [ ] **Step 3: Run the focused tests and verify GREEN**

Run:

```bash
dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~DemoShellTests"
```

Expected: tests pass.

### Task 3: Implement Fluent Shell And Navigation

**Files:**
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Layout/MainLayout.razor`
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Layout/NavMenu.razor`
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Layout/MainLayout.razor.css`

- [ ] **Step 1: Build command shell**

Use `Button`, `SwitchRoot`, and `SwitchThumb` for controls. Apply `data-theme` to the shell root and keep mobile menu behavior.

- [ ] **Step 2: Build grouped nav**

Group component links into Overview, Inputs, Overlays, Navigation, Feedback, and Utilities. Disabled future links render as non-clickable rows.

- [ ] **Step 3: Keep existing error UI styles**

Move only layout-level error UI styling into `MainLayout.razor.css`.

### Task 4: Implement Home And Theme CSS

**Files:**
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/Components/Pages/Home.razor`
- Modify: `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/wwwroot/app.css`

- [ ] **Step 1: Build the overview page**

Create a Fluent dashboard landing page with stats, component groups, and primary links using BlazorBaseUI `Button`.

- [ ] **Step 2: Add global tokens and shared classes**

Add `[data-theme="light"]` and `[data-theme="dark"]` variables plus reusable demo classes for shell, nav, cards, previews, buttons, controls, focus, code, and common component examples.

### Task 5: Verify Build And Browser Behavior

**Files:**
- No new files.

- [ ] **Step 1: Build**

Run:

```bash
dotnet build BlazorBaseUI.slnx
```

Expected: build succeeds.

- [ ] **Step 2: Run focused tests**

Run:

```bash
dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~DemoShellTests"
```

Expected: tests pass.

- [ ] **Step 3: Run demo and inspect**

Run the demo app, open it in the browser, and verify desktop and mobile views, theme toggle, and render-mode navigation.

## Self-Review

- Spec coverage: The plan covers Fluent shell, light/dark theme, BlazorBaseUI controls, shared wrappers, navigation, home page, and verification.
- Placeholder scan: No placeholder tasks remain.
- Type consistency: Paths and component names match the current repository structure.
