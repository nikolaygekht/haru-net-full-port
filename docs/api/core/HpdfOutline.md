# HpdfOutline Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfOutline` is an entry of the document outline, the tree of bookmarks that viewers show in their navigation panel.
Each entry has a title, can navigate to a [destination](HpdfDestination.md) and can have child entries, which are
shown expanded or collapsed. The invisible root of the tree is an `HpdfOutline` too.

---

## Getting an Instance

`HpdfOutline` has no public constructor. Use the document and the entries themselves:

| Member | Description |
|--------|-------------|
| `HpdfDocument.GetOutlineRoot()` | Returns the root of the outline tree; creates it (and the catalog's `/Outlines` entry) on the first call |
| `HpdfDocument.CreateOutline(string title)` | Creates a top-level entry: same as `pdf.GetOutlineRoot().CreateChild(title)` |
| [CreateChild()](#method-createchild) | Creates a child entry of an entry (or of the root) |

New entries are appended after the existing children of their parent, so the order of creation is the order shown.

**Declarations (HpdfDocument):**
```csharp
public HpdfOutline GetOutlineRoot()
public HpdfOutline CreateOutline(string title)
```

`CreateOutline` throws `HpdfException` with `HpdfErrorCode.InvalidParameter` when `title` is `null` or empty.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var font = pdf.GetFont("Helvetica");

HpdfDestination AddChapterPage(string heading)
{
    var page = pdf.AddPage();
    page.SetFontAndSize(font, 20);
    page.TextOut(50, 750, heading);

    var destination = page.CreateDestination();
    destination.SetXYZ(0, page.Height, 1);
    return destination;
}

var chapter1 = pdf.CreateOutline("Chapter 1");
chapter1.SetDestination(AddChapterPage("Chapter 1"));
chapter1.CreateChild("Section 1.1").SetDestination(AddChapterPage("Section 1.1"));
chapter1.CreateChild("Section 1.2").SetDestination(AddChapterPage("Section 1.2"));

var chapter2 = pdf.CreateOutline("Chapter 2");
chapter2.SetDestination(AddChapterPage("Chapter 2"));
chapter2.CreateChild("Section 2.1").SetDestination(AddChapterPage("Section 2.1"));
chapter2.Opened = false;   // shown collapsed

// Open the bookmarks panel when the document is opened
pdf.Catalog.PageMode = HpdfPageMode.UseOutlines;

pdf.SaveToFile("outlines.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying outline dictionary |
| [Opened](#property-opened) | `bool` | Whether the children of the entry are shown expanded (default `true`) |

---

## Methods

| Name | Description |
|------|-------------|
| [CreateChild()](#method-createchild) | Creates a child entry with a title |
| [SetOpened()](#method-setopened) | Sets `Opened` (method form, for compatibility with the C library) |
| [SetDestination()](#method-setdestination) | Sets where the entry navigates to when clicked |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The dictionary written to the file. For the root it contains `/Type /Outlines`; for an entry it contains `/Title`,
`/Parent`, the sibling links `/Prev` and `/Next`, the child links `/First` and `/Last`, and `/Dest` once a
destination is set. `/Count` is written when the document is saved (see [Opened](#property-opened)).

---

#### Property: *Opened*

**Declaration:**
```csharp
public bool Opened { get; set; }
```

**Description:**
`true` (the default) shows the children of the entry expanded; `false` shows the entry collapsed, with its children
hidden until the user expands it. The value has no effect on an entry without children.

The state is written as the `/Count` entry each time the document is saved: the number of descendants that are
visible when the entry is open (every child, plus the visible descendants of each open child), positive for an open
entry and negative for a closed one. An entry without children gets no `/Count`. Because the count is computed when
saving, you can change `Opened` and add children in any order.

The root (from `GetOutlineRoot()`) cannot be closed: its `/Count` is always positive, as the PDF specification
requires, and setting `Opened = false` on the root has no effect on the file.

**Example:**
```csharp
var appendix = pdf.CreateOutline("Appendix");
appendix.CreateChild("A. Tables");
appendix.CreateChild("B. Figures");
appendix.Opened = false;   // /Count -2: collapsed, two hidden children
```

---

#### Method: *CreateChild()*

**Declaration:**
```csharp
public HpdfOutline CreateChild(string title)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| title | `string` | The text shown in the bookmarks panel. Any language can be used: ASCII text is stored as it is, other text as a UTF-16BE PDF text string. |

**Return Value:**
The new entry, appended as the last child of this entry.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `title` is `null` or empty |

**Description:**
Creates an entry under this one. Call it on the root (`pdf.GetOutlineRoot()`) to create a top-level entry. The new
entry is open and has no destination.

**Example:**
```csharp
var part = pdf.CreateOutline("Teil 1 - Überblick");
var chapter = part.CreateChild("Глава 1");
chapter.CreateChild("第一節");
```

---

#### Method: *SetOpened()*

**Declaration:**
```csharp
public void SetOpened(bool opened)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| opened | `bool` | `true` to show the children expanded, `false` to show the entry collapsed |

**Return Value:**
`void`

**Description:**
Same as assigning [Opened](#property-opened). It exists for compatibility with `HPDF_Outline_SetOpened` of the C
library.

---

#### Method: *SetDestination()*

**Declarations:**
```csharp
public void SetDestination(HpdfDestination destination)
public void SetDestination(HpdfArray destination)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| destination | [HpdfDestination](HpdfDestination.md) | The target page and view; its `DestArray` is stored |
| destination | `HpdfArray` | A destination array, for example `HpdfDestination.DestArray` |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `destination` is `null` |
| `HpdfException` (`InvalidDestination`) | The destination has no view (`[page]` only): none of `SetXYZ`, `SetFit`, `SetFitH`, `SetFitV`, `SetFitR`, `SetFitB`, `SetFitBH` or `SetFitBV` has been called |

**Description:**
Sets or replaces `/Dest`, the place the viewer shows when the entry is clicked. An entry without a destination is
shown but does nothing when clicked, which is useful for grouping entries.

Set the view of the destination (`SetXYZ`, `SetFit`, ...) before this call: a destination without a view is not
valid, and this method throws for it. The array is stored by reference, so one destination can be shared by several
entries, and changing its view later (before saving) changes all of them.

**Example:**
```csharp
var destination = page.CreateDestination();
destination.SetFit();

var entry = pdf.CreateOutline("First page");
entry.SetDestination(destination);
```

---

## Remarks

- **Bookmarks panel.** Creating outlines does not open the panel in the viewer; set
  `pdf.Catalog.PageMode = HpdfPageMode.UseOutlines` (see [HpdfCatalog](HpdfCatalog.md#property-pagemode)).
- **Read access.** The class has no members to read the title or the children of an entry; keep your own references
  to the entries you create.
- **Not supported:** text color and style of entries (`/C`, `/F`) and actions other than destinations (`/A`).

---

## Related Types

- [HpdfDestination](HpdfDestination.md) - Targets of outline entries
- [HpdfDocument](HpdfDocument.md) - `GetOutlineRoot()`, `CreateOutline()`
- [HpdfCatalog](HpdfCatalog.md) - `PageMode` to show the bookmarks panel
- [Outlines Guide](../../guides/OutlinesGuide.md)

---

[Back to Index](../../INDEX.md)
