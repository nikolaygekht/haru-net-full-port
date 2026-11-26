# HpdfRadioButtonAppearance Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms.Appearance`

**Implements:** [IElementAppearance](IElementAppearance.md)

---

## Purpose

`HpdfRadioButtonAppearance` draws the appearance of a radio button: a circular frame, and in the "on" state a black
filled circle (the dot) in its center. It is the generator that
[HpdfWidgetAnnotation.SetButtonAppearances()](HpdfWidgetAnnotation.md#method-setbuttonappearances) uses for
`ButtonElementType.RadioButton` (with the default dot size), and therefore for every option created by
`HpdfRadioButton.CreateOption()`.

---

## Getting an Instance

```csharp
var standard = new HpdfRadioButtonAppearance();               // dot of half the circle size
var bigDot = new HpdfRadioButtonAppearance(fillRatio: 0.8f);
// or through the factory
IElementAppearance fromFactory = HpdfAppearanceFactory.CreateRadioButtonAppearance(0.8f);
```

Options created by `HpdfRadioButton.CreateOption()` use `fillRatio` 0.5. To give a widget another dot size, pass a
generator to [HpdfWidgetAnnotation.SetButtonAppearances()](HpdfWidgetAnnotation.md#method-setbuttonappearances):
`widget.SetButtonAppearances(new HpdfRadioButtonAppearance(0.8f), "Large")`.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms.Appearance;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var radio = new HpdfRadioButtonAppearance();

page.GSave();
page.Concat(1, 0, 0, 1, 50, 700);
radio.DrawFrame(page, 14, 14, new[] { 0f, 0f, 0.6f }, null);
radio.GenerateOnStateAppearance(page, 14, 14);
page.GRestore();

pdf.SaveToFile("radio-dot.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Caption](#property-caption) | `string` | `"l"`, the ZapfDingbats filled circle |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfRadioButtonAppearance()](#constructor-hpdfradiobuttonappearance) | Creates a generator with the given dot size |
| [DrawFrame()](#method-drawframe) | Draws the circular frame |
| [GenerateOnStateAppearance()](#method-generateonstateappearance) | Draws the black dot |
| [GenerateOffStateAppearance()](#method-generateoffstateappearance) | Draws nothing |

---

## Detailed Descriptions

#### Property: *Caption*

**Declaration:**
```csharp
public string Caption { get; }
```

**Description:**
Always `"l"`, the filled circle character of the ZapfDingbats font. `SetButtonAppearances()` writes it as the
widget's `/MK /CA` caption, which viewers draw in ZapfDingbats when they build the radio button appearance themselves.

---

#### Constructor: *HpdfRadioButtonAppearance()*

**Declaration:**
```csharp
public HpdfRadioButtonAppearance(float fillRatio = 0.5f)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| fillRatio | `float` | Diameter of the dot relative to the smaller side of the element; greater than 0 and at most 1 |

**Description:**
Creates a generator with the given dot size. With the default 0.5 the dot is half as wide as the frame.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentOutOfRangeException` | `fillRatio` is 0 or less, or greater than 1 |

---

#### Method: *DrawFrame()*

**Declaration:**
```csharp
public void DrawFrame(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the radio button in points |
| height | `float` | Height of the radio button in points |
| borderColor | `float[]?` | RGB border color (three components, 0 to 1), or `null` for no border |
| backgroundColor | `float[]?` | RGB background color (three components, 0 to 1), or `null` for no background |

**Return Value:**
`void`

**Description:**
Draws a circle centered in the element, as large as its smaller side, by calling
[HpdfWidgetFrame.DrawCircle()](HpdfWidgetFrame.md#method-drawcircle).

---

#### Method: *GenerateOnStateAppearance()*

**Declaration:**
```csharp
public void GenerateOnStateAppearance(IDrawable drawable, float width, float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the radio button in points |
| height | `float` | Height of the radio button in points |

**Return Value:**
`void`

**Description:**
Fills a black circle in the center of the element with the radius `min(width, height) / 2 * fillRatio`. The graphics
state is saved before and restored after drawing.

---

#### Method: *GenerateOffStateAppearance()*

**Declaration:**
```csharp
public void GenerateOffStateAppearance(IDrawable drawable, float width, float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the radio button in points |
| height | `float` | Height of the radio button in points |

**Return Value:**
`void`

**Description:**
Draws nothing; an unselected radio button shows only the frame drawn by [DrawFrame()](#method-drawframe).

---

## Related Types

- [IElementAppearance](IElementAppearance.md) - The implemented interface
- [HpdfCheckboxAppearance](HpdfCheckboxAppearance.md) - The checkbox counterpart
- [HpdfAppearanceFactory](HpdfAppearanceFactory.md), [ButtonElementType](ButtonElementType.md) - How widgets choose this generator
- [HpdfRadioButton](HpdfRadioButton.md) - The radio button field

---

[Back to Index](../../INDEX.md)
