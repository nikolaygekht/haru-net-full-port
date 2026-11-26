# HpdfWidgetFrame Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms.Appearance`

---

## Purpose

`HpdfWidgetFrame` is a static class that draws the frame of a form field widget: its background and a one-point
border in the colors of the widget's appearance characteristics (`/MK`). The border lies entirely inside the widget
rectangle. The library uses it for every appearance it generates: checkbox frames, radio button frames,
[HpdfWidgetAnnotation.SetFrameAppearance()](HpdfWidgetAnnotation.md#method-setframeappearance) (signature fields),
and the frames of text and choice fields when the document is saved.

The colors come from [HpdfWidgetAnnotation.SetAppearanceCharacteristics()](HpdfWidgetAnnotation.md#method-setappearancecharacteristics);
to change a widget's frame, call that method rather than this class.

---

## Getting an Instance

The class is static; call its methods directly. They draw on any `IDrawable` (namespace `Haru.Graphics`), such as an
`HpdfPage`, in a coordinate system whose origin is the lower-left corner of the widget.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms.Appearance;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A framed box 200 x 20 points at (50, 700), drawn like a text field frame
page.GSave();
page.Concat(1, 0, 0, 1, 50, 700);
HpdfWidgetFrame.DrawBox(page, 200, 20, new[] { 0f, 0f, 0.6f }, new[] { 0.95f, 0.95f, 1f });
page.GRestore();

pdf.SaveToFile("frame.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [BorderWidth](#property-borderwidth) | `float` (constant) | The border width in points: 1 |

---

## Methods

| Name | Description |
|------|-------------|
| [DrawBox()](#method-drawbox) | Draws a rectangular frame that fills the widget |
| [DrawCircle()](#method-drawcircle) | Draws a circular frame centered in the widget |

---

## Detailed Descriptions

#### Property: *BorderWidth*

**Declaration:**
```csharp
public const float BorderWidth = 1f;
```

**Description:**
The width of the frame border in points, the default width of an annotation border. The generated appearances
always use this width; a border style set with `SetBorderStyle()` does not change it.

---

#### Method: *DrawBox()*

**Declaration:**
```csharp
public static void DrawBox(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the widget in points |
| height | `float` | Height of the widget in points |
| borderColor | `float[]?` | RGB border color (components 0 to 1), or `null` for no border |
| backgroundColor | `float[]?` | RGB background color (components 0 to 1), or `null` for no background |

**Return Value:**
`void`

**Description:**
Fills the rectangle `(0, 0, width, height)` with the background color and strokes the border half a point inside
its edges, so the one-point line stays within the widget. Without a border the background fills the whole
rectangle. Nothing is drawn when both colors are `null`. The graphics state is saved before and restored after
drawing. The color arrays must have at least three elements.

---

#### Method: *DrawCircle()*

**Declaration:**
```csharp
public static void DrawCircle(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the widget in points |
| height | `float` | Height of the widget in points |
| borderColor | `float[]?` | RGB border color (components 0 to 1), or `null` for no border |
| backgroundColor | `float[]?` | RGB background color (components 0 to 1), or `null` for no background |

**Return Value:**
`void`

**Description:**
Draws a circle centered in the widget whose diameter is the smaller of `width` and `height`, with the same rules as
[DrawBox()](#method-drawbox): background fill, a one-point border inside the circle, nothing when both colors are
`null`. Radio buttons use it for their frame.

**Example:**
```csharp
page.GSave();
page.Concat(1, 0, 0, 1, 300, 700);
HpdfWidgetFrame.DrawCircle(page, 14, 14, new[] { 0f, 0f, 0f }, null);
page.GRestore();
```

---

## Related Types

- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - `SetAppearanceCharacteristics()`, `SetFrameAppearance()`
- [HpdfCheckboxAppearance](HpdfCheckboxAppearance.md), [HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md) - Use `DrawBox()` and `DrawCircle()`
- [IElementAppearance](IElementAppearance.md) - `DrawFrame()`

---

[Back to Index](../../INDEX.md)
