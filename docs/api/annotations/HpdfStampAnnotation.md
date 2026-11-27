# HpdfStampAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) → [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfStampAnnotation` is a rubber stamp annotation (PDF 1.3): one of the standard stamps such as "Approved", "Draft"
or "Confidential", shown in a box on the page like a stamp on paper. Its text, a comment on the stamp, is shown in its
pop-up window.

---

## Getting an Instance

Create stamps with the page they are on:

```csharp
public HpdfStampAnnotation CreateStampAnnotation(HpdfRect rect, HpdfStampName name, string text, HpdfFont font)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The box of the stamp; reversed edges are swapped |
| name | `HpdfStampName` | The standard stamp (see [HpdfStampName](HpdfStampName.md)) |
| text | `string` | The text of the annotation (`/Contents`), shown in its pop-up window; may be empty |
| font | `HpdfFont` | The font of the label: any font of the document of horizontal writing |

The method creates the annotation (`/Subtype /Stamp`, the stamp as `/Name`, for example `/Name /Approved`) and adds
it to the page's `/Annots` array. The annotation has the Print flag (`/F 4`). Creating one raises the document version
to PDF 1.3.

The color of the stamp follows its meaning until another color is set with
[SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor) or the other color setters:

| Color | Stamps |
|-------|--------|
| Green (`0.1 0.5 0.1`) | `Approved`, `Final`, `Sold`, `ForPublicRelease` |
| Red (`0.75 0.1 0.1`) | `NotApproved`, `Expired`, `NotForPublicRelease`, `Confidential`, `TopSecret` |
| Blue (`0.1 0.2 0.6`) | `Experimental`, `AsIs`, `Departmental`, `ForComment`, `Draft` |

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `name` is not an `HpdfStampName` value |
| `HpdfException` (`InvalidParameter`) | `font` is `null`, or a font of vertical writing |

The arguments are checked before the annotation is created, so a call that throws adds nothing to the page or the
document.

### The appearance

The library draws the appearance stream when the document is saved (see
[Generated appearances](HpdfMarkupAnnotation.md#generated-appearances)), in the annotation color:

- A frame with rounded corners along the edge of the box. Its line width is 1/12 of the smaller side of the box, at
  least 1 point.
- The label of the stamp in capitals ("APPROVED", "NOT FOR PUBLIC RELEASE", ...), centered, as large as fits into
  the frame (the capitals at most 60 percent of its height), with the font of the annotation. The font is added to the
  resources of the stream; an embedded font includes the glyphs of the label in its subset.

The label is English whatever the language of the document. A bold font gives the usual look of a stamp. With
[SetNoColor()](HpdfAnnotation.md#method-setnocolor) nothing is drawn; the border style has no effect.

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var bold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");
var page = pdf.AddPage();

var approved = page.CreateStampAnnotation(new HpdfRect(380, 760, 560, 810), HpdfStampName.Approved,
    "Approved by the board on 27 November 2026", bold);
approved.SetTitle("J. Miller");

var draft = page.CreateStampAnnotation(new HpdfRect(380, 700, 560, 750), HpdfStampName.Draft, "", bold);
draft.SetTransparency(0.5f);

pdf.SaveToFile("stamps.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Name](#property-name) | `HpdfStampName` | The standard stamp |
| [Opacity](HpdfMarkupAnnotation.md#property-opacity) | `float` | The constant opacity (`/CA`) (inherited) |
| [Popup](HpdfMarkupAnnotation.md#property-popup) | `HpdfPopupAnnotation?` | The pop-up annotation that shows the text, or `null` (inherited) |
| [InReplyTo](HpdfMarkupAnnotation.md#property-inreplyto), [ReplyType](HpdfMarkupAnnotation.md#property-replytype) | | The annotation this one replies to, and how (inherited) |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Stamp` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetFont()](#method-setfont) | Sets the font of the label |
| [SetTitle()](HpdfMarkupAnnotation.md#method-settitle), [SetSubject()](HpdfMarkupAnnotation.md#method-setsubject), [SetCreationDate()](HpdfMarkupAnnotation.md#method-setcreationdate), [SetTransparency()](HpdfMarkupAnnotation.md#method-settransparency) | Author, subject, creation date and opacity (inherited) |
| [SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto), [SetRichText()](HpdfMarkupAnnotation.md#method-setrichtext), [Set3DView()](HpdfMarkupAnnotation.md#method-set3dview) | Replies, rich text, a 3D view (inherited) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Set the color of the stamp (`/C`) (inherited) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); has no effect on the stamp |

---

## Detailed Descriptions

#### Property: *Name*

**Declaration:**
```csharp
public HpdfStampName Name { get; }
```

**Description:**
The standard stamp given at creation, written as `/Name`. It cannot be changed.

**Example:**
```csharp
Console.WriteLine(stamp.Name);   // Draft
```

---

#### Method: *SetFont()*

**Declaration:**
```csharp
public void SetFont(HpdfFont font)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| font | `HpdfFont` | The font of the label: any font of the document of horizontal writing |

**Return Value:**
`void`

**Description:**
Replaces the font given at creation. The label is drawn with it when the document is saved, sized to fit the box.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `font` is `null`, or a font of vertical writing |

**Example:**
```csharp
stamp.SetFont(boldFont);
```

---

## Related Types

- [HpdfStampName](HpdfStampName.md) - the standard stamps
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - base class: author, subject, date, opacity, pop-up
- [HpdfAnnotation](HpdfAnnotation.md) - color, flags
- [HpdfPage](../core/HpdfPage.md) - `CreateStampAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
