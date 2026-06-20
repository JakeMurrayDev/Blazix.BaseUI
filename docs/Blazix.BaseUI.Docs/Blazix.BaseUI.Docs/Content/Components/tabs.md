# Tabs

Tabs organize related content into panels with keyboard-accessible tab navigation.

## Anatomy

```razor
@using Blazix.BaseUI.Tabs

<TabsRoot TValue="string" DefaultValue="@("overview")">
    <TabsList TValue="string">
        <TabsTab TValue="string" Value="@("overview")">Overview</TabsTab>
        <TabsIndicator TValue="string" />
    </TabsList>
    <TabsPanel TValue="string" Value="@("overview")">
        Overview content
    </TabsPanel>
</TabsRoot>
```

## Examples

### Links

Render tabs as anchors when the tabs should also act like navigation.

```razor
<TabsRoot TValue="string" DefaultValue="@("overview")">
    <TabsList TValue="string">
        <TabsTab TValue="string" Value="@("overview")" NativeButton="false" href="#overview">
            Overview
        </TabsTab>
        <TabsTab TValue="string" Value="@("settings")" NativeButton="false" href="#settings">
            Settings
        </TabsTab>
        <TabsIndicator TValue="string" />
    </TabsList>
    <TabsPanel TValue="string" Value="@("overview")">Overview content</TabsPanel>
    <TabsPanel TValue="string" Value="@("settings")">Settings content</TabsPanel>
</TabsRoot>
```

## Parts

Parts: `TabsRoot<TValue>`, `TabsList<TValue>`, `TabsTab<TValue>`, `TabsIndicator<TValue>`, and `TabsPanel<TValue>`.

Tabs expose `data-orientation` and `data-activation-direction` on root-driven parts. Tabs add `data-active` and `data-disabled`; panels add `data-hidden`, `data-starting-style`, and `data-ending-style`; the indicator exposes active-tab CSS variables such as `--active-tab-left`, `--active-tab-width`, and `--active-tab-height`.
