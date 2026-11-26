# HpdfWidgetAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

**Base class:** `HpdfAnnotation` (namespace `Haru.Annotations`)

---

## Purpose

`HpdfWidgetAnnotation` is the visible part of a form field: a rectangle on a page where the field is shown and edited.
It carries the appearance of the field (appearance streams `/AP`, appearance state `/AS`, border and background
colors `/MK`) and the highlighting used when the user clicks it. A field ([HpdfField](HpdfField.md)) has one or more
widgets; each option of a radio button group is a widget.

---

## Getting an Instance

Usually the field creates its widgets:

| Method | Description |
|--------|-------------|
| `HpdfField.CreateWidget(HpdfRect rect)` | Creates a widget with the appearance the field type needs and adds it to the field |
| `HpdfRadioButton.CreateOption(string stateName, HpdfRect rect)` | Creates a radio button option widget |

A widget can also be created with its constructor and then added to a field with
[HpdfField.AddWidget()](HpdfField.md#method-addwidget) or `HpdfRadioButton.AddOption()`. In both cases the widget
must be placed on a page with `HpdfPage.AddWidgetAnnotation()`, which adds it to the page's `/Annots` array and
calls [SetPage()](#method-setpage).

```csharp
var widget = new HpdfWidgetAnnotation(pdf.Xref, new HpdfRect(50, 700, 65, 715));
```

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var city = new HpdfTextField(pdf.Xref, "City");
city.SetValue("Toronto");

var widget = city.CreateWidget(new HpdfRect(150, 700, 300, 720));
widget.SetAppearanceCharacteristics(
    borderColor: new[] { 0f, 0f, 0.6f },
    backgroundColor: new[] { 0.95f, 0.95f, 1f });
widget.SetHighlightMode(HpdfHighlightMode.InvertBorder);
page.AddWidgetAnnotation(widget);
form.AddField(city);

pdf.SaveToFile("widget.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying annotation dictionary (inherited) |
| [Type](#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Widget` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfWidgetAnnotation()](#constructor-hpdfwidgetannotation) | Creates a widget for a rectangle |
| [SetParent()](#method-setparent) | Links the widget to its field (`/Parent`) |
| [SetPage()](#method-setpage) | Links the widget to its page (`/P`) |
| [SetAppearanceState()](#method-setappearancestate) | Selects the appearance state (`/AS`) |
| [SetHighlightMode()](#method-sethighlightmode) | Sets how the widget is highlighted when clicked (`/H`) |
| [SetAppearanceCharacteristics()](#method-setappearancecharacteristics) | Sets border color, background color and rotation (`/MK`) |
| [SetButtonAppearances()](#method-setbuttonappearances) | Draws checkbox or radio button appearances for an on and an off state |
| [SetFrameAppearance()](#method-setframeappearance) | Draws an appearance that shows only the frame |
| [SetBorderStyle()](#method-setborderstyle) | Sets the border style (`/BS`) (inherited) |
| [SetRgbColor()](#method-setrgbcolor) | Sets the annotation color in RGB (`/C`) (inherited) |
| [SetCmykColor()](#method-setcmykcolor) | Sets the annotation color in CMYK (`/C`) (inherited) |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The annotation dictionary, registered in the cross-reference table. It contains `/Type /Annot`, `/Subtype /Widget`,
`/Rect`, `/F 4` (the Print flag, so the widget is printed) and the entries written by the methods of this class.
Inherited from `HpdfAnnotation`.

---

#### Property: *Type*

**Declaration:**
```csharp
public HpdfAnnotationType Type { get; }
```

**Description:**
The annotation type; for a widget always `HpdfAnnotationType.Widget`. Inherited from `HpdfAnnotation`.

---

#### Constructor: *HpdfWidgetAnnotation()*

**Declaration:**
```csharp
public HpdfWidgetAnnotation(HpdfXref xref, HpdfRect rect)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The document's cross-reference table (`pdf.Xref`) |
| rect | `HpdfRect` | The widget rectangle, `new HpdfRect(left, bottom, right, top)` in page coordinates |

**Description:**
Creates the annotation dictionary and registers it in the cross-reference table. A rectangle whose top is below its
bottom is normalized. The widget has no appearance yet; the appearance streams created later by
[SetButtonAppearances()](#method-setbuttonappearances) and [SetFrameAppearance()](#method-setframeappearance) have
the size of this rectangle.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `xref` is `null` |

---

#### Method: *SetParent()*

**Declaration:**
```csharp
public void SetParent(HpdfField field)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| field | `HpdfField` | The field the widget belongs to |

**Return Value:**
`void`

**Description:**
Writes `/Parent`. [HpdfField.AddWidget()](HpdfField.md#method-addwidget) calls it; there is normally no reason to
call it directly, because it does not add the widget to the field's `/Kids`. The parent can be set only once: a
second call throws `ArgumentException`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `field` is `null` |
| `ArgumentException` | The widget already has a parent |

---

#### Method: *SetPage()*

**Declaration:**
```csharp
public void SetPage(HpdfDict page)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | `HpdfDict` | The page dictionary |

**Return Value:**
`void`

**Description:**
Writes `/P`, the page the widget is on. `HpdfPage.AddWidgetAnnotation()` calls it and also adds the widget to the
page; there is normally no reason to call it directly. The page can be set only once, so a widget can be placed on one
page only: a second call (also through `AddWidgetAnnotation()`) throws `ArgumentException`. To show a field on several
pages, create one widget per page.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `page` is `null` |
| `ArgumentException` | The widget already has a page |

---

#### Method: *SetAppearanceState()*

**Declaration:**
```csharp
public void SetAppearanceState(string state)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| state | `string` | The name of the appearance state to show, for example `"Yes"` or `"Off"` |

**Return Value:**
`void`

**Description:**
Writes `/AS`, which selects one of the state appearances created by
[SetButtonAppearances()](#method-setbuttonappearances). Checkboxes and radio buttons call it whenever their value
changes, so it is rarely needed directly; a state set by hand is overwritten by the next value change of the field.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `state` is `null` or empty |

---

#### Method: *SetHighlightMode()*

**Declaration:**
```csharp
public void SetHighlightMode(HpdfHighlightMode mode)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| mode | `HpdfHighlightMode` | How the viewer highlights the widget while it is clicked |

**Return Value:**
`void`

**Description:**
Writes `/H` (see [HpdfHighlightMode](#hpdfhighlightmode-enum)). A value outside the enum writes `InvertBox`. Without
a call, `/H` is absent and viewers use `InvertBox`, the PDF default.

**Example:**
```csharp
widget.SetHighlightMode(HpdfHighlightMode.NoHighlight);
```

---

#### Method: *SetAppearanceCharacteristics()*

**Declaration:**
```csharp
public void SetAppearanceCharacteristics(float[]? borderColor = null, float[]? backgroundColor = null, int rotation = 0)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| borderColor | `float[]?` | Border color as RGB, three components from 0 to 1; `null` for no border |
| backgroundColor | `float[]?` | Background color as RGB, three components from 0 to 1; `null` for no background |
| rotation | `int` | Counterclockwise rotation of the widget content: 0, 90, 180 or 270 |

**Return Value:**
`void`

**Description:**
Writes the appearance characteristics dictionary `/MK` (`/BC`, `/BG`, `/R`, and the caption `/CA` of a button) and
redraws the appearances that use them. Each call replaces all three values; an omitted argument means no border,
no background or no rotation. An array that does not have exactly three elements is treated as `null`; the
components themselves are not range-checked.

How the widget types use the characteristics:

| Widget | Effect |
|--------|--------|
| Text and choice fields | The appearance generated when the document is saved draws the frame and rotates the text. Without a call, these widgets have no frame |
| Checkboxes, radio buttons ([SetButtonAppearances()](#method-setbuttonappearances)) | The state appearances are redrawn with the new frame; the default black border is replaced (no colors: no frame). The mark is not rotated; the rotation is only written to `/MK` |
| Signature fields ([SetFrameAppearance()](#method-setframeappearance)) | The frame is redrawn with the new colors; the default black border is replaced |

The call can come before or after [SetButtonAppearances()](#method-setbuttonappearances) or
[SetFrameAppearance()](#method-setframeappearance). The frame is drawn with a one-point border inside the widget
rectangle (see [HpdfWidgetFrame](HpdfWidgetFrame.md)).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `rotation` is not 0, 90, 180 or 270 |

**Example:**
```csharp
var vertical = new HpdfTextField(pdf.Xref, "Vertical");
vertical.SetValue("Rotated text");
var verticalWidget = vertical.CreateWidget(new HpdfRect(450, 500, 470, 650));
verticalWidget.SetAppearanceCharacteristics(borderColor: new[] { 0.5f, 0.5f, 0.5f }, rotation: 90);
page.AddWidgetAnnotation(verticalWidget);
form.AddField(vertical);
```

---

#### Method: *SetButtonAppearances()*

**Declaration:**
```csharp
public void SetButtonAppearances(ButtonElementType elementType, string onStateName = "Yes", string offStateName = "Off")
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| elementType | `ButtonElementType` | `Checkbox` (check mark in a square) or `RadioButton` (dot in a circle) |
| onStateName | `string` | The name of the "on" state: `"Yes"` for a checkbox, the option name for a radio button |
| offStateName | `string` | The name of the "off" state, normally `"Off"` |

**Return Value:**
`void`

**Description:**
Creates the normal appearance (`/AP /N`) of a button widget: a dictionary with one Form XObject per state, drawn by
the generator that [HpdfAppearanceFactory.CreateAppearance()](HpdfAppearanceFactory.md#method-createappearance)
returns for `elementType`. Both states draw the frame; the "on" state adds the check mark or the dot. The widget's
`/MK /CA` caption is set to the generator's ZapfDingbats character (`4` check mark, `l` filled circle), which viewers
use when they draw the button themselves; the form adds the ZapfDingbats font (`ZaDb`) to its `/DR` for them.

The frame uses the colors of [SetAppearanceCharacteristics()](#method-setappearancecharacteristics); if that has not
been called, the widget gets a black border. The method does not set `/AS`: the field does that when the widget is
added and whenever its value changes.

`HpdfCheckbox.CreateWidget()` and `HpdfRadioButton.CreateOption()` call this method, so call it yourself only for
widgets you create with the constructor. Calling it again replaces the previous appearances. The state names are not
validated: they must be different from each other and not `null`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | `elementType` is not a `ButtonElementType` value, or both state names are equal |

**Example:**
```csharp
var size = new HpdfRadioButton(pdf.Xref, "Size");
var small = new HpdfWidgetAnnotation(pdf.Xref, new HpdfRect(50, 450, 65, 465));
small.SetAppearanceCharacteristics(borderColor: new[] { 0f, 0f, 0.8f }, backgroundColor: new[] { 0.9f, 0.9f, 1f });
small.SetButtonAppearances(ButtonElementType.RadioButton, "Small");
size.AddOption("Small", small);
page.AddWidgetAnnotation(small);
form.AddField(size);
```

---

#### Method: *SetFrameAppearance()*

**Declaration:**
```csharp
public void SetFrameAppearance()
```

**Return Value:**
`void`

**Description:**
Creates a normal appearance (`/AP /N`) that draws only the frame: the border and background of
[SetAppearanceCharacteristics()](#method-setappearancecharacteristics), or a black border if that has not been
called. `HpdfSignature.CreateWidget()` uses it so an unsigned signature field is visible in every viewer and in
print. It replaces any button appearances and removes the `/MK /CA` caption.

For text and choice fields there is no need to call it: their appearance is generated when the document is saved
and replaces this one.

**Example:**
```csharp
var box = new HpdfWidgetAnnotation(pdf.Xref, new HpdfRect(300, 400, 500, 450));
box.SetAppearanceCharacteristics(borderColor: new[] { 0.7f, 0f, 0f });
box.SetFrameAppearance();
```

---

#### Method: *SetBorderStyle()*

**Declaration:**
```csharp
public void SetBorderStyle(HpdfBorderStyle style, float width = 1.0f, ushort dashOn = 3, ushort dashOff = 3)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| style | `HpdfBorderStyle` | `Solid`, `Dashed`, `Beveled`, `Inset` or `Underlined` (namespace `Haru.Annotations`) |
| width | `float` | Border width in points; written only when it is not 1 |
| dashOn | `ushort` | Dash length for `Dashed` |
| dashOff | `ushort` | Gap length for `Dashed` |

**Return Value:**
`void`

**Description:**
Writes the border style dictionary `/BS`. Inherited from `HpdfAnnotation`. It describes the border for viewers that
draw the widget themselves; the appearances generated by the library always draw a solid one-point border. The
border style can be set only once per widget: a second call throws `ArgumentException`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `style` is not an `HpdfBorderStyle` value |
| `ArgumentException` | The border style was already set |

**Example:**
```csharp
widget.SetBorderStyle(HpdfBorderStyle.Dashed, 1.0f, 2, 2);
```

---

#### Method: *SetRgbColor()*

**Declaration:**
```csharp
public void SetRgbColor(float r, float g, float b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r | `float` | Red, 0 to 1 |
| g | `float` | Green, 0 to 1 |
| b | `float` | Blue, 0 to 1 |

**Return Value:**
`void`

**Description:**
Writes the annotation color `/C`. Inherited from `HpdfAnnotation`. For widgets, border and background colors come
from [SetAppearanceCharacteristics()](#method-setappearancecharacteristics); `/C` is not used by the generated
appearances. The color can be set only once per widget, by this method or [SetCmykColor()](#method-setcmykcolor).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is outside 0 to 1 |
| `ArgumentException` | The color was already set |

---

#### Method: *SetCmykColor()*

**Declaration:**
```csharp
public void SetCmykColor(float c, float m, float y, float k)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| c | `float` | Cyan, 0 to 1 |
| m | `float` | Magenta, 0 to 1 |
| y | `float` | Yellow, 0 to 1 |
| k | `float` | Black, 0 to 1 |

**Return Value:**
`void`

**Description:**
Writes the annotation color `/C` with four components. Inherited from `HpdfAnnotation`. The same remarks as for
[SetRgbColor()](#method-setrgbcolor) apply.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is outside 0 to 1 |
| `ArgumentException` | The color was already set |

---

## HpdfHighlightMode Enum

**Namespace:** `Haru.Annotations`

The highlighting a viewer applies while the user clicks a widget, set with
[SetHighlightMode()](#method-sethighlightmode).

| Name | Value | Description |
|------|-------|-------------|
| `NoHighlight` | 0 | No highlighting (`/H /N`) |
| `InvertBox` | 1 | Inverts the colors inside the widget rectangle (`/H /I`); the PDF default |
| `InvertBorder` | 2 | Inverts the border of the widget (`/H /O`) |
| `DownAppearance` | 3 | Shows the widget's down appearance (`/H /P`). The library creates no down appearances (`/AP /D`) |

```csharp
widget.SetHighlightMode(HpdfHighlightMode.InvertBox);
```

---

## Remarks

- Text and choice field widgets get their appearance stream when the document is saved; it is regenerated on every
  save, so a document can be changed and saved again. The generated text uses the WinAnsi character set of the
  standard fonts.
- `SetParent`, `SetPage`, `SetBorderStyle` and the color methods can be applied only once per widget.

---

## Related Types

- [HpdfField](HpdfField.md) - The field a widget belongs to
- [HpdfCheckbox](HpdfCheckbox.md), [HpdfRadioButton](HpdfRadioButton.md), [HpdfSignature](HpdfSignature.md) - Fields
  whose widgets draw their own appearances
- [ButtonElementType](ButtonElementType.md), [HpdfAppearanceFactory](HpdfAppearanceFactory.md),
  [IElementAppearance](IElementAppearance.md), [HpdfWidgetFrame](HpdfWidgetFrame.md) - Appearance generators
- [HpdfPage](../core/HpdfPage.md) - `AddWidgetAnnotation()`
- [Forms Guide](../../guides/FormsGuide.md)

---

[Back to Index](../../INDEX.md)
