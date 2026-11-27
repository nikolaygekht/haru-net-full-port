# HpdfInkAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfInkAnnotation` is an ink annotation (PDF 1.3; not in the C library): freehand strokes, as made with a pen tool,
each a path of points (`/InkList`). Use it for handwritten marks such as a check mark, a scribble or a signature-like
line. Its text is shown in the pop-up window of the annotation.

---

## Getting an Instance

Create ink annotations with the page they are on:

```csharp
public HpdfInkAnnotation CreateInkAnnotation(HpdfPoint[][] strokes, string text)
```

| Name | Type | Description |
|------|------|-------------|
| strokes | `HpdfPoint[][]` | At least one stroke; every stroke is a path of at least one point in page coordinates. A stroke of one point is a dot. The arrays are copied |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |

The method creates the annotation (`/Subtype /Ink`) and adds it to the page's `/Annots` array. The strokes are red and
1 point wide until they are changed, and the annotation has the Print flag (`/F 4`). Creating one raises the document
version to PDF 1.3. Ink annotations are allowed in PDF/A-1.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | `strokes` is `null` or empty, or a stroke is `null` or empty |
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)): every stroke is a line through its points in
the annotation color ([SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) and the other color setters), as wide as
the border ([SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle), 1 point until it is called), with round ends
and joins, dashed for `HpdfBorderStyle.Dashed`. The points are joined by straight lines; pass enough points for a
smooth curve. With width 0 or [SetNoColor()](HpdfAnnotation.md#method-setnocolor) nothing is drawn.

The annotation rectangle is the bounding box of all points, grown by half the border width plus 1 point on every
side; it is computed when the strokes are set and again when the document is saved.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A zigzag and a short stroke in green, 2.5 points wide
var ink = page.CreateInkAnnotation(new[]
{
    new HpdfPoint[] { new(60, 500), new(75, 540), new(90, 505), new(105, 545), new(120, 510), new(135, 548) },
    new HpdfPoint[] { new(150, 515), new(175, 530), new(195, 512) }
}, "Freehand strokes");
ink.SetRgbColor(0f, 0.5f, 0f);
ink.SetBorderStyle(HpdfBorderStyle.Solid, 2.5f);

// A check mark and a dot
var check = page.CreateInkAnnotation(new[]
{
    new HpdfPoint[] { new(300, 520), new(310, 505), new(335, 545) },
    new HpdfPoint[] { new(345, 505) }
}, "Done");
check.SetBorderStyle(HpdfBorderStyle.Solid, 3);

pdf.SaveToFile("ink.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Strokes](#property-strokes) | `HpdfPoint[][]` | A copy of the strokes |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Ink` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetInkList()](#method-setinklist) | Replaces the strokes (`/InkList`) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the width and dash of the strokes (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the color of the strokes (`/C`) (inherited) |

---

## Detailed Descriptions

#### Property: *Strokes*

**Declaration:**
```csharp
public HpdfPoint[][] Strokes { get; }
```

**Description:**
The strokes given at creation or by the last [SetInkList()](#method-setinklist) call. Every call returns new copies of
the arrays: changing them does not change the annotation.

**Example:**
```csharp
Console.WriteLine($"{ink.Strokes.Length} strokes, {ink.Strokes.Sum(s => s.Length)} points");
```

---

#### Method: *SetInkList()*

**Declaration:**
```csharp
public void SetInkList(params HpdfPoint[][] strokes)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| strokes | `HpdfPoint[][]` | The new strokes: at least one, each with at least one point in page coordinates. The arrays are copied |

**Return Value:**
`void`

**Description:**
Replaces the strokes and writes them as `/InkList [[x1 y1 x2 y2 ...] [...]]`; the annotation rectangle is computed
from the new points.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `strokes` is `null` or empty, or a stroke is `null` or empty |
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

**Example:**
```csharp
ink.SetInkList(new HpdfPoint[] { new(100, 100), new(150, 120), new(200, 100) });
```

---

## Related Types

- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up, replies
- [HpdfPolyLineAnnotation](HpdfPolyLineAnnotation.md) - straight segments with line endings
- [HpdfPoint](../types/HpdfPoint.md) - the points
- [HpdfPage](../core/HpdfPage.md) - `CreateInkAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
