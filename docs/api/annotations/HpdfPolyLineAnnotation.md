# HpdfPolyLineAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfPolygonalAnnotation](HpdfPolygonalAnnotation.md) → [HpdfGeometricAnnotation](HpdfGeometricAnnotation.md)
→ [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfPolyLineAnnotation` is a polyline annotation (PDF 1.5; not in the C library): connected straight lines through
two or more vertices, with optional line endings at the first and at the last vertex. Use it for a path that bends,
such as an arrow around an obstacle or a measured path. Its text is shown in the pop-up window of the annotation.

---

## Getting an Instance

Create polylines with the page they are on:

```csharp
public HpdfPolyLineAnnotation CreatePolyLineAnnotation(HpdfPoint[] vertices, string text)
```

| Name | Type | Description |
|------|------|-------------|
| vertices | `HpdfPoint[]` | At least two vertices in page coordinates, from the first to the last |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |

The method creates the annotation (`/Subtype /PolyLine`, `/Vertices`) and adds it to the page's `/Annots` array. The
line is red and 1 point wide until it is changed, has no line endings, and the annotation has the Print flag (`/F 4`).
Creating one raises the document version to PDF 1.5. PDF/A-1 does not allow polyline annotations: saving a PDF/A
document with one throws `HpdfException` (`InvalidOperation`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | `vertices` is `null` or has fewer than two points |
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)):

- The line is stroked in the annotation color with the border width and dash, with round joins; it is never filled.
- The line endings are drawn as on a line annotation ([HpdfLineEndingStyle](HpdfLineEndingStyle.md)): their size
  grows with the border width, closed endings are filled with the interior color, and the line stops at the edge of a
  `Square`, `Circle` or `Diamond` ending and at the base of a `ClosedArrow`. Endings are drawn solid also on a dashed
  line.
- The direction of an ending is the direction of the first or the last segment.
- With width 0 or [SetNoColor()](HpdfAnnotation.md#method-setnocolor) no line is drawn, only the inside of closed
  endings, if an interior color is set.

The annotation rectangle is computed from the vertices and the size of the endings (see
[The rectangle and the appearance](HpdfPolygonalAnnotation.md#the-rectangle-and-the-appearance)).

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// An arrow that goes around a figure
var path = page.CreatePolyLineAnnotation(new HpdfPoint[] { new(400, 630), new(440, 700), new(490, 640), new(540, 710) },
    "Follow this path");
path.SetLineEndingStyle(HpdfLineEndingStyle.Circle, HpdfLineEndingStyle.ClosedArrow);
path.SetInteriorRgbColor(1f, 0f, 0f);

pdf.SaveToFile("polyline.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Vertices](HpdfPolygonalAnnotation.md#property-vertices) | `HpdfPoint[]` | A copy of the vertices (inherited) |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.PolyLine` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetLineEndingStyle()](#method-setlineendingstyle) | Sets the endings at the first and at the last vertex (`/LE`) |
| [SetIntent()](#method-setintent) | Marks the polyline as a dimension polyline (`/IT`) |
| [SetVertices()](HpdfPolygonalAnnotation.md#method-setvertices) | Replaces the vertices (`/Vertices`) (inherited) |
| [SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor), [SetInteriorCmykColor()](HpdfGeometricAnnotation.md#method-setinteriorcmykcolor), [SetInteriorGrayColor()](HpdfGeometricAnnotation.md#method-setinteriorgraycolor), [SetInteriorTransparent()](HpdfGeometricAnnotation.md#method-setinteriortransparent) | Set or remove the fill color of closed endings (`/IC`) (inherited) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the width and dash of the line (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the line color (`/C`) (inherited) |

---

## Detailed Descriptions

#### Method: *SetLineEndingStyle()*

**Declaration:**
```csharp
public void SetLineEndingStyle(HpdfLineEndingStyle start, HpdfLineEndingStyle end)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| start | `HpdfLineEndingStyle` | The ending at the first vertex |
| end | `HpdfLineEndingStyle` | The ending at the last vertex |

**Return Value:**
`void`

**Description:**
Sets the endings and writes them as `/LE [/start /end]`. Arrows point away from the line: an `OpenArrow` at the end
points beyond the last vertex. Closed endings (`Square`, `Circle`, `Diamond`, `ClosedArrow`, `RClosedArrow`) are
filled with the interior color. No version is raised beyond the 1.5 of the polyline. A later call replaces both
endings; `None, None` removes them from the drawing (`/LE [/None /None]` stays).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `start` or `end` is not an `HpdfLineEndingStyle` value |

**Example:**
```csharp
polyline.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.OpenArrow);
```

---

#### Method: *SetIntent()*

**Declaration:**
```csharp
public void SetIntent(HpdfPolyLineIntent intent)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| intent | `HpdfPolyLineIntent` | `Dimension`, the only intent of a polyline (see [HpdfPolyLineIntent](HpdfPolyLineIntent.md)) |

**Return Value:**
`void`

**Description:**
Writes `/IT /PolyLineDimension` and raises the document version to PDF 1.6. The intent does not change the
appearance.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intent` is not an `HpdfPolyLineIntent` value |

**Example:**
```csharp
polyline.SetIntent(HpdfPolyLineIntent.Dimension);
```

---

## Related Types

- [HpdfPolygonalAnnotation](HpdfPolygonalAnnotation.md) - base class: vertices
- [HpdfPolygonAnnotation](HpdfPolygonAnnotation.md) - the closed shape
- [HpdfLineEndingStyle](HpdfLineEndingStyle.md), [HpdfPolyLineIntent](HpdfPolyLineIntent.md)
- [HpdfLineAnnotation](HpdfLineAnnotation.md) - a single straight line with leader lines and a caption
- [HpdfPage](../core/HpdfPage.md) - `CreatePolyLineAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
