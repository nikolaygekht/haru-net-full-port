# HpdfPageNumStyle Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfPageNumStyle` is the numbering style of a page label range: the page numbers a viewer displays instead of the
physical page index (for example "i, ii, iii" for a preface and "1, 2, 3" for the body). It is written as the
`/S` entry of a page label dictionary in the catalog's `/PageLabels` number tree.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Decimal` | 0 | Arabic numerals: 1, 2, 3. Writes `/S /D`. |
| `UpperRoman` | 1 | Uppercase Roman numerals: I, II, III. Writes `/S /R`. |
| `LowerRoman` | 2 | Lowercase Roman numerals: i, ii, iii. Writes `/S /r`. |
| `UpperLetters` | 3 | Uppercase letters: A to Z, then AA to ZZ, and so on. Writes `/S /A`. |
| `LowerLetters` | 4 | Lowercase letters: a to z, then aa to zz, and so on. Writes `/S /a`. |
| `Eof` | 5 | Sentinel kept from the C library. **Not a valid argument:** `AddPageLabel`, the `HpdfPageLabel` constructor and `HpdfPageLabel.Style` throw `HpdfException` (`PageNumStyleOutOfRange`), as they do for any value outside the enumeration. |

The label's prefix (`/P`) and start value (`/St`, omitted when it is 1) are set by the other arguments of
`AddPageLabel`. Page labels are a PDF 1.3 feature: `AddPageLabel` raises the document version to 1.3 automatically.
When no label is added for page index 0, the document writes a `Decimal` label for it.

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
for (int i = 0; i < 6; i++)
    pdf.AddPage();

// Pages 0-1: i, ii    Pages 2-5: 1, 2, 3, 4
pdf.AddPageLabel(0, HpdfPageNumStyle.LowerRoman);
pdf.AddPageLabel(2, HpdfPageNumStyle.Decimal);

pdf.SaveToFile("pagelabels.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfDocument.AddPageLabel(int pageNum, HpdfPageNumStyle style, int firstPage = 1, string? prefix = null)` | Starts a label range at the 0-based page index `pageNum`. See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfPageLabel(HpdfPageNumStyle style, int firstPage = 1, string? prefix = null)` | Constructor of the label object that `AddPageLabel` creates. |
| `HpdfPageLabel.Style { get; set; }` | The numbering style of the label (default `Decimal`). |

## Related Types

- [HpdfDocument](../core/HpdfDocument.md) - `AddPageLabel`

---

[Back to Index](../../INDEX.md)
