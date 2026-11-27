# HpdfCaretAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfCaretAnnotation` is a caret annotation (PDF 1.5; not in the C library): a caret (^) that marks where text is to be
inserted. The text to insert is the text of the annotation, shown in its pop-up window; a paragraph sign beside the
caret asks for a new paragraph. Viewers create carets with their "insert text" tool.

---

## Getting an Instance

Create carets with the page they are on:

```csharp
public HpdfCaretAnnotation CreateCaretAnnotation(HpdfRect rect, string text)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The annotation rectangle; the caret fills it. Reversed edges are swapped |
| text | `string` | The text to insert (`/Contents`), shown in the pop-up window; may be empty |

The method creates the annotation (`/Subtype /Caret`) and adds it to the page's `/Annots` array. The caret is blue
(`/C [0 0 1]`) until another color is set, has no symbol, and the annotation has the Print flag (`/F 4`). Creating one
raises the document version to PDF 1.5. PDF/A-1 does not allow caret annotations: saving a PDF/A document with one
throws `HpdfException` (`InvalidOperation`).

Place the rectangle on the baseline where the text goes, a few points wide and about as high as the small letters:
for 12-point text, `new HpdfRect(x - 4, baseline - 8, x + 4, baseline + 4)` is a usual size.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)): a caret with curved sides, its point at the top
center, filled with the annotation color. With [SetSymbol(Paragraph)](#method-setsymbol) the caret takes the left part
of the box and a pilcrow (¶) is drawn beside it. [SetRectDifference()](#method-setrectdifference) draws the caret in a
smaller box inside the rectangle. [SetNoColor()](HpdfAnnotation.md#method-setnocolor) draws nothing; the border style
is not used.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

page.SetFontAndSize(font, 12);
page.TextOut(50, 700, "Insert the words here and a new paragraph here.");

// Words are missing after "Insert the words"
float x = 50 + font.MeasureText("Insert the words", 12);
var words = page.CreateCaretAnnotation(new HpdfRect(x - 4, 692, x + 4, 704), "that are missing");
words.SetTitle("Editor");

// A new paragraph after "a new paragraph"
float p = 50 + font.MeasureText("Insert the words here and a new paragraph", 12);
var paragraph = page.CreateCaretAnnotation(new HpdfRect(p - 6, 692, p + 10, 704), "");
paragraph.SetSymbol(HpdfCaretSymbol.Paragraph);

pdf.SaveToFile("carets.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Symbol](#property-symbol) | `HpdfCaretSymbol` | The symbol beside the caret; `None` by default |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Caret` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetSymbol()](#method-setsymbol) | Sets the symbol: none or a new paragraph (`/Sy`) |
| [SetRectDifference()](#method-setrectdifference) | Sets the distances from the edges of the rectangle to the caret (`/RD`) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the color of the caret (`/C`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); the generated appearance does not use it |

---

## Detailed Descriptions

#### Property: *Symbol*

**Declaration:**
```csharp
public HpdfCaretSymbol Symbol { get; }
```

**Description:**
The symbol set by [SetSymbol()](#method-setsymbol); `HpdfCaretSymbol.None` until it is called.

---

#### Method: *SetSymbol()*

**Declaration:**
```csharp
public void SetSymbol(HpdfCaretSymbol symbol)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| symbol | `HpdfCaretSymbol` | `None` or `Paragraph` (see [HpdfCaretSymbol](HpdfCaretSymbol.md)) |

**Return Value:**
`void`

**Description:**
Writes `/Sy /None` or `/Sy /P`. With `Paragraph` the generated appearance draws the caret in the left 55% of the box
and a pilcrow in the rest, so make the rectangle about twice as wide as for a caret alone. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `symbol` is not an `HpdfCaretSymbol` value |

**Example:**
```csharp
caret.SetSymbol(HpdfCaretSymbol.Paragraph);
```

---

#### Method: *SetRectDifference()*

**Declaration:**
```csharp
public void SetRectDifference(float left, float top, float right, float bottom)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | Distance from the left edge of the annotation rectangle to the caret |
| top | `float` | Distance from the top edge |
| right | `float` | Distance from the right edge |
| bottom | `float` | Distance from the bottom edge |

**Return Value:**
`void`

**Description:**
Draws the caret inside the annotation rectangle made smaller by the given distances, and writes them as the rectangle
differences `/RD [left top right bottom]`, in the order of ISO 32000-1. Use it to keep a rectangle that is easy to
click while the caret itself stays small. Without a call, the caret fills the rectangle and `/RD` is absent. A later
call replaces the distances.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A distance is negative, NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `left + right` is not less than the width of the rectangle, or `top + bottom` not less than its height |

**Example:**
```csharp
var wide = page.CreateCaretAnnotation(new HpdfRect(100, 500, 120, 516), "insert");
wide.SetRectDifference(6, 4, 6, 0);   // an 8 x 12 caret in a 20 x 16 rectangle
```

---

## Related Types

- [HpdfCaretSymbol](HpdfCaretSymbol.md) - the symbols
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up, replies
- [HpdfTextMarkupAnnotation](HpdfTextMarkupAnnotation.md) - struck-out text, often combined with a caret for a
  replacement
- [HpdfPage](../core/HpdfPage.md) - `CreateCaretAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
