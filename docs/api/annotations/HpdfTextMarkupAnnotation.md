# HpdfTextMarkupAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfTextMarkupAnnotation` marks text that is already on the page: a highlight, an underline, a squiggly underline or
a strikeout. The marked text is given as quadrilaterals, one per line of text, so one annotation can cover a passage of
several lines. Its own text (a comment on the passage) is shown in its pop-up window.

---

## Getting an Instance

Create text markup with the page it is on; the four methods take the same arguments:

```csharp
public HpdfTextMarkupAnnotation CreateHighlightAnnotation(HpdfRect rect, string text)
public HpdfTextMarkupAnnotation CreateUnderlineAnnotation(HpdfRect rect, string text)
public HpdfTextMarkupAnnotation CreateSquigglyAnnotation(HpdfRect rect, string text)
public HpdfTextMarkupAnnotation CreateStrikeOutAnnotation(HpdfRect rect, string text)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The rectangle of the marked text (one line); for several lines call [SetQuadPoints()](#method-setquadpoints) afterwards. Reversed edges are swapped |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |

The methods create the annotation (`/Subtype /Highlight`, `/Underline`, `/Squiggly` or `/StrikeOut`) with the
rectangle as its only quadrilateral and add it to the page's `/Annots` array. A highlight is yellow (`/C [1 1 0]`),
the others are red (`/C [1 0 0]`) until another color is set; all have the Print flag (`/F 4`). Creating one raises
the document version to PDF 1.3, a squiggly underline and a highlight (for the blend mode of its appearance) to
PDF 1.4.

The rectangle of a line of text reaches from the bottom of the descenders to the top of the ascenders. With a font
and the position of the text it is:

```csharp
float width = font.MeasureText(text, size);
var rect = new HpdfRect(x, y + font.GetDescent() / 1000f * size, x + width, y + font.GetAscent() / 1000f * size);
```

**The annotation rectangle follows from the quadrilaterals.** When the document is saved, `/Rect` is set to the
smallest rectangle that encloses all quadrilaterals, with room for the squiggles and the outline of a PDF/A highlight.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)), for every quadrilateral, in the annotation
color:

| Type | Drawn as |
|------|----------|
| Highlight | The quadrilateral filled with the Multiply blend mode, so the text under it stays readable. In a PDF/A document, which allows no blend modes, an outline 1 point wide |
| Underline | A line parallel to the bottom edge at 1/7 of the height, as thick as 1/14 of the height (at least 0.5 point) |
| StrikeOut | The same line at half the height |
| Squiggly | A zigzag just above the bottom edge, between 1/28 and 1/7 of the height above it (at least between 0.5 and 2 points) |

The height is the distance between the bottom and the top edge, so the lines follow the size of the text, also in
a rotated quadrilateral. The border style has no effect on text markup. With
[SetNoColor()](HpdfAnnotation.md#method-setnocolor) nothing is drawn.

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

const float size = 12;
string[] lines =
{
    "Text markup annotations mark text that is already on the page.",
    "Each line of the marked text is one quadrilateral."
};

// Draw the text and remember the rectangle of every line
var rects = new HpdfRect[lines.Length];
page.SetFontAndSize(font, size);
for (int i = 0; i < lines.Length; i++)
{
    float x = 50, y = 700 - i * 16;
    page.TextOut(x, y, lines[i]);
    rects[i] = new HpdfRect(x, y + font.GetDescent() / 1000f * size,
        x + font.MeasureText(lines[i], size), y + font.GetAscent() / 1000f * size);
}

// One highlight over both lines
var highlight = page.CreateHighlightAnnotation(rects[0], "Important for the review");
highlight.SetQuadPoints(rects);
highlight.SetTitle("Reviewer");

// A strikeout of the second line only, in blue
var strikeOut = page.CreateStrikeOutAnnotation(rects[1], "Delete this sentence");
strikeOut.SetRgbColor(0f, 0.3f, 0.9f);

pdf.SaveToFile("textmarkup.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation that shows the text, or `null` (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | `Highlight`, `Underline`, `Squiggly` or `StrikeOut` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetQuadPoints(params HpdfRect[])](#method-setquadpoints) | Sets the marked text as rectangles, one per line (`/QuadPoints`) |
| [SetQuadPoints(HpdfPoint, HpdfPoint, HpdfPoint, HpdfPoint)](#method-setquadpoints-1) | Sets the marked text as one quadrilateral, for text that is not horizontal |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set the color of the markup (`/C`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); has no effect on text markup |

---

## Detailed Descriptions

#### Method: *SetQuadPoints()*

**Declaration:**
```csharp
public void SetQuadPoints(params HpdfRect[] lines)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| lines | `HpdfRect[]` | The rectangles of the marked lines of text, from the bottom of the descenders to the top of the ascenders; at least one |

**Return Value:**
`void`

**Description:**
Replaces the marked text with the given rectangles, one quadrilateral each, and writes them as `/QuadPoints`. Use one
rectangle per line of text: a passage that starts in the middle of one line and ends in the middle of another is
three rectangles (the end of the first line, the full lines between, the start of the last line) or two when it
spans two lines. Reversed edges are swapped.

`/QuadPoints` lists 8 numbers per quadrilateral in the order that viewers (Adobe Acrobat, pdf.js, PDFium) read:
upper-left, upper-right, lower-left, lower-right corner. This is not the counterclockwise order described in the
text of the PDF specification, which viewers do not follow.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `lines` is `null` or empty |
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

The rectangles are checked before anything changes.

**Example:**
```csharp
highlight.SetQuadPoints(
    new HpdfRect(200, 397, 545, 411),    // the end of the first line
    new HpdfRect(50, 380, 545, 394),     // a full line
    new HpdfRect(50, 363, 180, 377));    // the start of the last line
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
| leftBottom | `HpdfPoint` | The start of the bottom edge (the baseline side) of the text |
| rightBottom | `HpdfPoint` | The end of the bottom edge |
| leftTop | `HpdfPoint` | The start of the top edge |
| rightTop | `HpdfPoint` | The end of the top edge |

**Return Value:**
`void`

**Description:**
Replaces the marked text with one quadrilateral, for text that is rotated or slanted: the bottom edge runs from
`leftBottom` to `rightBottom` in the direction of the text, the top edge from `leftTop` to `rightTop`. Underlines,
strikeouts and squiggles are drawn parallel to the bottom edge, on the side of the top edge.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

**Example:**
```csharp
// Text drawn at 30 degrees from (100, 300), 120 points long and 14 points high
double angle = Math.PI / 6;
float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
var lb = new HpdfPoint(100, 300);
var rb = new HpdfPoint(100 + 120 * cos, 300 + 120 * sin);
var lt = new HpdfPoint(100 - 14 * sin, 300 + 14 * cos);
var rt = new HpdfPoint(rb.X - 14 * sin, rb.Y + 14 * cos);
underline.SetQuadPoints(lb, rb, lt, rt);
```

---

## Related Types

- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up
- [HpdfAnnotation](HpdfAnnotation.md) - color, flags
- [HpdfRect](../types/HpdfRect.md), [HpdfPoint](../types/HpdfPoint.md)
- [HpdfPage](../core/HpdfPage.md) - `CreateHighlightAnnotation()`, `CreateUnderlineAnnotation()`,
  `CreateSquigglyAnnotation()`, `CreateStrikeOutAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
