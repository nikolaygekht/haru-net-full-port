# HpdfTextAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfTextAnnotation` is a sticky note: a small icon on the page that opens a pop-up window with a text when the reader
clicks it. Use it for comments, review remarks and hints that should not be part of the page content. A sticky note
is a markup annotation: it has an author, a subject, a creation date, an opacity and optionally a pop-up annotation
that sets where its window appears.

---

## Getting an Instance

Create sticky notes with the page they are on:

```csharp
public HpdfTextAnnotation CreateTextAnnotation(HpdfRect rect, string text,
    HpdfAnnotationIcon icon = HpdfAnnotationIcon.Note)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | Where the icon is shown; reversed edges (a top below the bottom, a right edge left of the left edge) are swapped |
| text | `string` | The text of the note, in any language; line breaks (`\n`) are kept |
| icon | `HpdfAnnotationIcon` | The icon (see [HpdfAnnotationIcon](HpdfAnnotationIcon.md)); `Note` by default |

The method creates the annotation and adds it to the page's `/Annots` array. The rectangle,
`new HpdfRect(left, bottom, right, top)`, is where the icon is shown. Viewers usually draw the icon at a fixed size
from the upper-left corner of the rectangle, so a rectangle of about 20 x 20 points is usual. The constructor of the
class is internal: a note is always created by the page it is on.

The text is written as `/Contents` and the icon name as `/Name`. The text is a PDF text string: printable ASCII text
is written as it is, any other text as UTF-16BE, so notes in any language (Cyrillic, Greek, Chinese, Japanese, ...)
are shown correctly. The viewer draws the pop-up text with its own fonts; no font has to be loaded or embedded.

The viewer also draws the icon: unlike the other markup annotations, a sticky note has no generated appearance
stream. It gets no annotation flags, so it is not printed unless
[Flags](HpdfAnnotation.md#property-flags) is set to `HpdfAnnotationFlags.Print`. Creating a note does not change the
document version.

```csharp
page.CreateTextAnnotation(new HpdfRect(50, 600, 70, 620), "Проверить цифры\n数字を確認してください");
```

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` or empty |
| `HpdfException` (`AnnotInvalidIcon`) | `icon` is not an `HpdfAnnotationIcon` value |

The arguments are checked before the annotation is created, so a call that throws adds nothing to the page or the
document.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var note = page.CreateTextAnnotation(
    new HpdfRect(500, 760, 520, 780),
    "Please check the figures in this table.",
    HpdfAnnotationIcon.Comment);
note.SetRgbColor(1f, 0.8f, 0f);
note.SetOpened(true);

var question = page.CreateTextAnnotation(new HpdfRect(500, 700, 520, 720), "Is this still correct?",
    HpdfAnnotationIcon.Help);
question.SetRgbColor(0f, 0.5f, 1f);

pdf.SaveToFile("notes.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Icon](#property-icon) | `HpdfAnnotationIcon` | The icon the viewer shows |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) of the icon (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); none by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Text` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetOpened()](#method-setopened) | Sets whether the pop-up window is open when the document is opened (`/Open`) |
| [SetIcon()](#method-seticon) | Changes the icon (`/Name`) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle) | Sets the author shown in the title of the pop-up window (`/T`) (inherited) |
| [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject) | Sets the subject (`/Subj`) (inherited) |
| [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate) | Sets the creation date (`/CreationDate`) (inherited) |
| [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Sets the opacity of the icon (`/CA`) (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) | Sets the note color in RGB (`/C`) (inherited) |
| [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor) | Sets the note color in CMYK (`/C`) (inherited) |
| [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor) | Sets the note color as a gray level (`/C`) (inherited) |
| [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Makes the note color transparent (an empty `/C`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); sticky notes have no border, so it has no visible effect |

---

## Detailed Descriptions

#### Property: *Icon*

**Declaration:**
```csharp
public HpdfAnnotationIcon Icon { get; }
```

**Description:**
The icon set at creation (`HpdfPage.CreateTextAnnotation(rect, text, icon)`) or by [SetIcon()](#method-seticon).

---

#### Method: *SetIcon()*

**Declaration:**
```csharp
public void SetIcon(HpdfAnnotationIcon icon)
```

**Description:**
Changes the icon the viewer draws for the note (`/Name`; `HPDF_TextAnnot_SetIcon` of the C library). A value that is
not an `HpdfAnnotationIcon` throws `HpdfException` (`AnnotInvalidIcon`).

**Example:**
```csharp
var note = page.CreateTextAnnotation(new HpdfRect(50, 700, 70, 720), "Check this");
note.SetIcon(HpdfAnnotationIcon.Help);
```

---

#### Method: *SetOpened()*

**Declaration:**
```csharp
public void SetOpened(bool open)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| open | `bool` | `true` to show the pop-up window open when the page is displayed |

**Return Value:**
`void`

**Description:**
Writes `/Open true` or `/Open false`; a later call replaces the value. Without a call, `/Open` is absent and the note
is closed (the PDF default). Unless a pop-up annotation is created for the note with
`HpdfPage.CreatePopupAnnotation()`, the note has no `/Popup` and the viewer decides where and how large the open window
is. A pop-up annotation ([HpdfPopupAnnotation](HpdfPopupAnnotation.md)) sets the rectangle of the window and has an
open state of its own, set with [HpdfPopupAnnotation.SetOpened()](HpdfPopupAnnotation.md#method-setopened).

**Example:**
```csharp
note.SetOpened(true);
```

---

## Related Types

- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up
- [HpdfAnnotation](HpdfAnnotation.md) - color, flags
- [HpdfPopupAnnotation](HpdfPopupAnnotation.md) - the window of the note
- [HpdfAnnotationIcon](HpdfAnnotationIcon.md) - the icons
- [HpdfPage](../core/HpdfPage.md) - `CreateTextAnnotation()`, `CreatePopupAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
