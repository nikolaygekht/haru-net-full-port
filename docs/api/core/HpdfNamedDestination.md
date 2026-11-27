# HpdfNamedDestination Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfNamedDestination` is a named destination: a name in the document's `Dests` name tree that stands for a
[destination](HpdfDestination.md). Links and bookmarks that go to it write only the name, so they can be created
before the target page exists (a table of contents written first), and a URL opens the document at the name:
`manual.pdf#nameddest=chapter2`.

---

## Getting an Instance

The class has no public constructor. Create named destinations with the document:

| Method | Creates |
|--------|---------|
| `HpdfDocument.CreateNamedDestination(string name)` | A named destination without a target; set it with [SetDestination()](#method-setdestination) |
| `HpdfDocument.CreateNamedDestination(string name, HpdfDestination destination)` | A named destination with its target |

The names are unique in a document. Use it with:

| Method | Writes |
|--------|--------|
| `HpdfPage.CreateLinkAnnotation(HpdfRect rect, HpdfNamedDestination destination)` | A link with `/Dest (name)` |
| `HpdfOutline.SetDestination(HpdfNamedDestination destination)` | A bookmark with `/Dest (name)` |

When the document is saved, every named destination is written to the `Dests` name tree of the catalog's name
dictionary, with the names sorted as a name tree requires, mapped to the destination array of its target. Saving
throws `HpdfException` (`InvalidDestination`) while a named destination has no target. A named destination belongs to
the document that created it: a link or bookmark of another document, or a target on a page of another document,
throws `HpdfException` (`InvalidDestination`).

The name dictionary is a PDF 1.2 feature, so named destinations do not raise the document version. They are allowed
in PDF/A-1.

---

## Example

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
pdf.Catalog.SetPageMode(HpdfPageMode.UseOutlines);

// The contents page links to the chapters before their pages exist
var contents = pdf.AddPage();
contents.SetFontAndSize(font, 12);
var chapters = new HpdfNamedDestination[3];
for (int i = 0; i < chapters.Length; i++)
{
    chapters[i] = pdf.CreateNamedDestination($"chapter{i + 1}");
    float y = 700 - i * 25;
    contents.TextOut(70, y, $"Chapter {i + 1}");
    contents.CreateLinkAnnotation(new HpdfRect(65, y - 4, 160, y + 12), chapters[i]);
    pdf.CreateOutline($"Chapter {i + 1}").SetDestination(chapters[i]);
}

// The chapter pages set the targets
for (int i = 0; i < chapters.Length; i++)
{
    var page = pdf.AddPage();
    page.SetFontAndSize(font, 24);
    page.TextOut(50, 750, $"Chapter {i + 1}");

    var target = page.CreateDestination();
    target.SetXYZ(0, page.Height, 0);
    chapters[i].SetDestination(target);
}

pdf.SaveToFile("chapters.pdf");   // chapters.pdf#nameddest=chapter2 opens the second chapter
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Name](#property-name) | `string` | The name |
| [Destination](#property-destination) | `HpdfDestination?` | The target, or `null` while it is not set |

---

## Methods

| Name | Description |
|------|-------------|
| [SetDestination()](#method-setdestination) | Sets the target that the name stands for |

---

## Detailed Descriptions

#### Property: *Name*

**Declaration:**
```csharp
public string Name { get; }
```

**Description:**
The name given to `CreateNamedDestination()`. It is written as a PDF text string: as is when it is ASCII, otherwise as
UTF-16BE. Names that go into URLs (`#nameddest=`) are best made of ASCII letters, digits, `-` and `_`.

---

#### Property: *Destination*

**Declaration:**
```csharp
public HpdfDestination? Destination { get; }
```

**Description:**
The target set by `CreateNamedDestination()` or [SetDestination()](#method-setdestination), or `null` while none is
set.

---

#### Method: *SetDestination()*

**Declaration:**
```csharp
public void SetDestination(HpdfDestination destination)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| destination | [HpdfDestination](HpdfDestination.md) | A destination on a page of the same document, with its view set (`SetXYZ`, `SetFit`, ...) |

**Return Value:**
`void`

**Description:**
Sets the target of the name; a later call replaces it, and the links and bookmarks that go to the name go to the new
target. The destination array is stored by reference: changing the view of the destination before saving changes
the target too.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `destination` is `null` |
| `HpdfException` (`InvalidDestination`) | The destination has no view, or is on a page of another document |

**Example:**
```csharp
var target = page.CreateDestination();
target.SetFitH(page.Height);
appendix.SetDestination(target);
```

---

## Related Types

- [HpdfDocument](HpdfDocument.md) - `CreateNamedDestination()`, `NamedDestinations`
- [HpdfDestination](HpdfDestination.md) - The target of a name
- [HpdfLinkAnnotation](../annotations/HpdfLinkAnnotation.md) - Links to a name
- [HpdfOutline](HpdfOutline.md) - Bookmarks to a name
- [HpdfCatalog](HpdfCatalog.md) - The name dictionary that holds the `Dests` tree
- [Outlines Guide](../../guides/OutlinesGuide.md) - "Named Destinations"

---

[Back to Index](../../INDEX.md)
