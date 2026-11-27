# Hpdf3DPerpendicularMeasure Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

**Base class:** [Hpdf3DMeasure](Hpdf3DMeasure.md)

---

## Purpose

`Hpdf3DPerpendicularMeasure` is a perpendicular dimension in a 3D model (`/Type /3DMeasure /Subtype /PD3`; Adobe
ExtensionLevel 3): the distance between two points of the model, drawn as a dimension line between two leader lines in
a plane, with its value and units. It is the 3D counterpart of a dimension line on a drawing. The value is given, not
measured: the library does not read the model.

---

## Getting an Instance

Perpendicular dimensions are created by the model:

```csharp
public Hpdf3DPerpendicularMeasure CreatePerpendicularMeasure(HpdfPoint3D annotationPlaneNormal, HpdfPoint3D firstAnchor,
    HpdfPoint3D secondAnchor, HpdfPoint3D leaderLinesDirection, HpdfPoint3D valuePosition, HpdfPoint3D textUpDirection,
    float value, string units)   // Hpdf3DModel
```

| Name | Type | Writes | Description |
|------|------|--------|-------------|
| annotationPlaneNormal | `HpdfPoint3D` | `/AP` | The normal of the plane the dimension is drawn in |
| firstAnchor | `HpdfPoint3D` | `/A1` | The first measured point |
| secondAnchor | `HpdfPoint3D` | `/A2` | The second measured point |
| leaderLinesDirection | `HpdfPoint3D` | `/D1` | The direction of the leader lines |
| valuePosition | `HpdfPoint3D` | `/TP` | The position of the value text |
| textUpDirection | `HpdfPoint3D` | `/TY` | The up direction of the value text |
| value | `float` | `/V` | The value shown |
| units | `string` | `/U` | The units shown after the value, such as `mm`; may be empty |

All points and directions are in model coordinates; the directions should lie in the plane. Creating one declares
Adobe ExtensionLevel 3 and raises the document version to PDF 1.7. Add it to the views that show it with
[Hpdf3DView.AddMeasure()](Hpdf3DView.md#method-addmeasure), and list it among the comments with
`HpdfPage.CreateProjectionAnnotation()`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate or `value` is NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `units` is `null` |

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.ThreeD;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var model = pdf.Load3DModel("part.prc");
var view = model.CreateView("Dimensions");
view.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(0, 0, 1), 1000, 0);    // from above; a PRC model needs a camera
page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part");

// The 40 mm between (0, 0, 0) and (40, 0, 0), drawn in the x-y plane, the value 10 units along +y
var width = model.CreatePerpendicularMeasure(
    annotationPlaneNormal: new HpdfPoint3D(0, 0, 1),
    firstAnchor: new HpdfPoint3D(0, 0, 0),
    secondAnchor: new HpdfPoint3D(40, 0, 0),
    leaderLinesDirection: new HpdfPoint3D(0, 1, 0),
    valuePosition: new HpdfPoint3D(20, 10, 0),
    textUpDirection: new HpdfPoint3D(0, 1, 0),
    value: 40,
    units: "mm");
width.SetName("Width");
view.AddMeasure(width);
page.CreateProjectionAnnotation("Width: 40 mm", width);

pdf.SaveToFile("dimension.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](Hpdf3DMeasure.md#property-dict) | `HpdfDict` | The measurement dictionary (inherited) |
| [ProjectionAnnotation](Hpdf3DMeasure.md#property-projectionannotation) | `HpdfProjectionAnnotation?` | The projection annotation, or `null` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetName()](Hpdf3DMeasure.md#method-setname) | Sets the name shown in the list of measurements (`/TRL`) (inherited) |
| [SetColor()](Hpdf3DMeasure.md#method-setcolor) | Sets the color (`/C`) (inherited) |
| [SetTextSize()](Hpdf3DMeasure.md#method-settextsize) | Sets the size of the value text (`/TS`) (inherited) |

The class has no members of its own: everything it measures is given to `CreatePerpendicularMeasure()`.

---

## Related Types

- [Hpdf3DMeasure](Hpdf3DMeasure.md) - base class: name, color, text size, projection annotation
- [Hpdf3DCommentNote](Hpdf3DCommentNote.md) - the other measurement kind
- [Hpdf3DModel](Hpdf3DModel.md) - `CreatePerpendicularMeasure()`
- [HpdfProjectionAnnotation](../annotations/HpdfProjectionAnnotation.md) - lists the dimension among the comments
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
