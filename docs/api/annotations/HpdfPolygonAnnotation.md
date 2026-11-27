# HpdfPolygonAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfPolygonalAnnotation](HpdfPolygonalAnnotation.md) → [HpdfGeometricAnnotation](HpdfGeometricAnnotation.md)
→ [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfPolygonAnnotation` is a polygon annotation (PDF 1.5; not in the C library): a closed shape through any number of
vertices, used to mark an area of the page that is not a rectangle or an ellipse. The outline is drawn in the
annotation color, the inside can be filled with an interior color, and the border can be drawn as a cloud, also around
a concave shape. Its text is shown in the pop-up window of the annotation.

---

## Getting an Instance

Create polygons with the page they are on:

```csharp
public HpdfPolygonAnnotation CreatePolygonAnnotation(HpdfPoint[] vertices, string text)
```

| Name | Type | Description |
|------|------|-------------|
| vertices | `HpdfPoint[]` | At least three vertices in page coordinates; the last one is connected to the first |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |

The method creates the annotation (`/Subtype /Polygon`, `/Vertices`) and adds it to the page's `/Annots` array. The
outline is red and 1 point wide until it is changed, the inside is not filled, and the annotation has the Print flag
(`/F 4`). Creating one raises the document version to PDF 1.5. PDF/A-1 does not allow polygon annotations: saving a
PDF/A document with one throws `HpdfException` (`InvalidOperation`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | `vertices` is `null` or has fewer than three points |
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)): the closed path through the vertices,
stroked in the annotation color with the border width and dash, and filled with the interior color, if one is set.
The joins are round. With width 0 or [SetNoColor()](HpdfAnnotation.md#method-setnocolor) no outline is drawn; without
an interior color nothing is filled. The annotation rectangle is computed from the vertices (see
[The rectangle and the appearance](HpdfPolygonalAnnotation.md#the-rectangle-and-the-appearance)).

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A filled triangle
var triangle = page.CreatePolygonAnnotation(new HpdfPoint[] { new(60, 620), new(180, 620), new(120, 715) },
    "Check this area");
triangle.SetInteriorRgbColor(1f, 0.9f, 0.8f);

// A blue cloud around an L-shaped area
var cloud = page.CreatePolygonAnnotation(new HpdfPoint[]
{
    new(230, 620), new(350, 620), new(350, 660), new(280, 660), new(280, 715), new(230, 715)
}, "Rewrite these paragraphs");
cloud.SetCloudEffect(1);
cloud.SetIntent(HpdfPolygonIntent.Cloud);
cloud.SetRgbColor(0f, 0.3f, 0.8f);

pdf.SaveToFile("polygons.pdf");
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
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Polygon` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetCloudEffect()](#method-setcloudeffect) | Draws the border as a cloud (`/BE`) |
| [SetIntent()](#method-setintent) | Sets what the polygon is for: a cloud or a dimension polygon (`/IT`) |
| [SetVertices()](HpdfPolygonalAnnotation.md#method-setvertices) | Replaces the vertices (`/Vertices`) (inherited) |
| [SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor), [SetInteriorCmykColor()](HpdfGeometricAnnotation.md#method-setinteriorcmykcolor), [SetInteriorGrayColor()](HpdfGeometricAnnotation.md#method-setinteriorgraycolor), [SetInteriorTransparent()](HpdfGeometricAnnotation.md#method-setinteriortransparent) | Set or remove the fill color (`/IC`) (inherited) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the width and dash of the outline (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the outline color (`/C`) (inherited) |

---

## Detailed Descriptions

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
Draws the outline as a cloud: a row of overlapping round bumps along the edges of the polygon, one at every vertex and
the others evenly spaced. Writes the border effect `/BE << /S /C /I intensity >>`; intensity 0 removes `/BE` and draws
the plain outline again. The bumps have a radius of 4.75 points per unit of intensity and lie outside the polygon,
also at concave corners; the cloud is drawn with the color, width and dash of a plain outline, and an interior color
fills it.

Unlike a cloudy square or circle ([HpdfShapeAnnotation](HpdfShapeAnnotation.md#method-setcloudeffect)), whose cloud
stays inside its rectangle, the polygon keeps its vertices: when the document is saved, the annotation rectangle grows
by the radius of the bumps, and no `/RD` is written. The document version stays at 1.5, the version of the polygon.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intensity` is not 0, 1 or 2 |

**Example:**
```csharp
polygon.SetCloudEffect(2);
```

---

#### Method: *SetIntent()*

**Declaration:**
```csharp
public void SetIntent(HpdfPolygonIntent intent)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| intent | `HpdfPolygonIntent` | `Cloud` or `Dimension` (see [HpdfPolygonIntent](HpdfPolygonIntent.md)) |

**Return Value:**
`void`

**Description:**
Writes the intent `/IT` (`/PolygonCloud` or `/PolygonDimension`) and raises the document version to PDF 1.6. The
intent tells viewers which tool made the annotation; it does not change the appearance, so a cloud also needs
[SetCloudEffect()](#method-setcloudeffect). A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intent` is not an `HpdfPolygonIntent` value |

**Example:**
```csharp
polygon.SetIntent(HpdfPolygonIntent.Dimension);
```

---

## Related Types

- [HpdfPolygonalAnnotation](HpdfPolygonalAnnotation.md) - base class: vertices
- [HpdfPolyLineAnnotation](HpdfPolyLineAnnotation.md) - the open shape
- [HpdfPolygonIntent](HpdfPolygonIntent.md) - the intents
- [HpdfShapeAnnotation](HpdfShapeAnnotation.md) - rectangles and ellipses
- [HpdfPage](../core/HpdfPage.md) - `CreatePolygonAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
