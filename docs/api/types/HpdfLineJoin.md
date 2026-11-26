# HpdfLineJoin Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfLineJoin` is the shape drawn at the corners where two segments of a stroked path meet (the PDF line join
style). It is set with `SetLineJoin`, which writes the numeric value with the `j` operator.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `MiterJoin` | 0 | The outer edges of the two segments are extended until they meet in a sharp point. If the point would be longer than the miter limit (`SetMiterLimit`), a bevel is drawn instead. This is the default (`HpdfConst.DefLineJoin`). Writes `0 j`. |
| `RoundJoin` | 1 | A circular arc with a diameter equal to the line width is drawn around the corner. Writes `1 j`. |
| `BevelJoin` | 2 | The corner is cut off with a straight line across the outer edges. Writes `2 j`. |
| `LineJoinEof` | 3 | Sentinel kept from the C library (number of values). **Not a valid argument:** `SetLineJoin` does not reject it and would write the invalid operand `3 j`. |

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.SetLineWidth(10);
page.SetLineJoin(HpdfLineJoin.BevelJoin);
page.MoveTo(100, 600);
page.LineTo(150, 700);
page.LineTo(200, 600);
page.Stroke();

pdf.SaveToFile("linejoin.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfPageGraphics.SetLineJoin(this IDrawable drawable, HpdfLineJoin lineJoin)` | Sets the line join style of a page (or any other `IDrawable`). See [HpdfPageGraphics](../extensions/HpdfPageGraphics.md). |
| `HpdfGraphicsState.LineJoin` | The line join of the current graphics state (`page.GraphicsState.LineJoin`), updated by `SetLineJoin`. |
| `HpdfConst.DefLineJoin` | The default line join, `MiterJoin`. See [HpdfConst](HpdfConst.md). |

## Related Types

- [HpdfLineCap](HpdfLineCap.md) - shape of the line ends
- [HpdfDashMode](HpdfDashMode.md) - dash pattern of stroked lines
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - line style and path operations

---

[Back to Index](../../INDEX.md)
