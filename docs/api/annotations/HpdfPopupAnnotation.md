# HpdfPopupAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`HpdfPopupAnnotation` is a pop-up annotation (PDF 1.3): the window in which a viewer shows the text of a markup
annotation, its parent. Create one to choose where the window of a comment appears and how large it is, and whether
it is open when the document is opened. Without a pop-up annotation, the viewer places the window itself.

---

## Getting an Instance

Create a pop-up with the page of its parent:

```csharp
public HpdfPopupAnnotation CreatePopupAnnotation(HpdfRect rect, HpdfMarkupAnnotation parent)
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The rectangle of the window; reversed edges are swapped |
| parent | `HpdfMarkupAnnotation` | The markup annotation whose text the window shows; it must be on this page and have no pop-up yet |

The method creates the annotation (`/Subtype /Popup`), connects it with its parent in both directions (`/Parent` of the
pop-up, `/Popup` of the parent, also [Popup](HpdfMarkupAnnotation.md#property-popup) of the parent object) and adds
it to the page's `/Annots` array after the parent. Creating one raises the document version to PDF 1.3.

The viewer draws the window, with the text (`/Contents`) of the parent and its title ([SetTitle()](HpdfMarkupAnnotation.md#method-settitle))
in the title bar; the pop-up annotation has no text and no appearance stream of its own, and gets no annotation
flags. Any markup annotation can have a pop-up: sticky notes, shapes, lines, polygons, ink, text markup, carets, free
text, stamps, redactions, file attachments and projection annotations.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `parent` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `parent` is not an annotation of this page |
| `HpdfException` (`InvalidOperation`) | `parent` already has a pop-up annotation |

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var note = page.CreateTextAnnotation(new HpdfRect(60, 700, 80, 720),
    "The figures in this table are from the 2025 report.", HpdfAnnotationIcon.Comment);
note.SetTitle("Reviewer");

// The note's window opens to the right of the icon when the document is opened
var popup = page.CreatePopupAnnotation(new HpdfRect(100, 620, 330, 720), note);
popup.SetOpened(true);

Console.WriteLine(note.Popup == popup);    // True
Console.WriteLine(popup.Parent == note);   // True

pdf.SaveToFile("popup.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Parent](#property-parent) | `HpdfMarkupAnnotation` | The markup annotation whose text the window shows |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); none by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.Popup` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetOpened()](#method-setopened) | Sets whether the window is open when the document is opened (`/Open`) |
| [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Write `/C` (inherited); viewers usually color the window with the color of the parent |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) | Writes `/BS` (inherited); the viewer draws the window, so it has no visible effect |

---

## Detailed Descriptions

#### Property: *Parent*

**Declaration:**
```csharp
public HpdfMarkupAnnotation Parent { get; }
```

**Description:**
The markup annotation given at creation, written as `/Parent`. It cannot be changed.

---

#### Method: *SetOpened()*

**Declaration:**
```csharp
public void SetOpened(bool open)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| open | `bool` | `true` to show the window open when the page is displayed |

**Return Value:**
`void`

**Description:**
Writes `/Open true` or `/Open false`; a later call replaces the value. Without a call, `/Open` is absent and the window
is closed (the PDF default) until the reader opens it, for example by clicking the parent.

**Example:**
```csharp
popup.SetOpened(true);
```

---

## Related Types

- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - the parents: their `Popup` property
- [HpdfTextAnnotation](HpdfTextAnnotation.md) - sticky notes, whose window opens when the icon is clicked
- [HpdfAnnotation](HpdfAnnotation.md) - base class
- [HpdfPage](../core/HpdfPage.md) - `CreatePopupAnnotation()`
- [Annotations Guide](../../guides/AnnotationsGuide.md)

---

[Back to Index](../../INDEX.md)
