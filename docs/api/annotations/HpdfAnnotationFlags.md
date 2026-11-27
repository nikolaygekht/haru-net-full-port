# HpdfAnnotationFlags Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfAnnotationFlags` is the set of annotation flags (`/F`): whether a viewer shows and prints an annotation and
whether the reader may change it. It is the type of the [Flags](HpdfAnnotation.md#property-flags) property of every
annotation. The enum has the `[Flags]` attribute and the values are the bit values of the PDF `/F` entry, so they are
combined with `|`.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No flags: the annotation is shown on screen but not printed. Setting it removes `/F` |
| `Invisible` | 1 | Do not show an annotation of a type the viewer does not know (instead of an icon for an unknown type) |
| `Hidden` | 2 | Do not show or print the annotation, and do not let the reader interact with it |
| `Print` | 4 | Print the annotation when the page is printed |
| `NoZoom` | 8 | Do not scale the appearance with the zoom of the page |
| `NoRotate` | 16 | Do not rotate the appearance with the page |
| `NoView` | 32 | Do not show the annotation on screen; it may still be printed |
| `ReadOnly` | 64 | Do not let the reader interact with the annotation |
| `Locked` | 128 | Do not let the reader delete the annotation or change its properties, such as its position |
| `ToggleNoView` | 256 | Invert `NoView` for certain events, such as the mouse over the annotation (PDF 1.5) |
| `LockedContents` | 512 | Do not let the reader change the contents of the annotation (PDF 1.7) |

Defaults of the annotations the library creates:

| Annotation | `Flags` after creation |
|------------|------------------------|
| [HpdfShapeAnnotation](HpdfShapeAnnotation.md), [HpdfLineAnnotation](HpdfLineAnnotation.md), [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md), [HpdfFreeTextAnnotation](HpdfFreeTextAnnotation.md), [HpdfStampAnnotation](HpdfStampAnnotation.md) | `Print` |
| [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md) (form fields) | `Print` |
| [HpdfTextAnnotation](HpdfTextAnnotation.md), [HpdfLinkAnnotation](HpdfLinkAnnotation.md), [HpdfPopupAnnotation](HpdfPopupAnnotation.md) | `None` |

Setting a value with bits outside the enum (above 1023) throws `HpdfException` (`InvalidParameter`). In a PDF/A
document, saving adds `Print` to the flags of every annotation; the other flags are kept.

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var bold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");
var page = pdf.AddPage();

// A sticky note that is printed
var note = page.CreateTextAnnotation(new HpdfRect(50, 750, 70, 770), "Printed note");
note.Flags = HpdfAnnotationFlags.Print;

// A stamp that the reader can neither move nor delete
var stamp = page.CreateStampAnnotation(new HpdfRect(380, 760, 560, 810), HpdfStampName.Final, "", bold);
stamp.Flags |= HpdfAnnotationFlags.Locked;

// A square shown on screen only
var square = page.CreateSquareAnnotation(new HpdfRect(50, 600, 200, 700), "Screen only");
square.Flags = HpdfAnnotationFlags.None;

Console.WriteLine(stamp.Flags);   // Print, Locked

pdf.SaveToFile("flags.pdf");
```

## Related Types

- [HpdfAnnotation](HpdfAnnotation.md) - the `Flags` property
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotFlags` in `Haru.Types`, an unused enum from the C library
  whose values are bit positions, not bit values

---

[Back to Index](../../INDEX.md)
