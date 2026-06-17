# Slider

An easily stylable range input.

Rendered docs: `/components/slider`

## Usage guidelines

- Slider controls need an accessible name. Prefer `SliderLabel`, or set `AriaLabel` on each thumb when no visible label is rendered.

## Anatomy

```razor
@using Blazix.BaseUI.Slider

<SliderRoot>
    <SliderLabel />
    <SliderValue />
    <SliderControl>
        <SliderTrack>
            <SliderIndicator />
            <SliderThumb />
        </SliderTrack>
    </SliderControl>
</SliderRoot>
```

## Examples

### Range slider

```razor
<SliderRoot DefaultValues="@(new double[] { 25, 75 })">
    <SliderControl>
        <SliderTrack>
            <SliderIndicator />
            <SliderThumb Index="0" AriaLabel="Minimum price" />
            <SliderThumb Index="1" AriaLabel="Maximum price" />
        </SliderTrack>
    </SliderControl>
</SliderRoot>
```

### Thumb alignment

```razor
<SliderRoot ThumbAlignment="ThumbAlignment.Edge" DefaultValue="25">
    ...
</SliderRoot>
```

### Vertical

```razor
<SliderRoot Orientation="Orientation.Vertical" DefaultValue="35">
    ...
</SliderRoot>
```

### Form integration

```razor
<Form Model="@model">
    <SliderRoot Name="volume">
        <SliderLabel>Volume</SliderLabel>
        <SliderControl>
            <SliderTrack>
                <SliderIndicator />
                <SliderThumb />
            </SliderTrack>
        </SliderControl>
    </SliderRoot>
</Form>
```

## API reference

Parts: `SliderRoot`, `SliderLabel`, `SliderValue`, `SliderControl`, `SliderTrack`, `SliderIndicator`, and `SliderThumb`.

Root exposes single and range value parameters, min/max/step configuration, `ThumbCollisionBehavior`, `ThumbAlignment`, form parameters, formatting, and value-change/commit callbacks. All visual parts expose orientation, dragging, disabled, and Field state data attributes. Thumbs render hidden range inputs and expose `Index`, `AriaLabel`, `AriaLabelledBy`, `AriaDescribedBy`, `GetAriaLabel`, and `GetAriaValueText`.
