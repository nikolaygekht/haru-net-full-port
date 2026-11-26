# HpdfCheckboxAppearance Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms.Appearance`

**Implements:** [IElementAppearance](IElementAppearance.md)

---

## Purpose

`HpdfCheckboxAppearance` draws the appearance of a checkbox: a rectangular frame, and in the "on" state a black
check mark drawn with path operators (no font needed). It is the generator that
[HpdfWidgetAnnotation.SetButtonAppearances()](HpdfWidgetAnnotation.md#method-setbuttonappearances) uses for
`ButtonElementType.Checkbox`, and therefore for every widget created by `HpdfCheckbox.CreateWidget()`.

---

## Getting an Instance

```csharp
var checkbox = new HpdfCheckboxAppearance();
// or
IElementAppearance same = HpdfAppearanceFactory.CreateAppearance(ButtonElementType.Checkbox);
```

The class has a public parameterless constructor and keeps no state.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms.Appearance;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var checkbox = new HpdfCheckboxAppearance();

// An unchecked and a checked box, 12 x 12 points, on a light gray background
float[] border = { 0f, 0f, 0f };
float[] background = { 0.9f, 0.9f, 0.9f };

page.GSave();
page.Concat(1, 0, 0, 1, 50, 700);
checkbox.DrawFrame(page, 12, 12, border, background);
checkbox.GenerateOffStateAppearance(page, 12, 12);
page.GRestore();

page.GSave();
page.Concat(1, 0, 0, 1, 70, 700);
checkbox.DrawFrame(page, 12, 12, border, background);
checkbox.GenerateOnStateAppearance(page, 12, 12);
page.GRestore();

pdf.SaveToFile("checkbox-marks.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Caption](#property-caption) | `string` | `"4"`, the ZapfDingbats check mark |

---

## Methods

| Name | Description |
|------|-------------|
| [DrawFrame()](#method-drawframe) | Draws the square frame |
| [GenerateOnStateAppearance()](#method-generateonstateappearance) | Draws the black check mark |
| [GenerateOffStateAppearance()](#method-generateoffstateappearance) | Draws nothing |

---

## Detailed Descriptions

#### Property: *Caption*

**Declaration:**
```csharp
public string Caption { get; }
```

**Description:**
Always `"4"`, the check mark character of the ZapfDingbats font. `SetButtonAppearances()` writes it as the widget's
`/MK /CA` caption, which viewers draw in ZapfDingbats when they build the checkbox appearance themselves.

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
| width | `float` | Width of the checkbox in points |
| height | `float` | Height of the checkbox in points |
| borderColor | `float[]?` | RGB border color (three components, 0 to 1), or `null` for no border |
| backgroundColor | `float[]?` | RGB background color (three components, 0 to 1), or `null` for no background |

**Return Value:**
`void`

**Description:**
Draws a rectangle that fills the element, by calling [HpdfWidgetFrame.DrawBox()](HpdfWidgetFrame.md#method-drawbox).

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
| width | `float` | Width of the checkbox in points |
| height | `float` | Height of the checkbox in points |

**Return Value:**
`void`

**Description:**
Fills a black check mark, scaled with the width and height of the element (a non-square element gives a stretched
mark). The graphics state is saved before and restored after drawing.

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
| width | `float` | Width of the checkbox in points |
| height | `float` | Height of the checkbox in points |

**Return Value:**
`void`

**Description:**
Draws nothing; an unchecked checkbox shows only the frame drawn by [DrawFrame()](#method-drawframe).

---

## Related Types

- [IElementAppearance](IElementAppearance.md) - The implemented interface
- [HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md) - The radio button counterpart
- [HpdfAppearanceFactory](HpdfAppearanceFactory.md), [ButtonElementType](ButtonElementType.md) - How widgets choose this generator
- [HpdfCheckbox](HpdfCheckbox.md) - The checkbox field

---

[Back to Index](../../INDEX.md)
