# HpdfLineCap Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfLineCap` is the shape drawn at the ends of open subpaths when they are stroked (the PDF line cap style).
It is set with `SetLineCap`, which writes the numeric value with the `J` operator.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `ButtEnd` | 0 | The stroke is squared off at the endpoint of the path; nothing extends beyond it. This is the default (`HpdfConst.DefLineCap`). Writes `0 J`. |
| `RoundEnd` | 1 | A semicircular arc with a diameter equal to the line width is drawn around the endpoint. Writes `1 J`. |
| `ProjectingSquareEnd` | 2 | The stroke continues beyond the endpoint for half the line width and is squared off. Writes `2 J`. |
| `LineCapEof` | 3 | Sentinel kept from the C library (number of values). **Not a valid argument:** `SetLineCap` does not reject it and would write the invalid operand `3 J`. |

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.SetLineWidth(12);
page.SetLineCap(HpdfLineCap.RoundEnd);
page.MoveTo(100, 700);
page.LineTo(300, 700);
page.Stroke();

pdf.SaveToFile("linecap.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfPageGraphics.SetLineCap(this IDrawable drawable, HpdfLineCap lineCap)` | Sets the line cap style of a page (or any other `IDrawable`). See [HpdfPageGraphics](../extensions/HpdfPageGraphics.md). |
| `HpdfGraphicsState.LineCap` | The line cap of the current graphics state (`page.GraphicsState.LineCap`), updated by `SetLineCap`. |
| `HpdfConst.DefLineCap` | The default line cap, `ButtEnd`. See [HpdfConst](HpdfConst.md). |

## Related Types

- [HpdfLineJoin](HpdfLineJoin.md) - shape of the corners between path segments
- [HpdfDashMode](HpdfDashMode.md) - dash pattern of stroked lines
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - line style and path operations

---

[Back to Index](../../INDEX.md)
