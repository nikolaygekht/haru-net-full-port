# HpdfPageLabel Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPageLabel` describes the page numbering of a range of pages: a numbering style (decimal, roman, letters), the
number of the first page of the range and an optional prefix. Viewers show these labels instead of the plain page
index, for example "iii" for the front matter or "A-1" for an appendix. A range starts at the page given when the
label is added and lasts until the page where the next label starts.

---

## Getting an Instance

Add labels to a document with `HpdfDocument.AddPageLabel`; the document creates the `HpdfPageLabel` objects and
writes them as the catalog's `/PageLabels` number tree when it is saved.

**Declaration (HpdfDocument):**
```csharp
public void AddPageLabel(int pageNum, HpdfPageNumStyle style, int firstPage = 1, string? prefix = null)
```

| Name | Type | Description |
|------|------|-------------|
| pageNum | `int` | The 0-based index of the first page of the range, in the order the viewer shows the pages |
| style | [HpdfPageNumStyle](../types/HpdfPageNumStyle.md) | The numbering style |
| firstPage | `int` | The number of the first page of the range (default `1`), at least 1; later pages count up from it |
| prefix | `string?` | Text put before the number (default: none) |

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `pageNum` is negative |
| `HpdfException` (`InvalidParameter`) | `firstPage` is less than 1 |

`AddPageLabel` replaces a label added before for the same `pageNum`. It does not check that the page exists, so
labels can be added before the pages. Page labels were introduced in PDF 1.3: the method raises the document version
to at least 1.3.

**Always add a label for page index 0.** The PDF specification requires the page labels tree to start at page 0;
the document does not add a default label for it.

The public constructors of `HpdfPageLabel` create a label object only; the document has no method that takes one.
Use them only to build a `/PageLabels` dictionary yourself with [ToDict()](#method-todict) and
`HpdfCatalog.SetPageLabels` (see [Remarks](#remarks)).

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
for (int i = 0; i < 10; i++)
    pdf.AddPage();

pdf.AddPageLabel(0, HpdfPageNumStyle.LowerRoman);           // pages 0-2: i, ii, iii
pdf.AddPageLabel(3, HpdfPageNumStyle.Decimal);              // pages 3-7: 1, 2, 3, 4, 5
pdf.AddPageLabel(8, HpdfPageNumStyle.Decimal, 1, "A-");     // pages 8-9: A-1, A-2

pdf.SaveToFile("page-labels.pdf");   // PDF 1.3
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Style](#property-style) | [HpdfPageNumStyle](../types/HpdfPageNumStyle.md) | The numbering style (`/S`) |
| [FirstPage](#property-firstpage) | `int` | The number of the first page of the range (`/St`) |
| [Prefix](#property-prefix) | `string?` | Text put before the number (`/P`) |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfPageLabel()](#constructor-hpdfpagelabel) | Creates a label |
| [ToDict()](#method-todict) | Converts the label to a page label dictionary |
| [ToString()](#method-tostring) | Returns a readable description of the label |

---

## Detailed Descriptions

#### Constructor: *HpdfPageLabel()*

**Declarations:**
```csharp
public HpdfPageLabel()
public HpdfPageLabel(HpdfPageNumStyle style, int firstPage = 1, string? prefix = null)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| style | [HpdfPageNumStyle](../types/HpdfPageNumStyle.md) | The numbering style |
| firstPage | `int` | The number of the first page of the range (default `1`) |
| prefix | `string?` | Text put before the number (default `null`) |

**Description:**
The parameterless constructor creates a decimal label starting at 1 without a prefix. No argument is validated.

---

#### Property: *Style*

**Declaration:**
```csharp
public HpdfPageNumStyle Style { get; set; }
```

**Description:**
The numbering style, written as `/S`:

| Value | Written as | Labels |
|-------|-----------|--------|
| `Decimal` | `/D` | 1, 2, 3 |
| `UpperRoman` | `/R` | I, II, III |
| `LowerRoman` | `/r` | i, ii, iii |
| `UpperLetters` | `/A` | A, B, ..., Z, AA, BB |
| `LowerLetters` | `/a` | a, b, ..., z, aa, bb |

`HpdfPageNumStyle.Eof` and values outside the enumeration are written as `/D`. A label always has a style: labels
that consist of the prefix only cannot be created.

---

#### Property: *FirstPage*

**Declaration:**
```csharp
public int FirstPage { get; set; }
```

**Description:**
The number of the first page of the range; the following pages count up from it. Written as `/St` only when it is
not `1`, the PDF default. The PDF specification requires a value of at least 1: `HpdfDocument.AddPageLabel` rejects
smaller values, but this property and the constructors do not check them.

**Example:**
```csharp
// Continue the numbering of a previous volume: 301, 302, ...
pdf.AddPageLabel(0, HpdfPageNumStyle.Decimal, 301);
```

---

#### Property: *Prefix*

**Declaration:**
```csharp
public string? Prefix { get; set; }
```

**Description:**
Text put before the number of every page of the range, for example `"A-"` or `"Chapter "`. Written as `/P` (a PDF
text string, so any language can be used) when it is not `null` or empty.

---

#### Method: *ToDict()*

**Declaration:**
```csharp
public HpdfDict ToDict()
```

**Return Value:**
A new page label dictionary with `/S`, and `/P` and `/St` when they differ from the defaults.

**Description:**
Creates the dictionary that the document puts into the `/Nums` array of `/PageLabels`. Each call creates a new
dictionary.

**Example:**
```csharp
var label = new HpdfPageLabel(HpdfPageNumStyle.UpperRoman, 4, "Part ");
var dict = label.ToDict();   // << /S /R /P (Part ) /St 4 >>
```

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
A description such as `PageLabel[Style=Decimal, FirstPage=1, Prefix='A-']`; the prefix part is left out when there is
no prefix.

---

## Remarks

- **Page order.** `pageNum` counts pages in the order the viewer shows them, which is the order of the page tree. It
  differs from `HpdfDocument.Pages` only when pages are added to intermediate page tree nodes out of order (see
  [HpdfPages](HpdfPages.md)).
- **When labels are written.** The `/PageLabels` dictionary is built each time the document is saved, from all labels
  added so far, sorted by page index. It replaces a dictionary set with `HpdfCatalog.SetPageLabels`.
- **Building the tree yourself.** Without `AddPageLabel`, you can pass your own number tree to
  `pdf.Catalog.SetPageLabels`; in that case set the version yourself if needed, because `SetPageLabels` does not
  raise it to 1.3.

---

## Related Types

- [HpdfPageNumStyle](../types/HpdfPageNumStyle.md) - Numbering styles
- [HpdfDocument](HpdfDocument.md) - `AddPageLabel()`
- [HpdfCatalog](HpdfCatalog.md) - `SetPageLabels()` / `GetPageLabels()`

---

[Back to Index](../../INDEX.md)
