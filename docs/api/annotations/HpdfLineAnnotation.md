# HpdfLineAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfGeometricAnnotation](HpdfGeometricAnnotation.md) → [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md)
→ [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfLineAnnotation` is a line annotation (PDF 1.3): a straight line between two points, with optional line endings
(arrowheads, squares, circles, ...), leader lines that make it a dimension line, and a caption that shows the text of
the annotation on the line. Use it for arrows that point at something in a review and for measurements.

---

## Getting an Instance

Create lines with the page they are on:

```csharp
public HpdfLineAnnotation CreateLineAnnotation(HpdfPoint start, HpdfPoint end, string text)
```

| Name | Type | Description |
|------|------|-------------|
| start | `HpdfPoint` | The start point of the line, in page coordinates |
| end | `HpdfPoint` | The end point |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window and as the caption; may be empty |

The method creates the annotation (`/Subtype /Line`, the points as `/L [x1 y1 x2 y2]`) and adds it to the page's
`/Annots` array. The line is red (`/C [1 0 0]`), 1 point wide, has no line endings and the Print flag (`/F 4`).
Creating one raises the document version to PDF 1.3.

**The annotation rectangle follows from the geometry.** When the document is saved, `/Rect` is computed from the
line, its leader lines, its endings and its caption, with room for the stroke, so it always encloses what is shown.
(The C library writes `/Rect [0 0 0 0]` for a line.)

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `start` or `end` is NaN or infinite |

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)):

- The line, its leader lines and the outlines of its endings are drawn in the annotation color with the border width
  of [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle); `HpdfBorderStyle.Dashed` makes the line and the
  leader lines dashed, the other styles draw it solid. Line endings are always drawn solid.
- Closed endings (`Square`, `Circle`, `Diamond`, `ClosedArrow`, `RClosedArrow`) are filled with the interior color
  ([SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor)); without one they are outlines.
- The size of the endings grows with the line width: 6 + 3 × width points (9 points for a line 1 point wide). The
  sides of an arrowhead are as long as the size, at 30 degrees to the line; a square, circle or diamond is two thirds
  of the size across (see [HpdfLineEndingStyle](HpdfLineEndingStyle.md)).
- The line stops at the edge of a square, circle or diamond ending and at the base of a closed arrowhead, so it does
  not cross an unfilled ending.
- With [SetNoColor()](HpdfAnnotation.md#method-setnocolor) or width 0 only the filled endings and the caption are
  drawn.

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

// The object that is measured
page.Rectangle(100, 600, 300, 40);
page.Stroke();

// A dimension line 20 points above the top edge, with arrows and its length as caption
var dimension = page.CreateLineAnnotation(new HpdfPoint(100, 640), new HpdfPoint(400, 640), "300 pt");
dimension.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow, HpdfLineEndingStyle.ClosedArrow);
dimension.SetInteriorRgbColor(1f, 0f, 0f);
dimension.SetLeader(20, 5);
dimension.SetCaption(font, 10);
dimension.SetIntent(HpdfLineIntent.Dimension);

// A blue arrow with a caption on top: drawn from left to right, so that the caption is upright,
// with the arrowhead at the start point
var arrow = page.CreateLineAnnotation(new HpdfPoint(350, 590), new HpdfPoint(450, 500), "Too narrow");
arrow.SetRgbColor(0f, 0f, 0.7f);
arrow.SetBorderStyle(HpdfBorderStyle.Solid, 1.5f);
arrow.SetLineEndingStyle(HpdfLineEndingStyle.OpenArrow, HpdfLineEndingStyle.None);
arrow.SetCaption(font, 9, HpdfLineCaptionPosition.Top);

pdf.SaveToFile("lines.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation that shows the text, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Line` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetPosition()](#method-setposition) | Sets the start and end points (`/L`) |
| [SetLineEndingStyle()](#method-setlineendingstyle) | Sets the endings at the start and the end (`/LE`) |
| [SetLeader()](#method-setleader) | Moves the line away from its points and draws leader lines (`/LL`, `/LLE`, `/LLO`) |
| [SetCaption()](#method-setcaption) | Shows the text of the annotation on the line (`/Cap`, `/CP`, `/CO`) |
| [RemoveCaption()](#method-removecaption) | Removes the caption |
| [SetIntent()](#method-setintent) | Sets what the line is for: an arrow or a dimension line (`/IT`) |
| [SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor), [SetInteriorCmykColor()](HpdfGeometricAnnotation.md#method-setinteriorcmykcolor), [SetInteriorGrayColor()](HpdfGeometricAnnotation.md#method-setinteriorgraycolor), [SetInteriorTransparent()](HpdfGeometricAnnotation.md#method-setinteriortransparent) | Fill color of the closed endings (`/IC`) (inherited) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the line width and dash (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set the line color (`/C`) (inherited) |

---

## Detailed Descriptions

#### Method: *SetPosition()*

**Declaration:**
```csharp
public void SetPosition(HpdfPoint start, HpdfPoint end)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| start | `HpdfPoint` | The start point |
| end | `HpdfPoint` | The end point |

**Return Value:**
`void`

**Description:**
Replaces the points given at creation and writes them as `/L [x1 y1 x2 y2]`. The annotation rectangle follows when
the document is saved. The direction of the line matters: the start ending is drawn at `start`, the end ending at
`end`, and leader lines and caption offsets are measured to the left of the direction from `start` to `end`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

**Example:**
```csharp
line.SetPosition(new HpdfPoint(100, 300), new HpdfPoint(300, 400));
```

---

#### Method: *SetLineEndingStyle()*

**Declaration:**
```csharp
public void SetLineEndingStyle(HpdfLineEndingStyle start, HpdfLineEndingStyle end)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| start | `HpdfLineEndingStyle` | The ending at the start point |
| end | `HpdfLineEndingStyle` | The ending at the end point |

**Return Value:**
`void`

**Description:**
Writes the line endings `/LE [/start /end]` (see [HpdfLineEndingStyle](HpdfLineEndingStyle.md)) and raises the
document version to PDF 1.4. Without a call both ends are plain (`None`). Closed endings are filled with the interior
color. A later call replaces both endings. Both values are checked before anything changes.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `start` or `end` is not an `HpdfLineEndingStyle` value |

**Example:**
```csharp
line.SetLineEndingStyle(HpdfLineEndingStyle.Circle, HpdfLineEndingStyle.OpenArrow);
```

---

#### Method: *SetLeader()*

**Declaration:**
```csharp
public void SetLeader(float length, float extension = 0, float offset = 0)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| length | `float` | The length of the leader lines: positive moves the line to the left of its direction (up for a line from left to right), negative to the right; 0 removes the leader lines |
| extension | `float` | How far the leader lines go beyond the line (non-negative) |
| offset | `float` | The gap between the points and the start of the leader lines (non-negative) |

**Return Value:**
`void`

**Description:**
Makes the annotation a dimension line: the line is drawn `length` points away from its points, parallel to them,
and a leader line is drawn from each point (after the gap `offset`) to the line and `extension` points beyond it.
With a negative length the line, the extension and the offset go to the other side.

Writes `/LL length`, `/LLE` when the extension is not 0 and `/LLO` when the offset is not 0. Raises the document
version to PDF 1.6, or 1.7 when the offset is not 0. A later call replaces all three values; a length of 0 removes
the three entries (and raises no version).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `length` is NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `extension` or `offset` is negative, NaN or infinite |

**Example:**
```csharp
// The points are on the object; the line is 15 points above it, the leader lines go 4 points beyond
line.SetLeader(15, 4, 2);
```

---

#### Method: *SetCaption()*

**Declaration:**
```csharp
public void SetCaption(HpdfFont font, float fontSize,
    HpdfLineCaptionPosition position = HpdfLineCaptionPosition.Inline,
    float horizontalOffset = 0, float verticalOffset = 0)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| font | `HpdfFont` | The font of the caption: any font of the document (standard, TrueType, Type1, CID) of horizontal writing |
| fontSize | `float` | The font size, positive |
| position | `HpdfLineCaptionPosition` | `Inline`: centered inside the line, which is broken around it; `Top`: on top of the line (see [HpdfLineCaptionPosition](HpdfLineCaptionPosition.md)) |
| horizontalOffset | `float` | Shift of the caption along the line from its center |
| verticalOffset | `float` | Shift of the caption perpendicular to the line; positive moves it to the left of the line's direction |

**Return Value:**
`void`

**Description:**
Shows the text of the annotation (the `text` given to `CreateLineAnnotation()`) as a caption on the line. The
caption is one line of text: line breaks in the text are replaced by spaces. It is drawn along the line (rotated with
it), in the annotation color (black when the color is transparent), with the given font; the font is added to the
resources of the appearance stream, and an embedded font includes the glyphs of the caption in its subset. Use a font
that can show the text, for example a CID font for Chinese or Japanese.

An inline caption breaks the line, with a gap of 2 points on each side, if it fits between the ends of the line;
otherwise the line is drawn whole under the caption. A caption on top stands 2 points above the line.

Writes `/Cap true` (PDF 1.6), `/CP /Top` for a caption on top (PDF 1.7; removed for `Inline`, the default), and
`/CO [horizontal vertical]` when an offset is not 0 (PDF 1.7). A later call replaces the caption settings.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `font` is `null`, or a font of vertical writing |
| `HpdfException` (`InvalidParameter`) | `fontSize` is not a positive number |
| `HpdfException` (`InvalidParameter`) | `position` is not an `HpdfLineCaptionPosition` value |
| `HpdfException` (`InvalidParameter`) | An offset is NaN or infinite |

**Example:**
```csharp
line.SetCaption(font, 9, HpdfLineCaptionPosition.Top, 0, 3);   // on top, 3 points higher
```

---

#### Method: *RemoveCaption()*

**Declaration:**
```csharp
public void RemoveCaption()
```

**Return Value:**
`void`

**Description:**
Removes the caption: `/Cap`, `/CP` and `/CO` are removed and the line is drawn without text. The text stays in
`/Contents` and is still shown in the pop-up window.

**Example:**
```csharp
line.RemoveCaption();
```

---

#### Method: *SetIntent()*

**Declaration:**
```csharp
public void SetIntent(HpdfLineIntent intent)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| intent | `HpdfLineIntent` | `Arrow` or `Dimension` (see [HpdfLineIntent](HpdfLineIntent.md)) |

**Return Value:**
`void`

**Description:**
Writes the intent `/IT /LineArrow` or `/IT /LineDimension` and raises the document version to PDF 1.6. The intent
tells viewers what the line is for (some show dimension lines with their measuring tools); it does not change the
generated appearance. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intent` is not an `HpdfLineIntent` value |

**Example:**
```csharp
line.SetIntent(HpdfLineIntent.Arrow);
```

---

## Related Types

- [HpdfLineEndingStyle](HpdfLineEndingStyle.md), [HpdfLineCaptionPosition](HpdfLineCaptionPosition.md),
  [HpdfLineIntent](HpdfLineIntent.md)
- [HpdfGeometricAnnotation](HpdfGeometricAnnotation.md) - base class: interior color
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - author, subject, date, opacity, pop-up
- [HpdfAnnotation](HpdfAnnotation.md) - color, border style, flags
- [HpdfPoint](../types/HpdfPoint.md) - the points of the line
- [HpdfPage](../core/HpdfPage.md) - `CreateLineAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
