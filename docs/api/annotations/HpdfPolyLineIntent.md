# HpdfPolyLineIntent Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfPolyLineIntent` tells viewers what a polyline annotation is meant for. It is passed to
[HpdfPolyLineAnnotation.SetIntent()](HpdfPolyLineAnnotation.md#method-setintent) and written as `/IT` (PDF 1.6).
ISO 32000-1 defines one intent for polylines.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Dimension` | 0 | A dimension polyline, a measured path. Writes `/IT /PolyLineDimension` |

Without a call to `SetIntent()`, `/IT` is absent: a plain polyline. The intent changes nothing in the generated
appearance. A value outside the enum makes `SetIntent()` throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var path = page.CreatePolyLineAnnotation(new HpdfPoint[] { new(100, 600), new(250, 600), new(250, 700) }, "250 pt");
path.SetLineEndingStyle(HpdfLineEndingStyle.Butt, HpdfLineEndingStyle.Butt);
path.SetIntent(HpdfPolyLineIntent.Dimension);

pdf.SaveToFile("dimension.pdf");
```

## Related Types

- [HpdfPolyLineAnnotation](HpdfPolyLineAnnotation.md) - `SetIntent()`
- [HpdfPolygonIntent](HpdfPolygonIntent.md), [HpdfLineIntent](HpdfLineIntent.md),
  [HpdfFreeTextIntent](HpdfFreeTextIntent.md) - the intents of the other annotation types
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotIntent` in `Haru.Types`, an unused enum from the C library

---

[Back to Index](../../INDEX.md)
