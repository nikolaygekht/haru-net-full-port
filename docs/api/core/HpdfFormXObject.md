# HpdfFormXObject Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

**Base class:** [HpdfCanvas](HpdfCanvas.md)

---

## Purpose

`HpdfFormXObject` is a form XObject: content drawn once - text, paths, images, other forms - that pages and other
forms show any number of times, at any size: a letterhead, a watermark, a logo, a stamp. The file holds it once
however often it is shown, which keeps documents with repeated content small. Draw into it with the same methods as
on a page.

---

## Getting an Instance

| Method | Creates |
|--------|---------|
| `HpdfDocument.CreateFormXObject(float width, float height)` | An empty form with the box (0, 0)-(width, height) |
| `HpdfDocument.CreateFormXObjectFromImage(HpdfImage image, HpdfRect rect, bool zoom)` | A form that shows an image (`HPDF_Page_CreateXObjectFromImage`) |
| `HpdfDocument.CreateWhiteRectFormXObject(HpdfRect rect)` | A form filled with white (`HPDF_Page_CreateXObjectAsWhiteRect`) |

`HpdfDocument.FormXObjects` lists the forms of the document. A form is shown with
`DrawFormXObject(form, x, y)`, `DrawFormXObject(form, x, y, width, height)` or `ExecuteXObject(form)`
([HpdfPageExtensions](../extensions/HpdfPageExtensions.md)) on a page or another form.

**The form's own coordinates.** Its content uses the coordinates of its box, from (0, 0) to (`Width`, `Height`);
what is drawn outside the box is cut off. `DrawFormXObject` places the box with its lower-left corner at (x, y) and
scales it to the given size.

**Graphics state.** A form inherits the state of the place where it is shown (the transformation, and colors and
line width it does not set), but the library tracks its state from the defaults: set the font, and the colors you
need, inside the form. Like a page, a form must end with balanced `GSave`/`GRestore` and no open text object;
saving checks it.

**Forms in forms.** A form can show other forms. It cannot show itself, directly or through other forms; that, and a
form of another document, throws `HpdfException` (`InvalidParameter`).

**The C helpers.** `CreateFormXObjectFromImage` makes a form of the rectangle's size with the image scaled to fill it
(`zoom: true`) or drawn at its pixel size, one point per pixel (`zoom: false`). The C library also offsets the box by
the rectangle's corner and, without zoom, draws the image into a 1 x 1 unit square; here the box starts at (0, 0).

---

## Example

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");

var letterhead = pdf.CreateFormXObject(515, 60);
letterhead.SetRgbFill(0.1f, 0.3f, 0.6f);
letterhead.Rectangle(0, 0, 515, 60);
letterhead.Fill();
letterhead.SetRgbFill(1, 1, 1);
letterhead.SetFontAndSize(font, 24);
letterhead.TextOut(15, 20, "Example Inc.");

for (int i = 0; i < 3; i++)
{
    var page = pdf.AddPage();
    page.DrawFormXObject(letterhead, 40, page.Height - 100);                 // full size
    page.DrawFormXObject(letterhead, 400, 40, 515 / 4f, 60 / 4f);           // a quarter
}

pdf.SaveToFile("letterhead.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [LocalName](#property-localname) | `string` | The resource name on the canvases that show the form |
| [Width](#property-width) | `float` | The width of the box |
| [Height](#property-height) | `float` | The height of the box |
| [StreamObject](#property-streamobject) | `HpdfStreamObject` | The form XObject stream |

The drawing properties (`GraphicsState`, `CurrentFont`, `InTextObject`, ...) are those of [HpdfCanvas](HpdfCanvas.md).

---

## Detailed Descriptions

#### Property: *LocalName*

**Declaration:**
```csharp
public string LocalName { get; }
```

**Description:**
The name under `/XObject` in the resources of the pages and forms that show it: `HaruFm` followed by a number.

---

#### Property: *Width*

**Declaration:**
```csharp
public float Width { get; }
```

**Description:**
The width of the box (`/BBox`), in the form's own units: points when the form is drawn at full size.

---

#### Property: *Height*

**Declaration:**
```csharp
public float Height { get; }
```

**Description:**
The height of the box.

---

#### Property: *StreamObject*

**Declaration:**
```csharp
public HpdfStreamObject StreamObject { get; }
```

**Description:**
The form XObject stream, an indirect object: `/Type /XObject`, `/Subtype /Form`, `/FormType 1`, `/BBox`,
`/Resources` and the content. It is compressed when the document's compression mode includes `Text`. The same
object as `Contents`.

---

## Remarks

- **PDF/A.** Forms follow the rules of pages: in a PDF/A-1 document, colors in an ICC profile of version 4 make
  saving fail.
- **Text extraction.** Readers that extract text follow form XObjects: the text of a form shown on several pages is
  found on each.

---

## Related Types

- [HpdfCanvas](HpdfCanvas.md) - the drawing surface the form is
- [HpdfDocument](HpdfDocument.md) - `CreateFormXObject()`, `FormXObjects`
- [HpdfPageExtensions](../extensions/HpdfPageExtensions.md) - `DrawFormXObject()`, `ExecuteXObject()`
- [HpdfContentStream](HpdfContentStream.md) - shared content streams, the other way to repeat content
- [Graphics Guide](../../guides/GraphicsGuide.md) - "Reusing Content"

---

[Back to Index](../../INDEX.md)
