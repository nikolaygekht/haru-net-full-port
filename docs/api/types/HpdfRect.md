# HpdfRect Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfRect` is a rectangle given by its four edges in PDF user space units (points, 1/72 inch), measured from the
lower-left corner of the page. It is the rectangle type for annotations and form field widgets.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfPage.CreateTextAnnotation(HpdfRect rect, string text, HpdfAnnotationIcon icon)` | Area of a text annotation (sticky note) |
| `HpdfPage.CreateLinkAnnotation(HpdfRect rect, string uri)` | Clickable area of a URI link |
| `HpdfPage.CreateLinkAnnotation(HpdfRect rect, HpdfDestination destination)`, `CreateLinkAnnotation(HpdfRect rect, HpdfArray destination)` | Clickable area of an internal link |
| `HpdfPage.CreateSquareAnnotation(HpdfRect rect, string text)`, `CreateCircleAnnotation(...)`, `CreateFreeTextAnnotation(...)`, `CreateStampAnnotation(...)`, `CreatePopupAnnotation(...)` | Rectangle of a markup annotation or a pop-up window |
| `HpdfPage.CreateHighlightAnnotation(HpdfRect rect, string text)` (and underline, squiggly, strikeout), `HpdfTextMarkupAnnotation.SetQuadPoints(params HpdfRect[] lines)` | The marked lines of text |
| `HpdfPage.CreateCaretAnnotation(HpdfRect rect, string text)`, `CreateFileAttachmentAnnotation(HpdfRect rect, ...)` | Rectangle of a caret or of a file attachment icon |
| `HpdfPage.CreateRedactAnnotation(HpdfRect rect, string text)`, `HpdfRedactAnnotation.SetQuadPoints(params HpdfRect[] areas)` | The areas to redact |
| `HpdfPage.Create3DAnnotation(HpdfRect rect, ...)` | The area that shows a 3D model (`CreateProjectionAnnotation()` takes no rectangle: a projection annotation is written with `/Rect [0 0 0 0]`) |
| `HpdfField.CreateWidget(HpdfRect rect)` | Position and size of a form field widget |
| `HpdfRadioButton.CreateOption(string stateName, HpdfRect rect)` | Position and size of one radio button option |
| `new HpdfWidgetAnnotation(HpdfXref xref, HpdfRect rect)` | Lower-level widget constructor |
| `new HpdfArray(HpdfRect rect)` | Creates a PDF array `[left bottom right top]` |
| `AfmData.FontBBox` (`Haru.Font.Type1`) | Font bounding box read from an AFM file |

See [HpdfPage](../core/HpdfPage.md) and the [Forms Guide](../../guides/FormsGuide.md).
Page boxes (CropBox, BleedBox, TrimBox, ArtBox) and font bounding boxes use the structurally identical
[HpdfBox](HpdfBox.md) instead. There is no conversion between the two types; copy the four values.

---

## Getting an Instance

Create it with the constructor, giving the edges in the order left, bottom, right, top:

```csharp
using Haru.Types;

var rect = new HpdfRect(50, 700, 250, 720);   // left, bottom, right, top
```

`default(HpdfRect)` (or `new HpdfRect()`) has all four edges at 0.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A 200 x 20 point link area whose lower-left corner is at (50, 700)
var area = new HpdfRect(50, 700, 250, 720);
page.CreateLinkAnnotation(area, "https://example.com");

// A sticky note next to it
page.CreateTextAnnotation(new HpdfRect(270, 700, 290, 720), "Visit the site");

Console.WriteLine($"{area.Width} x {area.Height}");   // 200 x 20

pdf.SaveToFile("rect.pdf");
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
| [HpdfRect(float, float, float, float)](#constructor-hpdfrect) | Creates a rectangle from its edges |
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
X coordinate of the left edge, in points.

---

#### Property: *Bottom*

**Declaration:**
```csharp
public float Bottom { get; set; }
```

**Description:**
Y coordinate of the bottom edge, in points.

---

#### Property: *Right*

**Declaration:**
```csharp
public float Right { get; set; }
```

**Description:**
X coordinate of the right edge, in points.

---

#### Property: *Top*

**Declaration:**
```csharp
public float Top { get; set; }
```

**Description:**
Y coordinate of the top edge, in points.

---

#### Property: *Width*

**Declaration:**
```csharp
public float Width { get; }
```

**Description:**
Returns `Right - Left`. The struct does not validate or normalize its edges, so the value is negative when
`Right` is less than `Left`.

---

#### Property: *Height*

**Declaration:**
```csharp
public float Height { get; }
```

**Description:**
Returns `Top - Bottom`. Negative when `Top` is less than `Bottom`.

---

#### Constructor: *HpdfRect()*

**Declaration:**
```csharp
public HpdfRect(float left, float bottom, float right, float top)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | X coordinate of the left edge |
| bottom | `float` | Y coordinate of the bottom edge |
| right | `float` | X coordinate of the right edge |
| top | `float` | Y coordinate of the top edge |

**Description:**
Stores the four values as given; there is no validation.

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfRect other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all four edges are equal (exact `float` comparison). The `object` overload returns `false` for
anything that is not an `HpdfRect` (an [HpdfBox](HpdfBox.md) with the same values is not equal).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfRect left, HpdfRect right)
public static bool operator !=(HpdfRect left, HpdfRect right)
```

**Description:**
Same comparison as `Equals(HpdfRect)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of the four edges.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
A string such as `[L:50, B:700, R:250, T:720]`. The numbers use the default `float` formatting of the invariant
culture, so the text does not depend on the current culture.

---

[Back to Index](../../INDEX.md)
