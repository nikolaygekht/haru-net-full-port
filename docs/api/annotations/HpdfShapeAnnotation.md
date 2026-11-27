# HpdfShapeAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfGeometricAnnotation](HpdfGeometricAnnotation.md) → [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md)
→ [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfShapeAnnotation` is a square or circle annotation (PDF 1.3): a rectangle or an ellipse drawn inside the
annotation rectangle, used to mark an area of the page in a review. The outline is drawn in the annotation color with
the border width and dash, the inside can be filled with an interior color, and the border can be drawn as a cloud.
Its text is shown in the pop-up window of the annotation.

---

## Getting an Instance

Create squares and circles with the page they are on:

```csharp
public HpdfShapeAnnotation CreateSquareAnnotation(HpdfRect rect, string text)
public HpdfShapeAnnotation CreateCircleAnnotation(HpdfRect rect, string text)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The annotation rectangle; the shape is drawn inside it. Reversed edges are swapped |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |

The method creates the annotation (`/Subtype /Square` or `/Subtype /Circle`) and adds it to the page's `/Annots`
array. The annotation is red (`/C [1 0 0]`) until another color is set, has no interior color (the inside is not
filled), and has the Print flag (`/F 4`). Creating one raises the document version to PDF 1.3.

A circle annotation is an ellipse inscribed in its rectangle; a square rectangle gives a circle.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |

### The appearance

The library draws the appearance stream (`/AP /N`, a Form XObject whose bounding box is the annotation rectangle)
when the document is saved, from the properties the annotation has then; the setters can be called in any order, and
saving again redraws it. Viewers show this appearance as it is, so the annotation looks the same in every viewer and
is printed as it is shown.

- The outline is drawn in the annotation color ([SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) and the
  other color setters) with the border width of [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle), 1 point
  until it is called. The stroke lies inside the rectangle: it is inset by half the border width.
- `HpdfBorderStyle.Dashed` draws a dashed outline with the dash and gap lengths of `SetBorderStyle()`. `Beveled`,
  `Inset` and `Underlined` are drawn solid.
- Width 0 or [SetNoColor()](HpdfAnnotation.md#method-setnocolor) draws no outline.
- An interior color ([SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor) and the other
  interior setters) fills the shape.
- [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) makes the whole shape translucent.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A red rectangle around an area of the page
var square = page.CreateSquareAnnotation(new HpdfRect(50, 650, 250, 750), "This table is out of date");
square.SetTitle("Reviewer");

// A filled, half-transparent ellipse with a thick blue border
var circle = page.CreateCircleAnnotation(new HpdfRect(300, 650, 500, 750), "Check this figure");
circle.SetRgbColor(0f, 0.3f, 0.8f);
circle.SetInteriorRgbColor(0.85f, 0.92f, 1f);
circle.SetBorderStyle(HpdfBorderStyle.Solid, 3);
circle.SetTransparency(0.6f);

// A cloud around a paragraph
var cloud = page.CreateSquareAnnotation(new HpdfRect(50, 450, 500, 600), "Rewrite this paragraph");
cloud.SetCloudEffect(1);

pdf.SaveToFile("shapes.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation that shows the text, or `null` (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | `Square` or `Circle` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetCloudEffect()](#method-setcloudeffect) | Draws the border as a cloud (`/BE`) |
| [SetRectDifference()](#method-setrectdifference) | Sets the distances from the edges of the rectangle to the shape (`/RD`) |
| [SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor) | Fills the shape with an RGB color (`/IC`) (inherited) |
| [SetInteriorCmykColor()](HpdfGeometricAnnotation.md#method-setinteriorcmykcolor) | Fills the shape with a CMYK color (`/IC`) (inherited) |
| [SetInteriorGrayColor()](HpdfGeometricAnnotation.md#method-setinteriorgraycolor) | Fills the shape with a gray level (`/IC`) (inherited) |
| [SetInteriorTransparent()](HpdfGeometricAnnotation.md#method-setinteriortransparent) | Leaves the shape unfilled (an empty `/IC`) (inherited) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle) | Sets the author shown in the title of the pop-up window (`/T`) (inherited) |
| [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject) | Sets the subject (`/Subj`) (inherited) |
| [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate) | Sets the creation date (`/CreationDate`) (inherited) |
| [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Sets the constant opacity (`/CA`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the width and dash of the outline (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) | Sets the outline color in RGB (`/C`) (inherited) |
| [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor) | Sets the outline color in CMYK (`/C`) (inherited) |
| [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor) | Sets the outline color as a gray level (`/C`) (inherited) |
| [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Draws no outline (an empty `/C`) (inherited) |

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
Draws the outline as a cloud: a row of overlapping round bumps along the edge of the rectangle or the ellipse.
Writes the border effect `/BE << /S /C /I intensity >>` and raises the document version to PDF 1.5; intensity 0
removes `/BE` and draws the plain outline again. The bumps have a radius of 4.75 points per unit of intensity, and
the outline is drawn with the color, width and dash of a plain outline; an interior color fills the cloud.

The cloud stays inside the annotation rectangle: unless [SetRectDifference()](#method-setrectdifference) was called,
the shape is the rectangle made smaller on every side by the radius of the bumps and half the border width, and
`/RD` is written with this distance, so viewers that redraw the annotation place the cloud the same way.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `intensity` is not 0, 1 or 2 |

**Example:**
```csharp
square.SetCloudEffect(2);    // large bumps
square.SetCloudEffect(0);    // back to a plain rectangle
```

---

#### Method: *SetRectDifference()*

**Declaration:**
```csharp
public void SetRectDifference(float left, float top, float right, float bottom)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | Distance from the left edge of the annotation rectangle to the shape |
| top | `float` | Distance from the top edge |
| right | `float` | Distance from the right edge |
| bottom | `float` | Distance from the bottom edge |

**Return Value:**
`void`

**Description:**
Draws the shape inside the annotation rectangle made smaller by the given distances, and writes them as the
rectangle differences `/RD [left top right bottom]` (PDF 1.5; the document version is raised). This leaves room
between the shape and the edge of the rectangle, for example for a cloudy border with a margin of your own choice.
A later call replaces the distances.

The array is written in the order of ISO 32000-1 (left, top, right, bottom). The C library
(`HPDF_MarkupAnnot_SetRectDiff`) writes left, the smaller of top and bottom, right, and the larger one, so its second
and fourth values do not follow the specification.

Without a call, the shape fills the rectangle and `/RD` is absent; with a cloudy border, the distances are computed
from the bumps (see [SetCloudEffect()](#method-setcloudeffect)). With both, the given distances are used: bumps that
reach beyond the annotation rectangle are cut off by the viewer.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A distance is negative, NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `left + right` is not less than the width of the rectangle, or `top + bottom` not less than its height |

**Example:**
```csharp
// A cloud with 15 points of room on every side
square.SetCloudEffect(1);
square.SetRectDifference(15, 15, 15, 15);
```

---

## Related Types

- [HpdfGeometricAnnotation](HpdfGeometricAnnotation.md) - base class: interior color
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - author, subject, date, opacity, pop-up
- [HpdfAnnotation](HpdfAnnotation.md) - color, border style, flags
- [HpdfLineAnnotation](HpdfLineAnnotation.md) - the other geometric annotation
- [HpdfPage](../core/HpdfPage.md) - `CreateSquareAnnotation()`, `CreateCircleAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
