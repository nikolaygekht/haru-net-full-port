# HpdfAnnotationType Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfAnnotationType` names the kind of an annotation; it is the value of
[HpdfAnnotation.Type](HpdfAnnotation.md#property-type) and determines the `/Subtype` written to the annotation
dictionary. The enum lists the annotation types of the PDF specification; the library creates the ones marked
**Supported** below, each with a method of `HpdfPage`. The constructor of `HpdfAnnotation` that takes a type is
protected, so the other values cannot be used to create annotations.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Text` | 0 | Sticky note, [HpdfTextAnnotation](HpdfTextAnnotation.md). Writes `/Subtype /Text`. **Supported.** |
| `Link` | 1 | Link to a URI or a destination, [HpdfLinkAnnotation](HpdfLinkAnnotation.md). Writes `/Subtype /Link`. **Supported.** |
| `Sound` | 2 | Sound annotation (`/Sound`). Not implemented. |
| `FreeText` | 3 | Text shown directly on the page, [HpdfFreeTextAnnotation](HpdfFreeTextAnnotation.md). Writes `/Subtype /FreeText`. **Supported.** |
| `Stamp` | 4 | Rubber stamp, [HpdfStampAnnotation](HpdfStampAnnotation.md). Writes `/Subtype /Stamp`. **Supported.** |
| `Square` | 5 | Rectangle, [HpdfShapeAnnotation](HpdfShapeAnnotation.md). Writes `/Subtype /Square`. **Supported.** |
| `Circle` | 6 | Ellipse, [HpdfShapeAnnotation](HpdfShapeAnnotation.md). Writes `/Subtype /Circle`. **Supported.** |
| `StrikeOut` | 7 | Struck-out text, [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md). Writes `/Subtype /StrikeOut`. **Supported.** |
| `Highlight` | 8 | Highlighted text, [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md). Writes `/Subtype /Highlight`. **Supported.** |
| `Underline` | 9 | Underlined text, [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md). Writes `/Subtype /Underline`. **Supported.** |
| `Ink` | 10 | Freehand strokes, [HpdfInkAnnotation](HpdfInkAnnotation.md). Writes `/Subtype /Ink`. **Supported.** |
| `FileAttachment` | 11 | An icon that opens an embedded file, [HpdfFileAttachmentAnnotation](HpdfFileAttachmentAnnotation.md). Writes `/Subtype /FileAttachment`. **Supported.** |
| `Popup` | 12 | Pop-up window of a markup annotation, [HpdfPopupAnnotation](HpdfPopupAnnotation.md). Writes `/Subtype /Popup`. **Supported.** |
| `ThreeD` | 13 | A 3D model on the page, [Hpdf3DAnnotation](Hpdf3DAnnotation.md). Writes `/Subtype /3D`. **Supported.** |
| `Squiggly` | 14 | Text with a squiggly underline, [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md). Writes `/Subtype /Squiggly`. **Supported.** |
| `Line` | 15 | Line, [HpdfLineAnnotation](HpdfLineAnnotation.md). Writes `/Subtype /Line`. **Supported.** |
| `Projection` | 16 | A 3D measurement among the comments, [HpdfProjectionAnnotation](HpdfProjectionAnnotation.md). Writes `/Subtype /Projection`. **Supported.** |
| `Widget` | 17 | Form field widget, [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md). Writes `/Subtype /Widget`. **Supported.** |
| `Polygon` | 18 | Closed shape through vertices, [HpdfPolygonAnnotation](HpdfPolygonAnnotation.md). Writes `/Subtype /Polygon`. **Supported.** Not in the C library |
| `PolyLine` | 19 | Connected lines through vertices, [HpdfPolyLineAnnotation](HpdfPolyLineAnnotation.md). Writes `/Subtype /PolyLine`. **Supported.** Not in the C library |
| `Caret` | 20 | A caret where text is to be inserted, [HpdfCaretAnnotation](HpdfCaretAnnotation.md). Writes `/Subtype /Caret`. **Supported.** Not in the C library |
| `Redact` | 21 | A mark of content to be removed, [HpdfRedactAnnotation](HpdfRedactAnnotation.md). Writes `/Subtype /Redact`. **Supported.** Not in the C library |

The values 0 to 17 are those of the C library, in its order; the last four were added at the end so that the values
of the others did not change. `Sound` is the only value the library does not create.

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.CreateTextAnnotation(new HpdfRect(50, 750, 70, 770), "A note");
page.CreateLinkAnnotation(new HpdfRect(50, 700, 200, 720), "https://example.com");
page.CreateSquareAnnotation(new HpdfRect(50, 600, 200, 680), "A square");

foreach (var annotation in page.Annotations)
{
    if (annotation.Type == HpdfAnnotationType.Link)
        annotation.SetBorderStyle(HpdfBorderStyle.Solid, 0);
    Console.WriteLine(annotation.Type);   // Text, Link, Square
}

pdf.SaveToFile("types.pdf");
```

## Related Types

- [HpdfAnnotation](HpdfAnnotation.md) - the `Type` property
- [HpdfTextAnnotation](HpdfTextAnnotation.md), [HpdfLinkAnnotation](HpdfLinkAnnotation.md),
  [HpdfShapeAnnotation](HpdfShapeAnnotation.md), [HpdfLineAnnotation](HpdfLineAnnotation.md),
  [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md), [HpdfFreeTextAnnotation](HpdfFreeTextAnnotation.md),
  [HpdfStampAnnotation](HpdfStampAnnotation.md), [HpdfPopupAnnotation](HpdfPopupAnnotation.md),
  [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md), [HpdfPolygonAnnotation](HpdfPolygonAnnotation.md),
  [HpdfPolyLineAnnotation](HpdfPolyLineAnnotation.md), [HpdfInkAnnotation](HpdfInkAnnotation.md),
  [HpdfCaretAnnotation](HpdfCaretAnnotation.md), [HpdfRedactAnnotation](HpdfRedactAnnotation.md),
  [HpdfFileAttachmentAnnotation](HpdfFileAttachmentAnnotation.md), [Hpdf3DAnnotation](Hpdf3DAnnotation.md),
  [HpdfProjectionAnnotation](HpdfProjectionAnnotation.md)
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotType` in `Haru.Types`, an unused copy of this enum from the
  C library

---

[Back to Index](../../INDEX.md)
