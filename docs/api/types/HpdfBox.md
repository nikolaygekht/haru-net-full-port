# HpdfBox Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfBox` is a rectangle given by its four edges (left, bottom, right, top) in points. It has the same members and
behavior as [HpdfRect](HpdfRect.md) and is used for page boxes and font bounding boxes.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfPage.SetCropBox(HpdfBox)` / `GetCropBox()` | Visible region of the page (/CropBox) |
| `HpdfPage.SetBleedBox(HpdfBox)` / `GetBleedBox()` | Clipping region for production output (/BleedBox) |
| `HpdfPage.SetTrimBox(HpdfBox)` / `GetTrimBox()` | Intended size of the finished page (/TrimBox) |
| `HpdfPage.SetArtBox(HpdfBox)` / `GetArtBox()` | Meaningful content of the page (/ArtBox) |
| `HpdfFont.GetBBox()` | Font bounding box in glyph space (1000 units per em) |
| `IHpdfFontImplementation.FontBBox`, `FontMetrics.FontBBox` (`Haru.Font.CID`) | Bounding box in lower-level font implementations |

The `Get...Box()` methods return `HpdfBox?`: `null` when the page has no such box. Each page box also has a
`Set...Box(float left, float bottom, float right, float top)` overload. See [HpdfPage](../core/HpdfPage.md)
and [HpdfFont](../core/HpdfFont.md).

`HpdfBox` and `HpdfRect` are separate types: there is no conversion between them, and an `HpdfBox` is never
equal to an `HpdfRect`.

---

## Getting an Instance

Create it with the constructor (left, bottom, right, top), or read it from a page or font:

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var box = new HpdfBox(36, 36, 559, 806);
HpdfBox? crop = page.GetCropBox();   // null: no CropBox set yet
HpdfBox bbox = font.GetBBox();
```

`default(HpdfBox)` has all four edges at 0.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);

// Trim 10 mm (about 28.35 points) from each side of the A4 page
var trim = new HpdfBox(28.35f, 28.35f, page.Width - 28.35f, page.Height - 28.35f);
page.SetTrimBox(trim);
page.SetBleedBox(new HpdfBox(0, 0, page.Width, page.Height));

HpdfBox? stored = page.GetTrimBox();
if (stored is HpdfBox t)
    Console.WriteLine($"Trim box: {t} ({t.Width} x {t.Height})");

pdf.SaveToFile("boxes.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Left](#property-left) | `float` | X coordinate of the left edge |
| [Bottom](#property-bottom) | `float` | Y coordinate of the bottom edge |
| [Right](#property-right) | `float` | X coordinate of the right edge |
| [Top](#property-top) | `float` | Y coordinate of the top edge |
| [Width](#property-width) | `float` | `Right - Left` (read-only) |
| [Height](#property-height) | `float` | `Top - Bottom` (read-only) |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfBox(float, float, float, float)](#constructor-hpdfbox) | Creates a box from its edges |
| [Equals()](#method-equals) | Compares all four edges |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from the four edges |
| [ToString()](#method-tostring) | Text form `[L:left, B:bottom, R:right, T:top]` |

---

## Detailed Descriptions

#### Property: *Left*

**Declaration:**
```csharp
public float Left { get; set; }
```

**Description:**
X coordinate of the left edge, in points (or glyph units for a font bounding box).

---

#### Property: *Bottom*

**Declaration:**
```csharp
public float Bottom { get; set; }
```

**Description:**
Y coordinate of the bottom edge.

---

#### Property: *Right*

**Declaration:**
```csharp
public float Right { get; set; }
```

**Description:**
X coordinate of the right edge.

---

#### Property: *Top*

**Declaration:**
```csharp
public float Top { get; set; }
```

**Description:**
Y coordinate of the top edge.

---

#### Property: *Width*

**Declaration:**
```csharp
public float Width { get; }
```

**Description:**
Returns `Right - Left`. The edges are not validated, so the value can be negative.

---

#### Property: *Height*

**Declaration:**
```csharp
public float Height { get; }
```

**Description:**
Returns `Top - Bottom`. Can be negative when `Top` is less than `Bottom`.

---

#### Constructor: *HpdfBox()*

**Declaration:**
```csharp
public HpdfBox(float left, float bottom, float right, float top)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | X coordinate of the left edge |
| bottom | `float` | Y coordinate of the bottom edge |
| right | `float` | X coordinate of the right edge |
| top | `float` | Y coordinate of the top edge |

**Description:**
Stores the four values as given; there is no validation. The page box setters write them to the page dictionary
unchanged.

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfBox other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all four edges are equal (exact `float` comparison). The `object` overload returns `false` for any
other type, including an `HpdfRect` with the same values.

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfBox left, HpdfBox right)
public static bool operator !=(HpdfBox left, HpdfBox right)
```

**Description:**
Same comparison as `Equals(HpdfBox)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Returns the same hash code as an `HpdfRect` with the same edges.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
Same format as [HpdfRect.ToString()](HpdfRect.md#method-tostring), for example `[L:0, B:0, R:595.276, T:841.89]`.

---

[Back to Index](../../INDEX.md)
