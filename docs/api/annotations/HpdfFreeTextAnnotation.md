# HpdfFreeTextAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfFreeTextAnnotation` is a free text annotation (PDF 1.3): text shown directly on the page in a box, instead of in
a pop-up window. The box can have a border (also a cloudy one) and a background, and a callout line that points at an
area of the page.
Use it for comments that should be readable without clicking, and for text "typed" onto a form that has no fields.

---

## Getting an Instance

Create free text with the page it is on:

```csharp
public HpdfFreeTextAnnotation CreateFreeTextAnnotation(HpdfRect rect, string text, HpdfFont font, float fontSize)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The text box; reversed edges are swapped |
| text | `string` | The text (`/Contents`); may be empty. Line breaks (`\n`) are kept |
| font | `HpdfFont` | The font of the text: any font of the document (standard, TrueType, Type1, CID) of horizontal writing |
| fontSize | `float` | The font size, positive |

The method creates the annotation (`/Subtype /FreeText`) and adds it to the page's `/Annots` array. It writes the
default appearance string `/DA` with the font's resource name, the size and the text color, for example
`/DA (/F1 11 Tf 0 g)`. Until it is changed, the text is black, the box has a solid black border 1 point wide and no
background, the text is aligned left, and the annotation has the Print flag (`/F 4`). Creating one raises the document
version to PDF 1.3.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `font` is `null`, or a font of vertical writing |
| `HpdfException` (`InvalidParameter`) | `fontSize` is not a positive number |

The arguments are checked before the annotation is created, so a call that throws adds nothing to the page or the
document.

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)), with the font of the annotation, so the text
looks the same in every viewer; the font is added to the resources of the stream, and an embedded font includes the
glyphs of the text in its subset. As in Adobe Acrobat, the colors are:

| Part | Color |
|------|-------|
| Text, border, callout line and its ending | The text color ([SetTextRgbColor()](#method-settextrgbcolor)), the color of `/DA`; black by default |
| Background of the text box | The annotation color `/C` ([SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor)); none by default |

- The border is drawn inside the text box with the width of [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle)
  (1 point until it is called; 0 draws none); `HpdfBorderStyle.Dashed` makes it dashed, the other styles draw it
  solid. A typewriter annotation ([SetIntent()](#method-setintent)) has no border.
- The text starts 2 points inside the border at the top of the box. It is wrapped to the width of the box like the
  text of a form field: at spaces, between the characters of Chinese and Japanese text where a line may break, and
  inside a word only when the word alone is wider than the box. The line height is the ascent minus the descent of
  the font, at least 1.15 times the font size. Text that does not fit into the box is cut off at its edges.
- The text color can be gray, RGB or CMYK; [SetNoColor()](HpdfAnnotation.md#method-setnocolor) removes the background.
- With a cloudy border ([SetCloudEffect()](#method-setcloudeffect)) the cloud replaces the rectangular border: it is
  drawn around the text box, stroked in the text color and filled with the background color.

The default appearance string `/DA` names the font by its resource name. When the document has an AcroForm
(`HpdfDocument.GetOrCreateAcroForm()`), the font is also added to the form's default resources `/DR` when the document
is saved, so that a viewer that redraws the annotation (for example when the reader edits the text) finds the font;
no form is created for it. If another font of the form, or of another annotation, has the same resource name (for
example `Helv`), saving throws `HpdfException` (`InvalidOperation`).

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

// A plain text box
page.CreateFreeTextAnnotation(new HpdfRect(50, 650, 270, 740),
    "The text is wrapped to the width of the box and drawn with the font given here.", font, 11);

// Blue, centered text on a yellow background with a dashed border
var note = page.CreateFreeTextAnnotation(new HpdfRect(310, 650, 540, 740), "Approved with changes", font, 14);
note.SetTextRgbColor(0f, 0f, 0.7f);
note.SetRgbColor(1f, 1f, 0.75f);
note.SetAlignment(HpdfTextAlignment.Center);
note.SetBorderStyle(HpdfBorderStyle.Dashed, 1.5f, 4, 2);

// A callout that points at a point of the page
var callout = page.CreateFreeTextAnnotation(new HpdfRect(310, 500, 540, 550),
    "This value comes from the 2025 report", font, 11);
callout.SetTextRgbColor(0.8f, 0f, 0f);
callout.SetCallout(new HpdfPoint(150, 480), new HpdfPoint(230, 525), new HpdfPoint(310, 525));
callout.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow);

pdf.SaveToFile("freetext.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.FreeText` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetFont()](#method-setfont) | Sets the font and the font size (`/DA`) |
| [SetTextRgbColor()](#method-settextrgbcolor) | Sets the color of the text, the border and the callout in RGB (`/DA`) |
| [SetTextCmykColor()](#method-settextcmykcolor) | The same in CMYK |
| [SetTextGrayColor()](#method-settextgraycolor) | The same as a gray level |
| [SetAlignment()](#method-setalignment) | Aligns the text left, centered or right (`/Q`) |
| [SetDefaultStyle()](#method-setdefaultstyle) | Sets the default style string for viewers that edit the text (`/DS`) |
| [SetCallout(HpdfPoint, HpdfPoint)](#method-setcallout) | Sets a callout line of two points (`/CL`) |
| [SetCallout(HpdfPoint, HpdfPoint, HpdfPoint)](#method-setcallout-1) | Sets a callout line of three points, with a bend (`/CL`) |
| [RemoveCallout()](#method-removecallout) | Removes the callout line |
| [SetCloudEffect()](#method-setcloudeffect) | Draws the border of the text box as a cloud (`/BE`) |
| [SetLineEndingStyle()](#method-setlineendingstyle) | Sets the ending of the callout line (`/LE`) |
| [SetIntent()](#method-setintent) | Sets how the annotation is meant to be shown: plain, callout or typewriter (`/IT`) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the width and dash of the border (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the background color (`/C`) (inherited) |

---

## Detailed Descriptions

#### Method: *SetFont()*

**Declaration:**
```csharp
public void SetFont(HpdfFont font, float fontSize)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| font | `HpdfFont` | The font: any font of the document of horizontal writing |
| fontSize | `float` | The font size, positive |

**Return Value:**
`void`

**Description:**
Replaces the font and the size given at creation and writes them to `/DA`. Use a font that can show the text, for
example an embedded TrueType or CID font for Chinese, Japanese or Korean text (see the
[Fonts Guide](../../guides/FontsGuide.md)).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `font` is `null`, or a font of vertical writing |
| `HpdfException` (`InvalidParameter`) | `fontSize` is not a positive number |

**Example:**
```csharp
freeText.SetFont(boldFont, 14);
```

---

#### Method: *SetTextRgbColor()*

**Declaration:**
```csharp
public void SetTextRgbColor(float r, float g, float b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r | `float` | Red, from 0 to 1 |
| g | `float` | Green, from 0 to 1 |
| b | `float` | Blue, from 0 to 1 |

**Return Value:**
`void`

**Description:**
Sets the color of the text, the border and the callout line, and writes it to `/DA` (`r g b rg`). A later call of
any of the three text color methods replaces it. The background is set separately, with
[SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is below 0, above 1 or NaN |

**Example:**
```csharp
freeText.SetTextRgbColor(0.8f, 0f, 0f);   // red text and border
```

---

#### Method: *SetTextCmykColor()*

**Declaration:**
```csharp
public void SetTextCmykColor(float c, float m, float y, float k)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| c | `float` | Cyan, from 0 to 1 |
| m | `float` | Magenta, from 0 to 1 |
| y | `float` | Yellow, from 0 to 1 |
| k | `float` | Black, from 0 to 1 |

**Return Value:**
`void`

**Description:**
The same as [SetTextRgbColor()](#method-settextrgbcolor) with a CMYK color (`c m y k k` in `/DA`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is below 0, above 1 or NaN |

**Example:**
```csharp
freeText.SetTextCmykColor(1f, 0.5f, 0f, 0.2f);
```

---

#### Method: *SetTextGrayColor()*

**Declaration:**
```csharp
public void SetTextGrayColor(float gray)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| gray | `float` | Gray level, from 0 (black) to 1 (white) |

**Return Value:**
`void`

**Description:**
The same as [SetTextRgbColor()](#method-settextrgbcolor) with a gray level (`gray g` in `/DA`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `gray` is below 0, above 1 or NaN |

**Example:**
```csharp
freeText.SetTextGrayColor(0.3f);
```

---

#### Method: *SetAlignment()*

**Declaration:**
```csharp
public void SetAlignment(HpdfTextAlignment alignment)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| alignment | `HpdfTextAlignment` | `Left`, `Center` or `Right` (see [HpdfTextAlignment](../types/HpdfTextAlignment.md)) |

**Return Value:**
`void`

**Description:**
Aligns every line of the text in the box and writes the quadding `/Q` (0 left, 1 centered, 2 right). Raises the
document version to PDF 1.4. Without a call the text is aligned left and `/Q` is absent. Free text cannot be
justified.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `alignment` is `Justify` or not an `HpdfTextAlignment` value |

**Example:**
```csharp
freeText.SetAlignment(HpdfTextAlignment.Right);
```

---

#### Method: *SetDefaultStyle()*

**Declaration:**
```csharp
public void SetDefaultStyle(string style)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| style | `string` | CSS text properties, such as `font: 12pt Helvetica; color: #FF0000` |

**Return Value:**
`void`

**Description:**
Writes the default style string `/DS` (PDF 1.5; the document version is raised). Viewers use it for the text when
the user edits the annotation. The generated appearance does not use it: it is drawn with the font and the color of
`/DA`, so keep the two consistent. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `style` is `null` |

**Example:**
```csharp
freeText.SetDefaultStyle("font: 11pt Helvetica; color: #000000");
```

---

#### Method: *SetCallout()*

**Declaration:**
```csharp
public void SetCallout(HpdfPoint start, HpdfPoint end)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| start | `HpdfPoint` | The point the callout points at; the line ending is drawn here |
| end | `HpdfPoint` | The point at the text box |

**Return Value:**
`void`

**Description:**
Adds a straight callout line from `start` to `end`, written as `/CL [x1 y1 x2 y2]`, and sets the intent to
`FreeTextCallout` (`/IT`, PDF 1.6). The line is drawn in the text color, as wide as the border (at least 1 point,
also without a border), with the ending of [SetLineEndingStyle()](#method-setlineendingstyle) at `start`.

The text box stays where it is: when the document is saved, the annotation rectangle grows to enclose the callout
line and its ending, and the distances from the annotation rectangle to the text box are written as `/RD [left top
right bottom]`. A later call replaces the callout line.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

**Example:**
```csharp
freeText.SetCallout(new HpdfPoint(100, 150), new HpdfPoint(50, 285));
```

---

#### Method: *SetCallout()*

**Declaration:**
```csharp
public void SetCallout(HpdfPoint start, HpdfPoint knee, HpdfPoint end)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| start | `HpdfPoint` | The point the callout points at; the line ending is drawn here |
| knee | `HpdfPoint` | The point where the line bends |
| end | `HpdfPoint` | The point at the text box |

**Return Value:**
`void`

**Description:**
The same as the two-point overload, with a line that bends at `knee`: `/CL [x1 y1 x2 y2 x3 y3]`. Usually the last
segment is horizontal and ends at the left or right edge of the text box.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

**Example:**
```csharp
freeText.SetCallout(new HpdfPoint(300, 150), new HpdfPoint(280, 285), new HpdfPoint(250, 285));
```

---

#### Method: *RemoveCallout()*

**Declaration:**
```csharp
public void RemoveCallout()
```

**Return Value:**
`void`

**Description:**
Removes the callout line: `/CL` and the line ending `/LE` are removed, and the intent becomes the plain `FreeText` (it
is `SetIntent(HpdfFreeTextIntent.Plain)`). Without a cloudy border, `/RD` is removed too and the annotation rectangle
is the text box again; with one, the rectangle still encloses the cloud. Without a callout line the call does nothing.

**Example:**
```csharp
freeText.RemoveCallout();
```

---

#### Method: *SetCloudEffect()*

**Declaration:**
```csharp
public void SetCloudEffect(int intensity)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| intensity | `int` | 0 (no cloud), 1, or 2 (larger bumps) |

**Return Value:**
`void`

**Description:**
Draws the border of the text box as a cloud: a row of overlapping round bumps around the box, one at every corner and
the others evenly spaced. Writes the border effect `/BE << /S /C /I intensity >>` and raises the document version to
PDF 1.6. The bumps have a radius of 4.75 points per unit of intensity; the cloud is stroked in the text color with
the border width and dash, and filled with the background color (`/C`). A typewriter annotation, which has no
border, shows only the fill of the cloud.

The text box stays where it is and keeps its size: the bumps lie outside it. When the document is saved, the
annotation rectangle grows by the radius of the bumps and half the border width on every side (and further for a
callout line), and the distances from the annotation rectangle to the text box are written as `/RD [left top right
bottom]`, so that viewers that redraw the annotation place the text the same way. Intensity 0 removes `/BE`; without
a callout line it also removes `/RD`, and the annotation rectangle is the text box again.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intensity` is not 0, 1 or 2 |

**Example:**
```csharp
var cloudy = page.CreateFreeTextAnnotation(new HpdfRect(300, 600, 500, 660), "Check these numbers", font, 12);
cloudy.SetCloudEffect(1);
cloudy.SetTextRgbColor(0.8f, 0f, 0f);
cloudy.SetRgbColor(1f, 0.95f, 0.95f);   // the fill of the cloud
```

---

#### Method: *SetLineEndingStyle()*

**Declaration:**
```csharp
public void SetLineEndingStyle(HpdfLineEndingStyle style)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| style | `HpdfLineEndingStyle` | The ending at the start point of the callout line (see [HpdfLineEndingStyle](HpdfLineEndingStyle.md)) |

**Return Value:**
`void`

**Description:**
Sets the ending drawn at the point the callout points at and writes it as `/LE` (PDF 1.6; the document version is
raised). For free text `/LE` is a single name, as ISO 32000-1 defines it (the C library writes an array). Closed
endings are filled with the text color. Without a call the callout line has no ending. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `style` is not an `HpdfLineEndingStyle` value |

**Example:**
```csharp
freeText.SetLineEndingStyle(HpdfLineEndingStyle.OpenArrow);
```

---

#### Method: *SetIntent()*

**Declaration:**
```csharp
public void SetIntent(HpdfFreeTextIntent intent)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| intent | `HpdfFreeTextIntent` | `Plain`, `Callout` or `TypeWriter` (see [HpdfFreeTextIntent](HpdfFreeTextIntent.md)) |

**Return Value:**
`void`

**Description:**
Writes the intent `/IT` (`/FreeText`, `/FreeTextCallout` or `/FreeTextTypeWriter`) and raises the document version to
PDF 1.6. A typewriter annotation is drawn without a border, as text typed onto the page; viewers also offer a
typewriter tool for editing it. [SetCallout()](#method-setcallout) sets `Callout` itself. An intent other than
`Callout` removes a callout line (`/CL`, `/LE`, and `/RD` unless the border is a cloud; the rectangle is the text box
again, or encloses the cloud); `Callout` alone does not add one: that is done by `SetCallout()`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intent` is not an `HpdfFreeTextIntent` value |

**Example:**
```csharp
freeText.SetIntent(HpdfFreeTextIntent.TypeWriter);
```

---

## Limitations

- The text is given at creation; there is no method to change it.
- The font of `/DA` goes to the default resources (`/DR`) of the document's AcroForm only when the document has one;
  without a form, the generated appearance still carries the font, so the annotation is shown correctly, but a viewer
  that redraws the annotation when the user edits it may substitute another font.
- Rich text ([SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), `/RC`) is written for viewers, but the
  generated appearance draws the plain text; a free text annotation cannot be written vertically.

---

## Related Types

- [HpdfFreeTextIntent](HpdfFreeTextIntent.md), [HpdfLineEndingStyle](HpdfLineEndingStyle.md),
  [HpdfTextAlignment](../types/HpdfTextAlignment.md)
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up
- [HpdfAnnotation](HpdfAnnotation.md) - background color, border style, flags
- [HpdfFont](../core/HpdfFont.md) - fonts
- [HpdfPage](../core/HpdfPage.md) - `CreateFreeTextAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
