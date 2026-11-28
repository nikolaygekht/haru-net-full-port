# HpdfCanvas Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

**Implements:** `IDrawable` (`Haru.Graphics`)

---

## Purpose

`HpdfCanvas` is what pages and form XObjects have in common: the content stream drawing operators are written to,
the graphics and text state tracked while drawing, and the resources (fonts, images, extended graphics states,
color spaces, form XObjects) the content refers to by name. The drawing methods of
[HpdfPageText](../extensions/HpdfPageText.md), [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) and
[HpdfPageExtensions](../extensions/HpdfPageExtensions.md) are extension methods on `HpdfCanvas` (or on `IDrawable`),
so everything that can be drawn on a page can be drawn into a form XObject the same way.

---

## Getting an Instance

`HpdfCanvas` is abstract and cannot be derived from outside the library. Its two classes are:

| Class | Created by |
|-------|------------|
| [HpdfPage](HpdfPage.md) | `HpdfDocument.AddPage()` |
| [HpdfFormXObject](HpdfFormXObject.md) | `HpdfDocument.CreateFormXObject(width, height)` |

A helper written against `HpdfCanvas` works for both:

```csharp
using Haru.Doc;
using Haru.Font;

static void DrawLabel(HpdfCanvas canvas, HpdfFont font, float x, float y, string text)
{
    canvas.SetFontAndSize(font, 10);
    canvas.TextOut(x, y, text);
}
```

---

## Example

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var stamp = pdf.CreateFormXObject(120, 30);
var page = pdf.AddPage();

foreach (HpdfCanvas canvas in new HpdfCanvas[] { stamp, page })
{
    canvas.SetFontAndSize(font, 12);
    canvas.TextOut(5, 10, canvas is HpdfPage ? "On the page" : "In the form");
}

page.DrawFormXObject(stamp, 50, 700);
pdf.SaveToFile("canvas.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Contents](#property-contents) | `HpdfStreamObject` | The content stream operators are written to |
| [Stream](#property-stream) | `HpdfMemoryStream` | The data of `Contents` |
| [GraphicsState](#property-graphicsstate) | `HpdfGraphicsState` | The tracked graphics state |
| [CurrentPos](#property-currentpos) | `HpdfPoint` | The current point of the path |
| [PathInProgress](#property-pathinprogress) | `bool` | Whether a path is being built |
| [InTextObject](#property-intextobject) | `bool` | Whether a text object is open |
| [TextMatrix](#property-textmatrix) | `HpdfTransMatrix` | The current text matrix |
| [TextLineMatrix](#property-textlinematrix) | `HpdfTransMatrix` | The text matrix at the start of the current line |
| [CurrentFont](#property-currentfont) | `HpdfFont?` | The font set with `SetFontAndSize` |

---

## Methods

| Name | Description |
|------|-------------|
| [GetResources()](#method-getresources) | Returns the resource dictionary |
| [PushGraphicsState()](#method-pushgraphicsstate--popgraphicsstate) | Called by `GSave` |
| [PopGraphicsState()](#method-pushgraphicsstate--popgraphicsstate) | Called by `GRestore` |

---

## Detailed Descriptions

#### Property: *Contents*

**Declaration:**
```csharp
public HpdfStreamObject Contents { get; }
```

**Description:**
The content stream that drawing operators are written to now. A form XObject has one stream (its own); a page has
one until `NewContentStream` starts another ([HpdfContentStream](HpdfContentStream.md)), and `Contents` is then the
current one.

---

#### Property: *Stream*

**Declaration:**
```csharp
public HpdfMemoryStream Stream { get; }
```

**Description:**
The data of `Contents`, which the drawing methods write to.

---

#### Property: *GraphicsState*

**Declaration:**
```csharp
public HpdfGraphicsState GraphicsState { get; }
```

**Description:**
The graphics state as the drawing methods tracked it: line attributes, colors (also ICC colors), font and size,
text state, transformation, and the depth of the GSave stack. `GSave` pushes a copy, `GRestore` brings the previous
one back. A form XObject starts with the default state: set the font and colors inside it.

---

#### Property: *CurrentPos*

**Declaration:**
```csharp
public HpdfPoint CurrentPos { get; set; }
```

**Description:**
The current point of the path, set by the path construction methods.

---

#### Property: *PathInProgress*

**Declaration:**
```csharp
public bool PathInProgress { get; set; }
```

**Description:**
Whether a path is being built: set by `MoveTo`, `LineTo`, `Rectangle`, ..., cleared by the painting methods. Form
XObjects and shared content streams cannot be drawn or inserted while a path is built.

---

#### Property: *InTextObject*

**Declaration:**
```csharp
public bool InTextObject { get; }
```

**Description:**
Whether a text object is open: `BeginText` (also inside `TextOut`) sets it, `EndText` clears it. Saving fails for a
form XObject with an open text object; form XObjects and shared content streams cannot be drawn or inserted inside
one.

---

#### Property: *TextMatrix*

**Declaration:**
```csharp
public HpdfTransMatrix TextMatrix { get; }
```

**Description:**
The current text matrix: reset to the identity by `BeginText`, set by `SetTextMatrix`, moved by the positioning
methods.

---

#### Property: *TextLineMatrix*

**Declaration:**
```csharp
public HpdfTransMatrix TextLineMatrix { get; }
```

**Description:**
The text matrix at the start of the current line, which `MoveTextPos` and `MoveToNextLine` move from.

---

#### Property: *CurrentFont*

**Declaration:**
```csharp
public HpdfFont? CurrentFont { get; }
```

**Description:**
The font set with `SetFontAndSize`, part of the graphics state (`GRestore` brings back the previous one); `null`
before a font is set.

---

#### Method: *GetResources()*

**Declaration:**
```csharp
public abstract HpdfDict GetResources()
```

**Return Value:**
The resource dictionary of the page (`/Resources` of the page) or of the form XObject (`/Resources` of its stream).
The drawing methods add what they use under its resource name; a name refers to one object, and a second, different
object under a name already in use throws `HpdfException` (`DuplicateRegistration`).

---

#### Method: *PushGraphicsState() / PopGraphicsState()*

**Declaration:**
```csharp
public void PushGraphicsState()
public void PopGraphicsState()
```

**Description:**
Push a copy of the graphics state and bring the previous one back; called by `GSave` and `GRestore`
([HpdfPageGraphics](../extensions/HpdfPageGraphics.md)), which also write the `q` and `Q` operators. Call `GSave` and
`GRestore` rather than these.

---

## Related Types

- [HpdfPage](HpdfPage.md), [HpdfFormXObject](HpdfFormXObject.md) - the two canvases
- [HpdfPageText](../extensions/HpdfPageText.md), [HpdfPageGraphics](../extensions/HpdfPageGraphics.md),
  [HpdfPageExtensions](../extensions/HpdfPageExtensions.md), [HpdfPageShapes](../extensions/HpdfPageShapes.md) - the drawing methods
- [Graphics Guide](../../guides/GraphicsGuide.md) - "Reusing Content"

---

[Back to Index](../../INDEX.md)
