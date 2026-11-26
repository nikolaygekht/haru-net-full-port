# HpdfPageExtensions Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPageExtensions` is a static class of convenience extension methods for [HpdfPage](../core/HpdfPage.md), most of
them named after functions of the C library: one-call text output and measuring, destinations, reading the current
font and colors, a six-number `SetTextMatrix`, painting an image in the current transformation and setting the page
size.

---

## Getting an Instance

The class is static. Its methods are extension methods: with `using Haru.Doc;` in place, call them on a page.

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
page.SetFontAndSize(pdf.GetFont("Helvetica"), 12);
page.TextOut(50, 750, "Hello");
```

---

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.SetFontAndSize(pdf.GetFont("Helvetica-Bold"), 18);

// Center a line of text horizontally
const string heading = "Quarterly Summary";
float width = page.TextWidth(heading);
page.TextOut((page.Width - width) / 2, 780, heading);

// A bookmark to this page
var destination = page.CreateDestination();
destination.SetFit();
pdf.CreateOutline("Summary").SetDestination(destination);

pdf.SaveToFile("page-extensions.pdf");
```

---

## Properties

`HpdfPageExtensions` is a static class and has no properties.

---

## Methods

| Name | Description |
|------|-------------|
| [CreateDestination()](#method-createdestination) | Creates a destination for the page |
| [TextOut()](#method-textout) | Writes a line of text at a position in its own text object |
| [TextWidth()](#method-textwidth) | Measures text in the current font and size |
| [GetCurrentFontSize()](#method-getcurrentfontsize) | Returns the current font size |
| [GetCurrentFont()](#method-getcurrentfont) | Returns the current font, or `null` |
| [GetRGBFill()](#method-getrgbfill) | Returns the last RGB fill color |
| [GetRGBStroke()](#method-getrgbstroke) | Returns the last RGB stroke color |
| [SetTextMatrix()](#method-settextmatrix) | Sets the text matrix from six numbers |
| [ExecuteXObject()](#method-executexobject) | Paints an image in the current transformation (`Do` operator) |
| [SetWidth()](#method-setwidth) | Sets the page width |
| [SetHeight()](#method-setheight) | Sets the page height |

---

## Detailed Descriptions

#### Method: *CreateDestination()*

**Declaration:**
```csharp
public static HpdfDestination CreateDestination(this HpdfPage page)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The target page |

**Return Value:**
A new [HpdfDestination](../core/HpdfDestination.md) for the page, without a view yet.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `page` is `null` |

**Description:**
Same as `new HpdfDestination(page)`. Set the view (`SetXYZ`, `SetFit`, ...) before using the destination for a
bookmark, a link or the open action.

**Example:**
```csharp
var destination = page.CreateDestination();
destination.SetXYZ(0, page.Height, 1);
pdf.Catalog.SetOpenAction(destination);
```

---

#### Method: *TextOut()*

**Declaration:**
```csharp
public static void TextOut(this HpdfPage page, float x, float y, string text)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The page |
| x | `float` | Horizontal position of the start of the baseline |
| y | `float` | Vertical position of the baseline |
| text | `string` | The text to write; `null` or empty writes no text |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |
| `HpdfException` (`PageFontNotFound`) | No font has been set with `SetFontAndSize`, also when `text` is empty |

**Description:**
Calls `BeginText()`, `MoveTextPos(x, y)`, `ShowText(text)` and `EndText()` (see
[HpdfPageText](HpdfPageText.md)), so it writes `BT x y Td (text) Tj ET`. Because `BeginText` resets the text matrix,
`x` and `y` are coordinates in the current user space (by default points from the lower-left corner of the page).

Call it outside a text object: text objects cannot be nested, and calling `TextOut` between `BeginText` and `EndText`
writes an invalid content stream. The font is checked before anything is written: when no font is set, the
exception leaves the content stream unchanged.

**Example:**
```csharp
page.SetFontAndSize(pdf.GetFont("Times-Roman"), 12);
page.TextOut(72, 720, "First line");
page.TextOut(72, 704, "Second line");
```

---

#### Method: *TextWidth()*

**Declaration:**
```csharp
public static float TextWidth(this HpdfPage page, string text)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The page |
| text | `string` | The text to measure |

**Return Value:**
The width of `text` in the current font and font size, in user space units; `0` for `null` or empty text.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |
| `HpdfException` (`PageFontNotFound`) | No font has been set with `SetFontAndSize` |

**Description:**
Same as `page.GetCurrentFont().MeasureText(text, page.GetCurrentFontSize())`: the sum of the glyph widths scaled to
the font size. Character spacing, word spacing and horizontal scaling set on the page are not included.

**Example:**
```csharp
page.SetFontAndSize(pdf.GetFont("Helvetica"), 10);
const string price = "1,234.50";
float right = 500;
page.TextOut(right - page.TextWidth(price), 600, price);   // right-aligned at x = 500
```

---

#### Method: *GetCurrentFontSize()*

**Declaration:**
```csharp
public static float GetCurrentFontSize(this HpdfPage page)
```

**Return Value:**
The font size set with the last `SetFontAndSize` call in the current graphics state; `0` before any font is set.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |

**Description:**
Reads `page.GraphicsState.FontSize`. The value is part of the graphics state, so `GRestore()` restores the size that
was current at the matching `GSave()`.

---

#### Method: *GetCurrentFont()*

**Declaration:**
```csharp
public static HpdfFont? GetCurrentFont(this HpdfPage page)
```

**Return Value:**
The current [HpdfFont](../core/HpdfFont.md), or `null` before any font is set.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |

**Description:**
Same as `page.CurrentFont`. Like the size, the font is part of the graphics state and follows `GSave()`/`GRestore()`.

**Example:**
```csharp
if (page.GetCurrentFont() is null)
    page.SetFontAndSize(pdf.GetFont("Helvetica"), 12);
```

---

#### Method: *GetRGBFill()*

**Declaration:**
```csharp
public static HpdfRgbColor GetRGBFill(this HpdfPage page)
```

**Return Value:**
The last fill color set with `SetRgbFill` in the current graphics state, as an [HpdfRgbColor](../types/HpdfRgbColor.md);
black `(0, 0, 0)` initially.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |

**Description:**
Reads `page.GraphicsState.RgbFill`. Setting a gray or CMYK fill color does not change this value, so check
`page.GraphicsState.FillColorSpace` if the page may use other color spaces.

**Example:**
```csharp
page.SetRgbFill(0.2f, 0.4f, 0.8f);
var fill = page.GetRGBFill();
Console.WriteLine($"{fill.R} {fill.G} {fill.B}");   // 0.2 0.4 0.8
```

---

#### Method: *GetRGBStroke()*

**Declaration:**
```csharp
public static HpdfRgbColor GetRGBStroke(this HpdfPage page)
```

**Return Value:**
The last stroke color set with `SetRgbStroke` in the current graphics state; black `(0, 0, 0)` initially.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |

**Description:**
Reads `page.GraphicsState.RgbStroke`. Gray and CMYK stroke colors do not change it (see `GraphicsState.StrokeColorSpace`).

---

#### Method: *SetTextMatrix()*

**Declaration:**
```csharp
public static void SetTextMatrix(this HpdfPage page, float a, float b, float c, float d, float x, float y)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The page |
| a, b, c, d | `float` | The scaling, rotation and skew part of the matrix |
| x, y | `float` | The translation: where the text origin is placed |

**Return Value:**
`void`

**Description:**
Same as `page.SetTextMatrix(new HpdfTransMatrix(a, b, c, d, x, y))` from [HpdfPageText](HpdfPageText.md):
writes the `Tm` operator and sets the text matrix and the text line matrix. Call it between `BeginText()` and
`EndText()`.

**Example:**
```csharp
// Text rotated by 45 degrees around (200, 400)
float angle = MathF.PI / 4;
page.SetFontAndSize(pdf.GetFont("Helvetica"), 16);
page.BeginText();
page.SetTextMatrix(MathF.Cos(angle), MathF.Sin(angle), -MathF.Sin(angle), MathF.Cos(angle), 200, 400);
page.ShowText("Rotated text");
page.EndText();
```

---

#### Method: *ExecuteXObject()*

**Declaration:**
```csharp
public static void ExecuteXObject(this HpdfPage page, HpdfImage image)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The page |
| image | [HpdfImage](../core/HpdfImage.md) | The image to paint |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |
| `HpdfException` (`InvalidParameter`) | `image` is `null` |
| `HpdfException` (`DuplicateRegistration`) | The page already uses a different image under the same `LocalName` |

**Description:**
Adds the image to the page resources (once) and writes `/Name Do`. An image is painted into the unit square
(0, 0)-(1, 1) of the current transformation, so without a transformation it is one point large. Scale and position it
with `Concat` first, inside `GSave()`/`GRestore()`, as in the example. `page.DrawImage(image, x, y, width, height)`
from [HpdfPageGraphics](HpdfPageGraphics.md) does all of this in one call; use `ExecuteXObject` when you need
another transformation, such as rotation.

**Example:**
```csharp
using var image = pdf.LoadPngImageFromFile("images/logo.png");

// Paint the image 100 x 100 points large, rotated by 30 degrees around its lower-left corner at (200, 500)
float angle = 30 * MathF.PI / 180;
page.GSave();
page.Concat(1, 0, 0, 1, 200, 500);                                                   // move
page.Concat(MathF.Cos(angle), MathF.Sin(angle), -MathF.Sin(angle), MathF.Cos(angle), 0, 0);   // rotate
page.Concat(100, 0, 0, 100, 0, 0);                                                   // scale
page.ExecuteXObject(image);
page.GRestore();
```

---

#### Method: *SetWidth()*

**Declaration:**
```csharp
public static void SetWidth(this HpdfPage page, float width)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The page |
| width | `float` | The page width in points |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |
| `HpdfException` (`InvalidParameter`) | `width` is not greater than 0, greater than 100000, `NaN` or infinite |

**Description:**
Same as assigning `page.Width`: changes the right edge of the media box and keeps its lower-left corner. It exists for
compatibility with `HPDF_Page_SetWidth` of the C library.

**Example:**
```csharp
page.SetWidth(612);    // US Letter width
page.SetHeight(792);   // US Letter height
```

---

#### Method: *SetHeight()*

**Declaration:**
```csharp
public static void SetHeight(this HpdfPage page, float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](../core/HpdfPage.md) | The page |
| height | `float` | The page height in points |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `page` is `null` |
| `HpdfException` (`InvalidParameter`) | `height` is not greater than 0, greater than 100000, `NaN` or infinite |

**Description:**
Same as assigning `page.Height`: changes the top edge of the media box and keeps its lower-left corner. Set the size
before drawing: content already drawn keeps its coordinates, which are measured from the lower-left corner.

---

## Related Types

- [HpdfPage](../core/HpdfPage.md) - The page class itself
- [HpdfPageText](HpdfPageText.md) - Text operations (`BeginText`, `ShowText`, `SetTextMatrix`, ...)
- [HpdfPageGraphics](HpdfPageGraphics.md) - Graphics operations (`Concat`, `DrawImage`, colors, ...)
- [HpdfDestination](../core/HpdfDestination.md) - Created by `CreateDestination()`
- [Text Guide](../../guides/TextGuide.md)

---

[Back to Index](../../INDEX.md)
