# HpdfPageSizes Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPageSizes` is a static helper that turns a predefined [HpdfPageSize](HpdfPageSize.md) and an
[HpdfPageDirection](HpdfPageDirection.md) into a width and height in points. `HpdfPage.SetSize(HpdfPageSize,
HpdfPageDirection)` and `HpdfDocument.AddPage(HpdfPageSize, HpdfPageDirection)` use it; call it directly when you
need the dimensions without creating a page, for example to lay out content before adding the page.

---

## Getting an Instance

`HpdfPageSizes` is a static class; call its method on the type name.

---

## Example

```csharp
using Haru.Doc;

HpdfPageSizes.GetSize(HpdfPageSize.A4, HpdfPageDirection.Portrait, out float width, out float height);
Console.WriteLine($"A4: {width} x {height} points");   // A4: 595.276 x 841.89 points

HpdfPageSizes.GetSize(HpdfPageSize.Legal, HpdfPageDirection.Landscape, out width, out height);
Console.WriteLine($"Legal landscape: {width} x {height}");   // 1008 x 612
```

---

## Properties

`HpdfPageSizes` has no public properties. The size table is private and loaded once.

---

## Methods

| Name | Description |
|------|-------------|
| [GetSize()](#method-getsize) | Static: width and height of a predefined page size in points |

---

## Detailed Descriptions

#### Method: *GetSize()*

**Declaration:**
```csharp
public static void GetSize(HpdfPageSize size, HpdfPageDirection direction, out float width, out float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| size | `HpdfPageSize` | The predefined page size |
| direction | `HpdfPageDirection` | `Portrait` returns the size as listed; any other value swaps width and height |
| width | `out float` | Page width in points |
| height | `out float` | Page height in points |

**Return Value:**
`void`; the result is in `width` and `height`.

**Description:**
Looks the size up in this table (portrait values, in points):

| Size | Width | Height |
|------|------:|-------:|
| `Letter` | 612 | 792 |
| `Legal` | 612 | 1008 |
| `A3` | 841.89 | 1190.551 |
| `A4` | 595.276 | 841.89 |
| `A5` | 419.528 | 595.276 |
| `B4` | 708.661 | 1000.63 |
| `B5` | 498.898 | 708.661 |
| `Executive` | 522 | 756 |
| `US4x6` | 288 | 432 |
| `US4x8` | 288 | 576 |
| `US5x7` | 360 | 504 |
| `Comm10` | 297 | 684 |

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `IndexOutOfRangeException` | `size` is not one of the defined `HpdfPageSize` values |

---

[Back to Index](../../INDEX.md)
