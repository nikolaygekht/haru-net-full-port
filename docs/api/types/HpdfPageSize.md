# HpdfPageSize Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPageSize` selects one of twelve predefined paper sizes for a page. Combine it with
[HpdfPageDirection](HpdfPageDirection.md) to choose portrait or landscape orientation.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfDocument.AddPage(HpdfPageSize size, HpdfPageDirection direction)` | Adds a page of that size |
| `HpdfPage.SetSize(HpdfPageSize size, HpdfPageDirection direction)` | Resizes an existing page |
| `HpdfPageSizes.GetSize(HpdfPageSize, HpdfPageDirection, out float, out float)` | Returns the width and height in points |

Both page methods set the MediaBox to `[0 0 width height]`. A page added with `AddPage()` without arguments is A4
portrait (595.276 x 841.89 points). See [HpdfPage](../core/HpdfPage.md) and [HpdfDocument](../core/HpdfDocument.md).

---

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();

var letter = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Portrait);   // 612 x 792
var a4Wide = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Landscape);      // 841.89 x 595.276

var page = pdf.AddPage();
page.SetSize(HpdfPageSize.A5, HpdfPageDirection.Portrait);                   // 419.528 x 595.276

pdf.SaveToFile("sizes.pdf");
```

---

## Values

Sizes are in points (1 point = 1/72 inch), portrait orientation (width x height), taken from
[HpdfPageSizes](HpdfPageSizes.md). Landscape swaps width and height.

| Name | Value | Paper size | Width (pt) | Height (pt) |
|------|-------|------------|-----------:|------------:|
| `Letter` | 0 | 8.5 x 11 in | 612 | 792 |
| `Legal` | 1 | 8.5 x 14 in | 612 | 1008 |
| `A3` | 2 | 297 x 420 mm | 841.89 | 1190.551 |
| `A4` | 3 | 210 x 297 mm | 595.276 | 841.89 |
| `A5` | 4 | 148 x 210 mm | 419.528 | 595.276 |
| `B4` | 5 | 250 x 353 mm (ISO B4) | 708.661 | 1000.63 |
| `B5` | 6 | 176 x 250 mm (ISO B5) | 498.898 | 708.661 |
| `Executive` | 7 | 7.25 x 10.5 in | 522 | 756 |
| `US4x6` | 8 | 4 x 6 in | 288 | 432 |
| `US4x8` | 9 | 4 x 8 in | 288 | 576 |
| `US5x7` | 10 | 5 x 7 in | 360 | 504 |
| `Comm10` | 11 | Commercial #10 envelope, 4.125 x 9.5 in | 297 | 684 |

Passing a value outside this list (for example `(HpdfPageSize)12`) makes `AddPage`, `SetSize` and `GetSize` throw
an `HpdfException` with `HpdfErrorCode.PageInvalidSize`.

For other sizes use `HpdfDocument.AddPage(float width, float height)`, `HpdfPage.SetSize(float width, float height)`
or the `Width` and `Height` properties of [HpdfPage](../core/HpdfPage.md).

---

[Back to Index](../../INDEX.md)
