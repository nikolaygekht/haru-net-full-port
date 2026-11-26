# HpdfAnnotationType Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfAnnotationType` names the kind of an annotation; it is the value of
[HpdfAnnotation.Type](HpdfAnnotation.md#property-type) and determines the `/Subtype` written to the annotation
dictionary. The enum lists the annotation types of the PDF specification, but the library creates only three of them:
`Text`, `Link` and `Widget`. The constructor of `HpdfAnnotation` that takes a type is protected, and `HpdfPage` adds
only these three kinds to a page, so the other values cannot be used to create annotations.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Text` | 0 | Sticky note, [HpdfTextAnnotation](HpdfTextAnnotation.md). Writes `/Subtype /Text`. **Supported.** |
| `Link` | 1 | Link to a URI or a destination, [HpdfLinkAnnotation](HpdfLinkAnnotation.md). Writes `/Subtype /Link`. **Supported.** |
| `Sound` | 2 | Sound annotation (`/Sound`). Not implemented. |
| `FreeText` | 3 | Text shown directly on the page (`/FreeText`). Not implemented. |
| `Stamp` | 4 | Rubber stamp (`/Stamp`). Not implemented. |
| `Square` | 5 | Rectangle (`/Square`). Not implemented. |
| `Circle` | 6 | Ellipse (`/Circle`). Not implemented. |
| `StrikeOut` | 7 | Strike-out text markup (`/StrikeOut`). Not implemented. |
| `Highlight` | 8 | Highlight text markup (`/Highlight`). Not implemented. |
| `Underline` | 9 | Underline text markup (`/Underline`). Not implemented. |
| `Ink` | 10 | Freehand drawing (`/Ink`). Not implemented. |
| `FileAttachment` | 11 | Attached file (`/FileAttachment`). Not implemented (file attachments are not supported). |
| `Popup` | 12 | Pop-up window of another annotation (`/Popup`). Not implemented; sticky notes use the viewer's pop-up. |
| `ThreeD` | 13 | 3D artwork (`/3D`). Not implemented (U3D is not supported). |
| `Squiggly` | 14 | Squiggly underline text markup (`/Squiggly`). Not implemented. |
| `Line` | 15 | Line (`/Line`). Not implemented. |
| `Projection` | 16 | Projection annotation (`/Projection`). Not implemented. |
| `Widget` | 17 | Form field widget, [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md). Writes `/Subtype /Widget`. **Supported.** |

Shapes (rectangles, ellipses, lines) and highlighted or underlined text can be drawn as page content instead; see
[HpdfPageGraphics](../extensions/HpdfPageGraphics.md) and [HpdfPageShapes](../extensions/HpdfPageShapes.md).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

HpdfAnnotation[] annotations =
{
    page.CreateTextAnnotation(new HpdfRect(50, 750, 70, 770), "A note"),
    page.CreateLinkAnnotation(new HpdfRect(50, 700, 200, 720), "https://example.com")
};

foreach (var annotation in annotations)
{
    if (annotation.Type == HpdfAnnotationType.Link)
        annotation.SetBorderStyle(HpdfBorderStyle.Solid, 0);
}

pdf.SaveToFile("types.pdf");
```

## Related Types

- [HpdfAnnotation](HpdfAnnotation.md) - the `Type` property
- [HpdfTextAnnotation](HpdfTextAnnotation.md), [HpdfLinkAnnotation](HpdfLinkAnnotation.md),
  [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md)
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotType` in `Haru.Types`, an unused copy of this enum from the
  C library

---

[Back to Index](../../INDEX.md)
