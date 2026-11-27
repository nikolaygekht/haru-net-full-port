# HpdfRedactAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfRedactAnnotation` is a redaction annotation (PDF 1.7; not in the C library): it marks content of the page that is
to be removed, such as a name or an account number. **The library removes nothing**: the content stays in the file
under the annotation. A viewer that applies redactions (such as Adobe Acrobat) removes the content under the
annotation when the user applies them, fills the area with the interior color and draws the overlay text into it.
Until then the area is shown as an outline. Use redaction annotations to hand a document to someone who reviews and
applies the redactions; to publish a document without some content, leave that content out when you create the page.

---

## Getting an Instance

Create redaction annotations with the page they are on:

```csharp
public HpdfRedactAnnotation CreateRedactAnnotation(HpdfRect rect, string text)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The area to redact; for several areas call [SetQuadPoints()](#method-setquadpoints) afterwards. Reversed edges are swapped |
| text | `string` | The text of the annotation (`/Contents`), for example the reason, shown in its pop-up window; may be empty |

The method creates the annotation (`/Subtype /Redact`) and adds it to the page's `/Annots` array. The outline is red
(`/C [1 0 0]`) until another color is set, there is no interior color and no overlay text, and the annotation has the
Print flag (`/F 4`). Creating one raises the document version to PDF 1.7. PDF/A-1 does not allow redaction annotations:
saving a PDF/A document with one throws `HpdfException` (`InvalidOperation`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |

### What is drawn and what is applied

| | Before the redaction is applied | After a viewer applies it |
|---|---|---|
| Shown | The appearance stream of the library: the outline of the area (or of every quadrilateral), 1 point wide, in the annotation color `/C` | The content under the area is removed; the area is filled with the interior color `/IC` and shows the overlay text |
| Set by | [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) and the other color setters; [SetNoColor()](HpdfAnnotation.md#method-setnocolor) draws no outline | [SetInteriorRgbColor()](#method-setinteriorrgbcolor), [SetOverlayText()](#method-setoverlaytext) |

The outline does not use the border style: it is always 1 point wide and solid. The annotation rectangle is the
rectangle given at creation; with quadrilaterals it is computed from them (with 1 point of room) when the document is
saved.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var bold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");
var page = pdf.AddPage();

string secret = "Account number: 1234 5678 9012";
page.SetFontAndSize(font, 12);
page.TextOut(50, 700, secret);

// The area of the text line, from the descent to the ascent
float width = font.MeasureText(secret, 12);
var area = new HpdfRect(50, 700 + font.GetDescent() / 1000f * 12, 50 + width, 700 + font.GetAscent() / 1000f * 12);

var redact = page.CreateRedactAnnotation(area, "Remove the account number");
redact.SetInteriorGrayColor(0);                                               // black box once applied
redact.SetOverlayText("REDACTED", bold, 10, HpdfTextAlignment.Center);
redact.SetOverlayTextRgbColor(1f, 1f, 1f);                                    // white text on it

pdf.SaveToFile("redaction.pdf");
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
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Redact` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetQuadPoints(params HpdfRect[])](#method-setquadpoints) | Sets the areas to redact as rectangles (`/QuadPoints`) |
| [SetQuadPoints(HpdfPoint, HpdfPoint, HpdfPoint, HpdfPoint)](#method-setquadpoints-1) | Sets the area to redact as one quadrilateral |
| [SetInteriorRgbColor()](#method-setinteriorrgbcolor) | Sets the RGB color that fills the area once it is redacted (`/IC`) |
| [SetInteriorCmykColor()](#method-setinteriorcmykcolor) | The same in CMYK |
| [SetInteriorGrayColor()](#method-setinteriorgraycolor) | The same as a gray level |
| [SetInteriorTransparent()](#method-setinteriortransparent) | Leaves the redacted area unfilled (an empty `/IC`) |
| [SetOverlayText()](#method-setoverlaytext) | Sets the text shown in the redacted area (`/OverlayText`, `/DA`, `/Q`, `/Repeat`) |
| [SetOverlayTextRgbColor()](#method-setoverlaytextrgbcolor) | Sets the color of the overlay text |
| [RemoveOverlayText()](#method-removeoverlaytext) | Removes the overlay text |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the color of the outline (`/C`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); the generated outline does not use it |

---

## Detailed Descriptions

#### Method: *SetQuadPoints()*

**Declaration:**
```csharp
public void SetQuadPoints(params HpdfRect[] areas)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| areas | `HpdfRect[]` | The areas to redact, for example one rectangle per line of text; at least one. Reversed edges are swapped |

**Return Value:**
`void`

**Description:**
Writes the areas as `/QuadPoints`, four corners per area, in the order viewers read them (upper left, upper right,
lower left, lower right; the same order as text markup annotations). With quadrilaterals, the areas are redacted
instead of the annotation rectangle, and the appearance outlines each of them. A later call replaces them.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `areas` is `null` or empty, or a coordinate is NaN or infinite |

**Example:**
```csharp
// Two lines of a paragraph
redact.SetQuadPoints(new HpdfRect(50, 600, 400, 614), new HpdfRect(50, 583, 220, 597));
```

---

#### Method: *SetQuadPoints()*

**Declaration:**
```csharp
public void SetQuadPoints(HpdfPoint leftBottom, HpdfPoint rightBottom, HpdfPoint leftTop, HpdfPoint rightTop)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| leftBottom, rightBottom | `HpdfPoint` | The corners at the bottom edge |
| leftTop, rightTop | `HpdfPoint` | The corners at the top edge |

**Return Value:**
`void`

**Description:**
Sets one quadrilateral, for an area that is not horizontal, such as rotated text. It replaces earlier
quadrilaterals.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

**Example:**
```csharp
redact.SetQuadPoints(new HpdfPoint(100, 400), new HpdfPoint(240, 480), new HpdfPoint(93, 412), new HpdfPoint(233, 492));
```

---

#### Method: *SetInteriorRgbColor()*

**Declaration:**
```csharp
public void SetInteriorRgbColor(float r, float g, float b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r, g, b | `float` | The red, green and blue components, from 0 to 1 |

**Return Value:**
`void`

**Description:**
Writes `/IC [r g b]`, the color that fills the area once a viewer has applied the redaction. It is not used by the
appearance before that. Without an interior color, the viewer that applies the redaction decides how the area looks.
A later call of any of the interior setters replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is below 0, above 1 or NaN |

**Example:**
```csharp
redact.SetInteriorRgbColor(0.2f, 0.2f, 0.2f);
```

---

#### Method: *SetInteriorCmykColor()*

**Declaration:**
```csharp
public void SetInteriorCmykColor(float c, float m, float y, float k)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| c, m, y, k | `float` | The cyan, magenta, yellow and black components, from 0 to 1 |

**Return Value:**
`void`

**Description:**
The same as [SetInteriorRgbColor()](#method-setinteriorrgbcolor) in CMYK: `/IC [c m y k]`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is below 0, above 1 or NaN |

---

#### Method: *SetInteriorGrayColor()*

**Declaration:**
```csharp
public void SetInteriorGrayColor(float gray)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| gray | `float` | The gray level, from 0 (black) to 1 (white) |

**Return Value:**
`void`

**Description:**
The same as [SetInteriorRgbColor()](#method-setinteriorrgbcolor) as a gray level: `/IC [gray]`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `gray` is below 0, above 1 or NaN |

**Example:**
```csharp
redact.SetInteriorGrayColor(0);   // black
```

---

#### Method: *SetInteriorTransparent()*

**Declaration:**
```csharp
public void SetInteriorTransparent()
```

**Return Value:**
`void`

**Description:**
Writes an empty `/IC []`: the content is removed and the area is left unfilled, so the page background shows through.

---

#### Method: *SetOverlayText()*

**Declaration:**
```csharp
public void SetOverlayText(string text, HpdfFont font, float fontSize,
    HpdfTextAlignment alignment = HpdfTextAlignment.Left, bool repeat = false)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| text | `string` | The text shown in the redacted area, such as "REDACTED"; may be empty |
| font | `HpdfFont` | The font of the text: any font of the document of horizontal writing |
| fontSize | `float` | The font size, positive |
| alignment | `HpdfTextAlignment` | `Left` (default), `Center` or `Right` |
| repeat | `bool` | Whether the text is repeated to fill the area |

**Return Value:**
`void`

**Description:**
Writes the text to show in the area once the redaction is applied: `/OverlayText (text)`, the default appearance string
`/DA` with the font's resource name, the size and the overlay text color (for example `/DA (/F2 10 Tf 1 0 0 rg)`),
the alignment `/Q` (0, 1 or 2) and `/Repeat true` when `repeat` is set. The overlay text is not part of the
appearance before the redaction is applied; it is drawn by the viewer that applies it.

When the document has an AcroForm (`HpdfDocument.GetOrCreateAcroForm()`), the font is added to its default resources
`/DR` when the document is saved, so the viewer finds the font that `/DA` names; no form is created for it. If
another font of the form or of another annotation has the same resource name, saving throws `HpdfException`
(`InvalidOperation`). A later call replaces the text, the font, the alignment and the repeat flag.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` or `font` is `null`, or the font is of vertical writing |
| `HpdfException` (`InvalidParameter`) | `fontSize` is not a positive number |
| `HpdfException` (`InvalidParameter`) | `alignment` is `Justify` or not an `HpdfTextAlignment` value |

**Example:**
```csharp
redact.SetOverlayText("CONFIDENTIAL", boldFont, 8, HpdfTextAlignment.Center, repeat: true);
```

---

#### Method: *SetOverlayTextRgbColor()*

**Declaration:**
```csharp
public void SetOverlayTextRgbColor(float r, float g, float b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r, g, b | `float` | The red, green and blue components, from 0 to 1 |

**Return Value:**
`void`

**Description:**
Sets the color of the overlay text, written in `/DA`; red until it is called. It can be called before or after
[SetOverlayText()](#method-setoverlaytext): the color is kept for a later overlay text.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is below 0, above 1 or NaN |

**Example:**
```csharp
redact.SetOverlayTextRgbColor(1f, 1f, 1f);   // white text on a dark fill
```

---

#### Method: *RemoveOverlayText()*

**Declaration:**
```csharp
public void RemoveOverlayText()
```

**Return Value:**
`void`

**Description:**
Removes `/OverlayText`, `/DA`, `/Q` and `/Repeat`; the font is no longer added to the form's default resources.

---

## Related Types

- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up, replies
- [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md) - the same quadrilaterals for marked text
- [HpdfTextAlignment](../types/HpdfTextAlignment.md) - the alignment of the overlay text
- [HpdfAcroForm](../forms/HpdfAcroForm.md) - its `/DR` receives the overlay font
- [HpdfPage](../core/HpdfPage.md) - `CreateRedactAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
