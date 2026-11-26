# HpdfPageDirection Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPageDirection` chooses the orientation of a predefined [HpdfPageSize](HpdfPageSize.md): portrait keeps the
paper's width and height, landscape swaps them.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfDocument.AddPage(HpdfPageSize size, HpdfPageDirection direction)` | Adds a page of that size and orientation |
| `HpdfPage.SetSize(HpdfPageSize size, HpdfPageDirection direction)` | Resizes an existing page |
| `HpdfPageSizes.GetSize(HpdfPageSize, HpdfPageDirection, out float, out float)` | Returns the width and height in points |

The orientation only decides the MediaBox dimensions; the page content is not rotated. To turn a page when it is
displayed or printed, use the `Rotate` property of [HpdfPage](../core/HpdfPage.md).

---

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
var page = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Landscape);

Console.WriteLine($"{page.Width} x {page.Height}");   // 792 x 612

pdf.SaveToFile("landscape.pdf");
```

---

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Portrait` | 0 | Width and height as listed for the page size (all predefined sizes are taller than wide) |
| `Landscape` | 1 | Width and height swapped |

`HpdfPageSizes.GetSize` treats every value other than `Portrait` as landscape.

---

[Back to Index](../../INDEX.md)
