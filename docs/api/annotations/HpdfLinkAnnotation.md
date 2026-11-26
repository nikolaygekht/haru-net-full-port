# HpdfLinkAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfLinkAnnotation` is a clickable area on a page. It either opens a web address (a URI action) or jumps to a place
in the same document (a destination). The link is only the clickable rectangle: the text or picture that shows the
reader where to click is drawn on the page separately.

---

## Getting an Instance

Create links with the page they are on:

| Method | Description |
|--------|-------------|
| `HpdfPage.CreateLinkAnnotation(HpdfRect rect, string uri)` | A link that opens `uri`, for example `https://example.com/` or `mailto:info@example.com` |
| `HpdfPage.CreateLinkAnnotation(HpdfRect rect, HpdfDestination destination)` | A link that shows a page of this document |
| `HpdfPage.CreateLinkAnnotation(HpdfRect rect, HpdfArray destination)` | The same, with a destination array such as `HpdfDestination.DestArray` |

Each method creates the annotation and adds it to the page's `/Annots` array. The constructors of the class are
internal: a link is always created by the page it is on.

The rectangle is `new HpdfRect(left, bottom, right, top)` in points from the lower-left corner of the page. Reversed
edges are swapped: a top below the bottom, or a right edge left of the left edge, gives the same rectangle.

**Web links.** The URI overload writes a URI action: `/A << /Type /Action /S /URI /URI (...) >>`. A URI action must
be 7-bit ASCII, so characters outside ASCII (an internationalized domain or path) are percent-encoded as UTF-8 bytes:
`https://example.com/café` is written as `https://example.com/caf%C3%A9`. ASCII characters, including spaces and `%`,
are written unchanged, so encode them yourself where the address needs it. The address is otherwise not checked.

**Links inside the document.** The destination overloads write the destination array as `/Dest`. The array is stored,
not copied: changing the `HpdfDestination` later (for example with another `SetFit...` call) changes the link too.
The destination needs a view: call one of the `Set...` methods of [HpdfDestination](../core/HpdfDestination.md)
before creating the link. A destination without a view (`[page]` only) is rejected.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `uri` is `null` or empty; `destination` is `null` |
| `HpdfException` (`InvalidDestination`) | The destination has no view (`[page]` only) |

The arguments are checked before the annotation is created, so a call that throws adds nothing to the page or the
document.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page1 = pdf.AddPage();
var page2 = pdf.AddPage();

page1.SetFontAndSize(font, 12);
page1.TextOut(50, 705, "Visit example.com");
page1.TextOut(50, 665, "Go to page 2");

// A web link around the first line
var web = page1.CreateLinkAnnotation(new HpdfRect(48, 700, 160, 718), "https://example.com");
web.SetBorderStyle(HpdfBorderStyle.Solid, 0);

// A link to the top of page 2
var target = page2.CreateDestination();
target.SetXYZ(0, page2.Height, 1);
var internalLink = page1.CreateLinkAnnotation(new HpdfRect(48, 660, 140, 678), target);
internalLink.SetBorderStyle(HpdfBorderStyle.Underlined, 1);
internalLink.SetRgbColor(0f, 0f, 1f);
internalLink.SetHighlightMode(HpdfHighlightMode.InvertBorder);

pdf.SaveToFile("links.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Link` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetHighlightMode()](#method-sethighlightmode) | Sets how the link is highlighted while it is clicked (`/H`) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Sets the border style and width (`/BS`) (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) | Sets the border color in RGB (`/C`) (inherited) |
| [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor) | Sets the border color in CMYK (`/C`) (inherited) |

---

## Detailed Descriptions

#### Method: *SetHighlightMode()*

**Declaration:**
```csharp
public void SetHighlightMode(HpdfHighlightMode mode)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| mode | `HpdfHighlightMode` | How the viewer shows the link while the mouse button is pressed on it |

**Return Value:**
`void`

**Description:**
Writes `/H`: `/N` (`NoHighlight`), `/I` (`InvertBox`), `/O` (`InvertBorder`) or `/P` (`DownAppearance`); see
[HpdfHighlightMode](HpdfHighlightMode.md). A later call replaces the mode. Without a call, `/H` is absent and viewers
use `InvertBox`, the PDF default. A link has no appearance streams, so `DownAppearance` has nothing to show.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `mode` is not an `HpdfHighlightMode` value |

**Example:**
```csharp
link.SetHighlightMode(HpdfHighlightMode.NoHighlight);
```

---

## Related Types

- [HpdfAnnotation](HpdfAnnotation.md) - base class: border and color
- [HpdfDestination](../core/HpdfDestination.md) - the target of a link inside the document
- [HpdfHighlightMode](HpdfHighlightMode.md), [HpdfBorderStyle](HpdfBorderStyle.md)
- [HpdfPage](../core/HpdfPage.md) - `CreateLinkAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
