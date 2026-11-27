# HpdfPolygonIntent Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfPolygonIntent` tells viewers what a polygon annotation is meant for: a cloud around an area, or the outline of a
measured area. It is passed to [HpdfPolygonAnnotation.SetIntent()](HpdfPolygonAnnotation.md#method-setintent) and
written as `/IT` (PDF 1.6).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Cloud` | 0 | A cloud around an area of the page. Writes `/IT /PolygonCloud`. Draw it with a cloudy border ([SetCloudEffect()](HpdfPolygonAnnotation.md#method-setcloudeffect)): the intent does not draw the cloud |
| `Dimension` | 1 | A dimension polygon, the outline of a measured area. Writes `/IT /PolygonDimension` |

Without a call to `SetIntent()`, `/IT` is absent: a plain polygon. The intent changes nothing in the generated
appearance; it tells viewers which tool made the annotation. A value outside the enum makes `SetIntent()` throw
`HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var cloud = page.CreatePolygonAnnotation(new HpdfPoint[]
{
    new(100, 600), new(300, 600), new(300, 650), new(200, 650), new(200, 720), new(100, 720)
}, "Rework this area");
cloud.SetCloudEffect(1);
cloud.SetIntent(HpdfPolygonIntent.Cloud);

pdf.SaveToFile("cloud.pdf");
```

## Related Types

- [HpdfPolygonAnnotation](HpdfPolygonAnnotation.md) - `SetIntent()`, `SetCloudEffect()`
- [HpdfPolyLineIntent](HpdfPolyLineIntent.md), [HpdfLineIntent](HpdfLineIntent.md),
  [HpdfFreeTextIntent](HpdfFreeTextIntent.md) - the intents of the other annotation types
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotIntent` in `Haru.Types`, an unused enum from the C library
  with the intents of all annotation types

---

[Back to Index](../../INDEX.md)
