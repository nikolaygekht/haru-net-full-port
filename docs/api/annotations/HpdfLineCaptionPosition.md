# HpdfLineCaptionPosition Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfLineCaptionPosition` is where the caption of a line annotation (its text shown on the line) is placed. It is
passed to [HpdfLineAnnotation.SetCaption()](HpdfLineAnnotation.md#method-setcaption) and written as `/CP`.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Inline` | 0 | The caption is centered inside the line, which is broken around it with a gap of 2 points on each side (when the caption fits between the ends of the line). The PDF default: `/CP` is not written |
| `Top` | 1 | The caption is on top of the line, 2 points above it. Writes `/CP /Top` (PDF 1.7) |

"On top" is on the left of the line's direction: above a line drawn from left to right, below a line drawn from right
to left (the caption is then upside down, as the text runs along the line). The caption can be moved along and across
the line with the offsets of `SetCaption()`.

A value outside the enum makes `SetCaption()` throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

var inline = page.CreateLineAnnotation(new HpdfPoint(100, 700), new HpdfPoint(400, 700), "Inline caption");
inline.SetCaption(font, 10, HpdfLineCaptionPosition.Inline);

var top = page.CreateLineAnnotation(new HpdfPoint(100, 650), new HpdfPoint(400, 650), "Caption on top");
top.SetCaption(font, 10, HpdfLineCaptionPosition.Top);

pdf.SaveToFile("captions.pdf");
```

## Related Types

- [HpdfLineAnnotation](HpdfLineAnnotation.md) - `SetCaption()`
- [Reserved Types](../types/ReservedTypes.md) - `HpdfLineAnnotCapPosition` in `Haru.Types`, an unused copy of this
  enum from the C library

---

[Back to Index](../../INDEX.md)
