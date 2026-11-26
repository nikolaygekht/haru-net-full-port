# HpdfTextAlignment Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfTextAlignment` selects how [`TextRect`](../extensions/HpdfPageText.md) places each line of text inside its rectangle:
flush left, flush right, centered, or justified to both edges.

It applies only to text printed on a page. Form text fields have their own enum,
`Haru.Forms.HpdfFieldAlignment` (written as the field's `/Q` entry), whose values are numbered differently
(`Left = 0`, `Center = 1`, `Right = 2`). The two enums are not interchangeable: never cast one to the other,
because `Right` and `Center` would be swapped.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Left` | 0 | Each line starts at the left edge of the rectangle. |
| `Right` | 1 | Each line ends at the right edge of the rectangle. |
| `Center` | 2 | Each line is centered between the left and right edges. |
| `Justify` | 3 | Every line except the last line of a paragraph is stretched to the full width by adjusting the character spacing; the last line of a paragraph (and the last line of the text) is left-aligned. The original character spacing is restored afterwards. |

## Example

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

const string text = "Haru.NET wraps this text into the rectangle and justifies every full line.";

page.BeginText();
page.SetFontAndSize(font, 12);
int printed = page.TextRect(50, 750, 300, 650, text, HpdfTextAlignment.Justify);
page.EndText();

// printed < text.Length means the rectangle was too small for the whole text
pdf.SaveToFile("aligned.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfPageText.TextRect(this HpdfPage page, float left, float top, float right, float bottom, string text, HpdfTextAlignment alignment)` | Prints wrapped text inside a rectangle with the given alignment and returns the number of characters printed. See [HpdfPageText](../extensions/HpdfPageText.md). |

## Related Types

- [HpdfPageText](../extensions/HpdfPageText.md) - text operations, including `TextRect`
- [Forms Guide](../../guides/FormsGuide.md) - `HpdfFieldAlignment` for text fields (a different enum)

---

[Back to Index](../../INDEX.md)
