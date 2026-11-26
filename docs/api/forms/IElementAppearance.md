# IElementAppearance Interface

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms.Appearance`

---

## Purpose

`IElementAppearance` is the interface of the generators that draw button appearances: the frame, the "on" state and
the "off" state of a checkbox or radio button, plus the ZapfDingbats caption character that viewers use when they
draw the button themselves. [HpdfWidgetAnnotation.SetButtonAppearances()](HpdfWidgetAnnotation.md#method-setbuttonappearances)
uses it to fill the widget's appearance streams and its `/MK /CA` caption.

The generators draw on any `IDrawable` (namespace `Haru.Graphics`): an `HpdfPage`, or an `HpdfAppearanceCanvas`
that wraps a Form XObject. Drawing uses the graphics extension methods of
[HpdfPageGraphics](../extensions/HpdfPageGraphics.md) and [HpdfPageShapes](../extensions/HpdfPageShapes.md), in a
coordinate system whose origin is the lower-left corner of the element.

---

## Getting an Instance

| Source | Description |
|--------|-------------|
| [HpdfAppearanceFactory.CreateAppearance()](HpdfAppearanceFactory.md#method-createappearance) | The generator for a [ButtonElementType](ButtonElementType.md) |
| [HpdfAppearanceFactory.CreateRadioButtonAppearance()](HpdfAppearanceFactory.md#method-createradiobuttonappearance) | A radio button generator with a custom dot size |
| `new HpdfCheckboxAppearance()`, `new HpdfRadioButtonAppearance()` | The implementations directly |

Any generator, including your own implementation of the interface, can be passed to a widget with
`HpdfWidgetAnnotation.SetButtonAppearances(IElementAppearance generator, ...)` (see
[Implementing the Interface](#implementing-the-interface)), or used to draw on pages and canvases you create yourself.

---

## Example

Drawing a checked checkbox directly on a page, for example in a printed (non-interactive) form:

```csharp
using Haru.Doc;
using Haru.Forms.Appearance;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

IElementAppearance checkbox = HpdfAppearanceFactory.CreateAppearance(ButtonElementType.Checkbox);

page.GSave();
page.Concat(1, 0, 0, 1, 50, 700);   // move the origin to the element's lower-left corner
checkbox.DrawFrame(page, 15, 15, new[] { 0f, 0f, 0f }, null);
checkbox.GenerateOnStateAppearance(page, 15, 15);
page.GRestore();

pdf.SaveToFile("printed-checkbox.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Caption](#property-caption) | `string` | The ZapfDingbats character of the "on" mark |

---

## Methods

| Name | Description |
|------|-------------|
| [DrawFrame()](#method-drawframe) | Draws the background and border shared by both states |
| [GenerateOnStateAppearance()](#method-generateonstateappearance) | Draws the "on" (checked, selected) mark |
| [GenerateOffStateAppearance()](#method-generateoffstateappearance) | Draws the "off" (unchecked) mark |

---

## Detailed Descriptions

#### Property: *Caption*

**Declaration:**
```csharp
string Caption { get; }
```

**Description:**
The character that shows the "on" mark in the ZapfDingbats font: `4` (check mark) for checkboxes, `l` (filled circle)
for radio buttons. `SetButtonAppearances()` writes it as the caption `/MK /CA` of the widget. Viewers that build
the button appearance themselves draw this character in ZapfDingbats, so the form adds that font (`ZaDb`) to its
default resources when it contains buttons.

---

#### Method: *DrawFrame()*

**Declaration:**
```csharp
void DrawFrame(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the element in points |
| height | `float` | Height of the element in points |
| borderColor | `float[]?` | RGB border color (three components, 0 to 1), or `null` for no border |
| backgroundColor | `float[]?` | RGB background color (three components, 0 to 1), or `null` for no background |

**Return Value:**
`void`

**Description:**
Draws the frame: a rectangle for checkboxes, a circle for radio buttons (see [HpdfWidgetFrame](HpdfWidgetFrame.md)).
Nothing is drawn when both colors are `null`. `SetButtonAppearances()` passes the colors of the widget's appearance
characteristics.

---

#### Method: *GenerateOnStateAppearance()*

**Declaration:**
```csharp
void GenerateOnStateAppearance(IDrawable drawable, float width, float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the element in points |
| height | `float` | Height of the element in points |

**Return Value:**
`void`

**Description:**
Draws the mark of the "on" state on top of the frame, scaled to the element size. The library's implementations
draw in black and save and restore the graphics state.

---

#### Method: *GenerateOffStateAppearance()*

**Declaration:**
```csharp
void GenerateOffStateAppearance(IDrawable drawable, float width, float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| drawable | `IDrawable` | The canvas to draw on |
| width | `float` | Width of the element in points |
| height | `float` | Height of the element in points |

**Return Value:**
`void`

**Description:**
Draws the mark of the "off" state on top of the frame. The library's implementations draw nothing: an unchecked
checkbox or unselected radio button shows only its frame.

---

## Implementing the Interface

A custom generator gives button widgets another look. `SetButtonAppearances()` calls `DrawFrame()` for both states,
then `GenerateOnStateAppearance()` or `GenerateOffStateAppearance()`, each on its own appearance stream, and redraws
them when the widget's appearance characteristics change. The width and height passed are those of the widget (swapped
for a widget rotated by 90 or 270 degrees). The frame can be drawn with [HpdfWidgetFrame](HpdfWidgetFrame.md).

```csharp
using Haru.Doc;
using Haru.Forms.Appearance;
using Haru.Graphics;

// A checkbox that shows a cross instead of a check mark
public class CrossAppearance : IElementAppearance
{
    public string Caption => "8";   // the cross in ZapfDingbats, for viewers that draw the button themselves

    public void DrawFrame(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
        => HpdfWidgetFrame.DrawBox(drawable, width, height, borderColor, backgroundColor);

    public void GenerateOnStateAppearance(IDrawable drawable, float width, float height)
    {
        drawable.GSave();
        drawable.SetLineWidth(1.5f);
        drawable.MoveTo(width * 0.25f, height * 0.25f);
        drawable.LineTo(width * 0.75f, height * 0.75f);
        drawable.MoveTo(width * 0.25f, height * 0.75f);
        drawable.LineTo(width * 0.75f, height * 0.25f);
        drawable.Stroke();
        drawable.GRestore();
    }

    public void GenerateOffStateAppearance(IDrawable drawable, float width, float height)
    {
    }
}
```

```csharp
var agree = new HpdfCheckbox(pdf.Xref, "Agree");
var widget = agree.CreateWidget(new HpdfRect(50, 600, 65, 615));
widget.SetButtonAppearances(new CrossAppearance());   // replaces the check mark; states "Yes" and "Off"
page.AddWidgetAnnotation(widget);
form.AddField(agree);
```

---

## Related Types

- [HpdfCheckboxAppearance](HpdfCheckboxAppearance.md), [HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md) - Implementations
- [HpdfAppearanceFactory](HpdfAppearanceFactory.md) - Creates the implementations
- [HpdfWidgetFrame](HpdfWidgetFrame.md) - Draws the frames
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - `SetButtonAppearances()`

---

[Back to Index](../../INDEX.md)
