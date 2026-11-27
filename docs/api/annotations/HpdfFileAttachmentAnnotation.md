# HpdfFileAttachmentAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfFileAttachmentAnnotation` is a file attachment annotation (PDF 1.3; not in the C library): an icon on the page
that stands for a file embedded in the PDF. Viewers open or save the file when the icon is double-clicked. Use it to attach source data, a spreadsheet or a drawing at the place of the
page it belongs to; for files that belong to the document as a whole, a document attachment
(`HpdfDocument.AttachFile()`) needs no icon.

---

## Getting an Instance

Create file attachment annotations with the page they are on, with an embedded file of the document or with a file of
their own:

```csharp
public HpdfFileAttachmentAnnotation CreateFileAttachmentAnnotation(HpdfRect rect, HpdfEmbeddedFile file, string text,
    HpdfFileAttachmentIcon icon = HpdfFileAttachmentIcon.PushPin)

public HpdfFileAttachmentAnnotation CreateFileAttachmentAnnotation(HpdfRect rect, string fileName, byte[] data, string text,
    HpdfFileAttachmentIcon icon = HpdfFileAttachmentIcon.PushPin)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The annotation rectangle; the icon fills it (about 20 x 20 points is usual). Reversed edges are swapped |
| file | `HpdfEmbeddedFile` | An embedded file, usually a document attachment returned by `HpdfDocument.AttachFile()` |
| fileName | `string` | The name of the annotation's own file, without a directory |
| data | `byte[]` | The contents of the annotation's own file (copied) |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |
| icon | `HpdfFileAttachmentIcon` | The icon; a push pin by default (see [HpdfFileAttachmentIcon](HpdfFileAttachmentIcon.md)) |

The method creates the annotation (`/Subtype /FileAttachment`, `/FS` with the file specification, `/Name` with the
icon) and adds it to the page's `/Annots` array. The icon is blue (`/C [0.2 0.3 0.8]`) until another color is set, and
the annotation has the Print flag (`/F 4`). Creating one raises the document version to PDF 1.3.

- With an `HpdfEmbeddedFile` of `AttachFile()`, the file is embedded once: it is listed in the attachments panel and
  opened by the icon. Several annotations can show the same file.
- With a file name and data, the file belongs to the annotation alone: it is not one of
  [HpdfDocument.AttachedFiles](../core/HpdfDocument.md#attachedfiles), has no MIME type, and its name need not differ
  from the names of other files. Reach it through [File](#property-file).

PDF/A-1 allows no embedded files: saving a PDF/A document with a file attachment annotation throws `HpdfException`
(`InvalidOperation`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `file`, `text` or `data` is `null` |
| `HpdfException` (`InvalidParameter`) | `fileName` is `null` or empty, or contains `/` or `\` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |
| `HpdfException` (`AnnotInvalidIcon`) | `icon` is not an `HpdfFileAttachmentIcon` value |

The arguments are checked before anything is created, so a call that throws adds nothing to the page or the document.

### The appearance

The library draws the icon in the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)): a push pin, a bar chart, a paper clip or a
tag, in the annotation color, as large as the square that fits into the rectangle and centered in it. The icon looks
the same in every viewer. [SetNoColor()](HpdfAnnotation.md#method-setnocolor) draws nothing; the border style is not
used.

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
page.TextOut(80, 705, "The revenue table is attached here.");

// A document attachment, shown by a paper clip next to the text
var data = System.Text.Encoding.UTF8.GetBytes("quarter,revenue\nQ1,100\nQ2,120\n");
var file = pdf.AttachFile("revenue.csv", data, "text/csv");
file.SetDescription("The revenue table");
var clip = page.CreateFileAttachmentAnnotation(new HpdfRect(50, 700, 70, 720), file, "revenue.csv",
    HpdfFileAttachmentIcon.Paperclip);
clip.SetTitle("Finance");

// A file of the annotation alone
page.CreateFileAttachmentAnnotation(new HpdfRect(50, 650, 70, 670), "readme.txt",
    System.Text.Encoding.UTF8.GetBytes("Read me"), "Notes on the table", HpdfFileAttachmentIcon.Tag);

pdf.SaveToFile("attachments.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [File](#property-file) | `HpdfEmbeddedFile` | The embedded file the annotation opens |
| [Icon](#property-icon) | `HpdfFileAttachmentIcon` | The icon |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.FileAttachment` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetIcon()](#method-seticon) | Sets the icon (`/Name`) |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set or remove the color of the icon (`/C`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); the generated icon does not use it |

---

## Detailed Descriptions

#### Property: *File*

**Declaration:**
```csharp
public HpdfEmbeddedFile File { get; }
```

**Description:**
The embedded file given at creation, or the file created for the annotation from a name and data. Use it to set the
description or the dates of the annotation's own file.

**Example:**
```csharp
attachment.File.SetModificationDate(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
```

---

#### Property: *Icon*

**Declaration:**
```csharp
public HpdfFileAttachmentIcon Icon { get; }
```

**Description:**
The icon given at creation or by the last [SetIcon()](#method-seticon) call.

---

#### Method: *SetIcon()*

**Declaration:**
```csharp
public void SetIcon(HpdfFileAttachmentIcon icon)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| icon | `HpdfFileAttachmentIcon` | `PushPin`, `Graph`, `Paperclip` or `Tag` |

**Return Value:**
`void`

**Description:**
Writes `/Name` (`/PushPin`, `/Graph`, `/Paperclip` or `/Tag`) and draws that icon in the appearance stream. A later
call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`AnnotInvalidIcon`) | `icon` is not an `HpdfFileAttachmentIcon` value |

**Example:**
```csharp
attachment.SetIcon(HpdfFileAttachmentIcon.Graph);
```

---

## Related Types

- [HpdfEmbeddedFile](../core/HpdfEmbeddedFile.md) - the embedded file
- [HpdfFileAttachmentIcon](HpdfFileAttachmentIcon.md) - the icons
- [HpdfDocument](../core/HpdfDocument.md) - `AttachFile()`, `AttachedFiles`
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up, replies
- [HpdfPage](../core/HpdfPage.md) - `CreateFileAttachmentAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
