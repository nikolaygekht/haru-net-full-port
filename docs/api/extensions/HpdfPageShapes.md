# HpdfPageShapes Class

[Back to Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

**Kind:** static class with extension methods

---

## Overview

`HpdfPageShapes` adds circles, ellipses and arcs to the current path. The shapes are approximated with cubic Bezier curves, built from the `MoveTo` and `CurveTo` operations of [HpdfPageGraphics](HpdfPageGraphics.md).

Like the graphics operations, the methods extend `IDrawable`, so they work on an `HpdfPage` and on an `HpdfAppearanceCanvas` (see [IDrawable](HpdfPageGraphics.md#idrawable-pages-and-appearance-canvases)).

### Shapes Build Paths Only

The methods only **construct** a path; nothing appears on the page until the path is painted with a method such as `Stroke`, `Fill`, `FillStroke` or `EoFill`, or used as a clipping path. Colors and line attributes must therefore be set **before** the shape method is called.

### Basic Usage

```csharp
using Haru.Doc;

var doc = new HpdfDocument();
var page = doc.AddPage();

page.SetLineWidth(2);
page.SetRgbStroke(0, 0, 0);
page.SetRgbFill(0.6f, 0.8f, 1);

page.Circle(150, 600, 50);
page.FillStroke();

page.Ellipse(350, 600, 80, 40);
page.Stroke();

page.Arc(250, 400, 60, 0, 90);
page.Stroke();

doc.SaveToFile("shapes.pdf");
```

---

## Method Summary

| Method | Description |
|--------|-------------|
| [Circle](#circle) | Appends a circle as a new subpath |
| [Ellipse](#ellipse) | Appends an axis-aligned ellipse as a new subpath |
| [Arc](#arc) | Appends a circular arc as a new subpath |

---

## Methods

### Circle

Appends a circle centered at `(x, y)`.

```csharp
public static void Circle(this IDrawable drawable, float x, float y, float radius)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `x` | `float` | X coordinate of the center |
| `y` | `float` | Y coordinate of the center |
| `radius` | `float` | Radius, `> 0` |

**Throws:** `HpdfException` (`PageOutOfRange`) when `radius` is zero or negative.

**Description:** Starts a new subpath with `MoveTo` at the leftmost point `(x - radius, y)` and appends four quarter-circle Bezier curves, clockwise: left, top, right, bottom and back to the left. The subpath ends where it started but is not closed with `ClosePath`; use `ClosePathStroke` or `ClosePathFillStroke` if the start point should get a line join. The current point afterwards is `(x - radius, y)`.

**Example:**

```csharp
// Outline
page.SetRgbStroke(0.8f, 0, 0);
page.SetLineWidth(3);
page.Circle(150, 500, 50);
page.Stroke();

// Filled with outline
page.SetRgbFill(1, 0.9f, 0.6f);
page.Circle(300, 500, 50);
page.FillStroke();
```

### Ellipse

Appends an ellipse centered at `(x, y)` whose axes are parallel to the x and y axes.

```csharp
public static void Ellipse(this IDrawable drawable, float x, float y, float xRadius, float yRadius)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `x` | `float` | X coordinate of the center |
| `y` | `float` | Y coordinate of the center |
| `xRadius` | `float` | Horizontal radius, `> 0` |
| `yRadius` | `float` | Vertical radius, `> 0` |

**Throws:** `HpdfException` (`PageOutOfRange`) when `xRadius` or `yRadius` is zero or negative.

**Description:** Built like `Circle`: a new subpath starting at the leftmost point `(x - xRadius, y)`, four Bezier curves clockwise, not explicitly closed. The current point afterwards is `(x - xRadius, y)`. For a rotated ellipse, rotate the coordinate system with `Concat` first.

**Example:**

```csharp
// Horizontal ellipse
page.Ellipse(200, 400, 100, 40);
page.Stroke();

// Ellipse rotated by 45 degrees around its center (300, 250)
page.GSave();
page.Concat(0.707f, 0.707f, -0.707f, 0.707f, 300, 250);
page.Ellipse(0, 0, 80, 30);
page.Stroke();
page.GRestore();
```

### Arc

Appends a circular arc centered at `(x, y)` from `angle1` to `angle2`.

```csharp
public static void Arc(this IDrawable drawable, float x, float y, float radius, float angle1, float angle2)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `x` | `float` | X coordinate of the center |
| `y` | `float` | Y coordinate of the center |
| `radius` | `float` | Radius, `> 0` |
| `angle1` | `float` | Angle of the start point, in degrees |
| `angle2` | `float` | Angle of the end point, in degrees |

**Throws:**
- `HpdfException` (`PageOutOfRange`) when `radius` is zero or negative.
- `HpdfException` (`PageOutOfRange`) when the arc spans 360 degrees or more (`|angle2 - angle1| >= 360`). Use `Circle` for a full circle.

#### Angle Convention

Angles are measured in degrees **clockwise from the top** (the positive y axis), like the hours on a clock face:

| Angle | Point on the arc | Clock position |
|-------|------------------|----------------|
| 0 | `(x, y + radius)` | 12 o'clock |
| 90 | `(x + radius, y)` | 3 o'clock |
| 180 | `(x, y - radius)` | 6 o'clock |
| 270 | `(x - radius, y)` | 9 o'clock |

The point for an angle `a` is `(x + radius * sin(a), y + radius * cos(a))`.

- The arc runs from `angle1` to `angle2`. When `angle2 > angle1`, it runs **clockwise**; when `angle2 < angle1`, it runs **counterclockwise**. `Arc(x, y, r, 0, 90)` and `Arc(x, y, r, 90, 0)` produce the same quarter circle (top to right) in opposite directions.
- Negative angles are allowed; both angles are shifted by 360 until neither is negative, which does not change the arc. `Arc(x, y, r, -90, 0)` runs from 9 o'clock to 12 o'clock.
- Angles above 360 are not normalized but describe the same points (`Arc(x, y, r, 270, 450)` runs from 9 o'clock over 12 to 3 o'clock).

**Note:** The XML documentation comment of `Arc` says that angles are measured counterclockwise from the positive x axis. That does not match the implementation; the convention above is what the method does, and it is the convention of the original Haru C library (`HPDF_Page_Arc`).

#### Path Behavior

- The arc is split into segments of at most 90 degrees, each drawn as one Bezier curve.
- The arc **always starts a new subpath** with `MoveTo` at the start point. It is not connected to a path built before it; a preceding `MoveTo` (for example to the center of a pie slice) only creates an empty subpath and is not joined to the arc.
- The current point afterwards is the end point of the arc, so `LineTo` can continue from it.
- `angle1` and `angle2` must differ. Equal angles are not rejected, but produce a degenerate curve whose control points are written as `(0, 0)`, so it can be drawn towards the origin of the coordinate system.

**Example - open arcs:**

```csharp
page.SetRgbStroke(0.8f, 0, 0);
page.SetLineWidth(2);

page.Arc(100, 500, 40, 0, 90);     // 12 to 3 o'clock
page.Stroke();

page.Arc(220, 500, 40, 0, 180);    // right half
page.Stroke();

page.Arc(340, 500, 40, 270, 450);  // upper half, 9 over 12 to 3 o'clock
page.Stroke();
```

**Example - pie slices:** Because `Arc` starts its own subpath, draw the arc first, then the line to the center, and let a "close" painting method draw the second radius:

```csharp
float cx = 300, cy = 300, r = 80;
float[] shares = { 0.5f, 0.3f, 0.2f };
float[][] colors =
{
    new[] { 1f, 0.7f, 0.7f },
    new[] { 0.7f, 1f, 0.7f },
    new[] { 0.7f, 0.7f, 1f },
};

page.SetRgbStroke(0, 0, 0);
page.SetLineWidth(1);

float start = 0;
for (int i = 0; i < shares.Length; i++)
{
    float end = start + shares[i] * 360;

    page.SetRgbFill(colors[i][0], colors[i][1], colors[i][2]);
    page.Arc(cx, cy, r, start, end);   // new subpath along the rim
    page.LineTo(cx, cy);               // to the center
    page.ClosePathFillStroke();        // back to the start of the arc

    start = end;
}
```

---

## Notes

- All three methods append a **new** subpath; several shapes can be combined into one path and painted together. With nested shapes the fill rule matters: two concentric circles filled with `Fill` give a solid disc, with `EoFill` a ring (see [HpdfPageGraphics](HpdfPageGraphics.md#path-painting-methods)).
- Shapes can serve as clipping paths: call `Clip` (or `EoClip`) and `EndPath` after the shape, inside `GSave` / `GRestore`.
- Like the other drawing methods, the shape methods throw `ArgumentNullException` when the target is `null`.

---

## See Also

- [HpdfPageGraphics](HpdfPageGraphics.md) - Paths, painting, colors and transformations
- [HpdfPage](../core/HpdfPage.md) - Pages
- [HpdfPageText](HpdfPageText.md) - Text operations
- [Getting Started](../../guides/GettingStarted.md) - Step-by-step tutorial

---

[Back to Index](../../INDEX.md)
