# HpdfPoint Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfPoint` is a point in two-dimensional PDF user space (X and Y in points). The library uses it to report the
current point of the path being built, and for the points of line annotations, callouts, polygons, polylines, ink
strokes and the quadrilaterals of text markup and redactions.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfPage.CurrentPos` | Current point of the path on a page |
| `HpdfAppearanceCanvas.CurrentPos` | Current point on a form field appearance canvas |
| `IDrawable.CurrentPos` | The same property through the drawing interface |
| `HpdfPage.CreateLineAnnotation(HpdfPoint start, HpdfPoint end, string text)`, `HpdfLineAnnotation.SetPosition(start, end)` | The points of a line annotation ([HpdfLineAnnotation](../annotations/HpdfLineAnnotation.md)) |
| `HpdfFreeTextAnnotation.SetCallout(...)` | The points of a callout line ([HpdfFreeTextAnnotation](../annotations/HpdfFreeTextAnnotation.md)) |
| `HpdfTextMarkupAnnotation.SetQuadPoints(leftBottom, rightBottom, leftTop, rightTop)` | The corners of rotated marked text ([HpdfTextMarkupAnnotation](../annotations/HpdfTextMarkupAnnotation.md)) |
| `HpdfPage.CreatePolygonAnnotation(HpdfPoint[] vertices, string text)`, `CreatePolyLineAnnotation(...)`, `HpdfPolygonalAnnotation.SetVertices(...)`, `Vertices` | The vertices of a polygon or polyline ([HpdfPolygonalAnnotation](../annotations/HpdfPolygonalAnnotation.md)) |
| `HpdfPage.CreateInkAnnotation(HpdfPoint[][] strokes, string text)`, `HpdfInkAnnotation.SetInkList(...)`, `Strokes` | The points of freehand strokes ([HpdfInkAnnotation](../annotations/HpdfInkAnnotation.md)) |
| `HpdfRedactAnnotation.SetQuadPoints(leftBottom, rightBottom, leftTop, rightTop)` | The corners of a rotated area to redact ([HpdfRedactAnnotation](../annotations/HpdfRedactAnnotation.md)) |

The path methods of [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) update `CurrentPos`: `MoveTo` and
`LineTo` set it to their end point, and the `CurveTo` methods set it to the curve's end point. The path methods take
coordinates, not points; the annotation methods above take `HpdfPoint` parameters.

---

## Getting an Instance

Create one with the constructor, or read `CurrentPos` from a page:

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var p = new HpdfPoint(100, 200);
HpdfPoint current = page.CurrentPos;   // (0, 0) on a new page
```

`default(HpdfPoint)` is (0, 0).

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.MoveTo(50, 50);
page.LineTo(200, 120);

HpdfPoint end = page.CurrentPos;
Console.WriteLine(end);                              // (200, 120)
Console.WriteLine(end == new HpdfPoint(200, 120));   // True

page.Stroke();
pdf.SaveToFile("point.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [X](#property-x) | `float` | Horizontal coordinate |
| [Y](#property-y) | `float` | Vertical coordinate |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfPoint(float, float)](#constructor-hpdfpoint) | Creates a point |
| [Equals()](#method-equals) | Compares X and Y |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from X and Y |
| [ToString()](#method-tostring) | Text form `(x, y)` |

---

## Detailed Descriptions

#### Property: *X*

**Declaration:**
```csharp
public float X { get; set; }
```

**Description:**
Horizontal coordinate in points.

---

#### Property: *Y*

**Declaration:**
```csharp
public float Y { get; set; }
```

**Description:**
Vertical coordinate in points.

---

#### Constructor: *HpdfPoint()*

**Declaration:**
```csharp
public HpdfPoint(float x, float y)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| x | `float` | Horizontal coordinate |
| y | `float` | Vertical coordinate |

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfPoint other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when X and Y are equal (exact `float` comparison).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfPoint left, HpdfPoint right)
public static bool operator !=(HpdfPoint left, HpdfPoint right)
```

**Description:**
Same comparison as `Equals(HpdfPoint)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of X and Y.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
A string such as `(200, 120)`, using the default `float` formatting of the invariant culture (the text does not
depend on the current culture).

---

[Back to Index](../../INDEX.md)
