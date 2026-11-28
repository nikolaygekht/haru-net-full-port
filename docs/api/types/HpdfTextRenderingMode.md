# HpdfTextRenderingMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfTextRenderingMode` decides whether glyphs of shown text are filled, stroked, both, made invisible, and whether
they are added to the clipping path (the PDF text rendering mode). It is set with `SetTextRenderingMode`, which
writes the numeric value with the `Tr` operator.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Fill` | 0 | Fill the glyphs with the fill color. This is the default (`HpdfConst.DefRenderingMode`). |
| `Stroke` | 1 | Stroke the glyph outlines with the stroke color and line width (outlined text). |
| `FillThenStroke` | 2 | Fill the glyphs, then stroke their outlines. |
| `Invisible` | 3 | Neither fill nor stroke: the text is invisible but still present (for example, searchable text over a scanned image). |
| `FillClipping` | 4 | Fill the glyphs and add them to the clipping path. |
| `StrokeClipping` | 5 | Stroke the glyphs and add them to the clipping path. |
| `FillStrokeClipping` | 6 | Fill and stroke the glyphs and add them to the clipping path. |
| `Clipping` | 7 | Only add the glyphs to the clipping path. |
| `RenderingModeEof` | 8 | Sentinel kept from the C library (number of values). **Not a valid argument:** `SetTextRenderingMode` throws `HpdfException` with `HpdfErrorCode.PageOutOfRange` for it (and for any other value outside 0..7). |

The clipping modes take effect for the drawing operations after the text object ends (`EndText`); wrap them in
`GSave` / `GRestore` to limit the clip.

## Example

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");

page.SetRgbStroke(0.8f, 0, 0);
page.SetLineWidth(0.8f);

page.BeginText();
page.SetFontAndSize(font, 36);
page.SetTextRenderingMode(HpdfTextRenderingMode.Stroke);
page.MoveTextPos(50, 700);
page.ShowText("Outlined text");
page.SetTextRenderingMode(HpdfTextRenderingMode.Fill);
page.EndText();

pdf.SaveToFile("rendering-mode.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfPageText.SetTextRenderingMode(this HpdfCanvas canvas, HpdfTextRenderingMode mode)` | Sets the text rendering mode of the page. See [HpdfPageText](../extensions/HpdfPageText.md). |
| `HpdfConst.DefRenderingMode` | The default rendering mode, `Fill`. See [HpdfConst](HpdfConst.md). |

`SetTextRenderingMode` also stores the mode in `page.GraphicsState.RenderingMode`; that property is an `int`
(the numeric value), not an `HpdfTextRenderingMode`.

## Related Types

- [HpdfPageText](../extensions/HpdfPageText.md) - text operations
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - fill and stroke colors, line width

---

[Back to Index](../../INDEX.md)
