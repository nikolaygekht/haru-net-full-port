# HpdfProjectionAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfProjectionAnnotation` is a projection annotation (Adobe ExtensionLevel 3, later part of PDF 2.0): a markup
annotation that makes a 3D measurement ([Hpdf3DMeasure](../threed/Hpdf3DMeasure.md)) one of the comments of the page.
Viewers that show 3D list the measurement in their comments panel, where the reader can select it, reply to it and see
it in the model. The annotation has no appearance of its own: the measurement is drawn by the 3D viewer inside the 3D
annotation.

---

## Getting an Instance

Create the measurement with the model, then the projection annotation with the page of the 3D annotation:

```csharp
public HpdfProjectionAnnotation CreateProjectionAnnotation(HpdfRect rect, string text, Hpdf3DMeasure measure)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The annotation rectangle, usually the rectangle of the 3D annotation. Reversed edges are swapped |
| text | `string` | The text of the annotation, shown in the comments list |
| measure | `Hpdf3DMeasure` | The measurement, created by [Hpdf3DModel.CreateCommentNote()](../threed/Hpdf3DModel.md#method-createcommentnote) or [CreatePerpendicularMeasure()](../threed/Hpdf3DModel.md#method-createperpendicularmeasure) |

The method creates the annotation (`/Subtype /Projection`) with the external data
`/ExData << /Type /ExData /Subtype /3DM /M3DREF measure >>`, connects the measurement back to it (`/S` of the
measurement, [Hpdf3DMeasure.ProjectionAnnotation](../threed/Hpdf3DMeasure.md#property-projectionannotation)), and
adds it to the page's `/Annots` array. Creating one declares Adobe ExtensionLevel 3 in the catalog and raises the
document version to PDF 1.7.

Unlike the other markup annotations, a projection annotation has no appearance stream (ISO 32000-2 defines none for
it) and no flags: it is not printed. PDF/A-1 does not allow it: saving a PDF/A document with one throws
`HpdfException` (`InvalidOperation`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `measure` or `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |
| `HpdfException` (`InvalidOperation`) | The measurement already has a projection annotation (one per measurement) |

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
var view = model.CreateView("Measurements");
var area = new HpdfRect(50, 350, 550, 750);
page.Create3DAnnotation(area, model, "A machine part");

// A comment note at the origin of the model, shown in the view and listed among the comments
var note = model.CreateCommentNote(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(20, 20, 20));
note.SetText("The origin of the model");
view.AddMeasure(note);

var projection = page.CreateProjectionAnnotation(area, "The origin of the model", note);
projection.SetTitle("Designer");

pdf.SaveToFile("measure.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Measure](#property-measure) | `Hpdf3DMeasure` | The 3D measurement the annotation stands for |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); none by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Projection` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext) | Replies and rich text (inherited) |
| [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Throws `HpdfException` (`InvalidOperation`): the external data of a projection annotation is its measurement (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle), [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Write `/BS` and `/C` (inherited); nothing is drawn by the library |

---

## Detailed Descriptions

#### Property: *Measure*

**Declaration:**
```csharp
public Hpdf3DMeasure Measure { get; }
```

**Description:**
The measurement given at creation; its [ProjectionAnnotation](../threed/Hpdf3DMeasure.md#property-projectionannotation)
is this annotation.

**Example:**
```csharp
Console.WriteLine(ReferenceEquals(projection.Measure.ProjectionAnnotation, projection));   // True
```

---

## Related Types

- [Hpdf3DMeasure](../threed/Hpdf3DMeasure.md), [Hpdf3DCommentNote](../threed/Hpdf3DCommentNote.md),
  [Hpdf3DPerpendicularMeasure](../threed/Hpdf3DPerpendicularMeasure.md) - the measurements
- [Hpdf3DAnnotation](Hpdf3DAnnotation.md) - the annotation that shows the model
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, replies
- [HpdfPage](../core/HpdfPage.md) - `CreateProjectionAnnotation()`
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
