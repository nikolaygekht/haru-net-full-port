# HpdfLineIntent Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfLineIntent` tells viewers what a line annotation is for: an arrow or a dimension line. It is passed to
[HpdfLineAnnotation.SetIntent()](HpdfLineAnnotation.md#method-setintent) and written as `/IT` (PDF 1.6). The intent
does not change the generated appearance; the look of a dimension line comes from its leader lines, endings and
caption.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Arrow` | 0 | The line is an arrow. Writes `/IT /LineArrow` |
| `Dimension` | 1 | The line is a dimension line that shows a measurement. Writes `/IT /LineDimension` |

Without a call to `SetIntent()`, `/IT` is absent. A value outside the enum makes `SetIntent()` throw `HpdfException`
(`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var arrow = page.CreateLineAnnotation(new HpdfPoint(300, 600), new HpdfPoint(150, 700), "Look here");
arrow.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.OpenArrow);
arrow.SetIntent(HpdfLineIntent.Arrow);

var dimension = page.CreateLineAnnotation(new HpdfPoint(100, 500), new HpdfPoint(400, 500), "300 pt");
dimension.SetLineEndingStyle(HpdfLineEndingStyle.Butt, HpdfLineEndingStyle.Butt);
dimension.SetLeader(15, 4);
dimension.SetIntent(HpdfLineIntent.Dimension);

pdf.SaveToFile("intents.pdf");
```

## Related Types

- [HpdfLineAnnotation](HpdfLineAnnotation.md) - `SetIntent()`
- [HpdfFreeTextIntent](HpdfFreeTextIntent.md) - the intent of free text annotations
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotIntent` in `Haru.Types`, an unused enum from the C library
  with the intents of all annotation types

---

[Back to Index](../../INDEX.md)
