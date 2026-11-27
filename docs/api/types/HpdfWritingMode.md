# HpdfWritingMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfWritingMode` tells in which direction a font writes: from left to right, or from top to bottom as Chinese,
Japanese and Korean text can be written. The writing mode belongs to the font, not to the page: a font is horizontal
or vertical from the moment it is created, and the text shown with it runs in that direction.

Only CID fonts can be vertical. Standard, TrueType and Type 1 fonts are always horizontal.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Horizontal` | 0 | Text runs from left to right. The text position is on the baseline, at the left of the first character. |
| `Vertical` | 1 | Text runs from top to bottom. The text position is at the top of the first character, in the middle of its width; every character moves it down by the font size. |
| `Eof` | 2 | Sentinel carried over from the C library; not a valid argument (`InvalidParameter`). |

## Example

```csharp
using Haru.Doc;
using Haru.Font.CID;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// The same font file, once for each direction
var horizontal = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JH", "fonts/noto-jp.ttf", 932).AsFont();
var vertical = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JV", "fonts/noto-jp.ttf", 932, HpdfWritingMode.Vertical).AsFont();

Console.WriteLine(horizontal.WritingMode);   // Horizontal
Console.WriteLine(vertical.WritingMode);     // Vertical

page.BeginText();
page.SetFontAndSize(horizontal, 16);
page.TextOut(50, 760, "日本語、「テスト」。");     // a line to the right of (50, 760)
page.SetFontAndSize(vertical, 16);
page.TextOut(500, 760, "日本語、「テスト」。");    // a column under (500, 760)
page.EndText();

pdf.SaveToFile("writing-modes.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfCIDFont.LoadFromTrueTypeFile(HpdfDocument document, string localName, string filePath, int codePage, HpdfWritingMode writingMode)` | Loads an embedded CID font for horizontal or vertical writing. |
| `HpdfFont.WritingMode`, `HpdfCIDFont.WritingMode`, `HpdfCIDFontType0.WritingMode` | The writing mode of a font. |
| `CIDEncoder.WritingMode` | The writing mode of an encoding of the predefined CJK fonts: `Vertical` for the encodings whose names end with `-V`. |
| `IHpdfFontImplementation.WritingMode` | The writing mode of a font implementation; `Horizontal` unless the implementation says otherwise. |

## Related Types

- [Fonts Guide](../../guides/FontsGuide.md#vertical-writing) - how vertical text is placed, measured and wrapped
- [HpdfFont](../core/HpdfFont.md) - `WritingMode` and `MeasureText`
- [HpdfPageText](../extensions/HpdfPageText.md) - `TextRect` lays out the text of a vertical font in columns

---

[Back to Index](../../INDEX.md)
