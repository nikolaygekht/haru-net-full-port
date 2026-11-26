# HpdfBlendMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfBlendMode` selects how colors that are painted are combined with the colors already on the page (the PDF
blend mode). It is set on an extended graphics state with `HpdfExtGState.SetBlendMode`, which writes the value's
name as the `/BM` entry, and applied to a page with `SetExtGState`.

Blend modes are a transparency feature: `SetBlendMode` raises the document version to **PDF 1.4** automatically,
for every value including `Normal`.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Normal` | 0 | The painted color replaces the backdrop (the default behavior of PDF). Writes `/BM /Normal`. |
| `Multiply` | 1 | Multiplies the colors; the result is always at least as dark as either color. Writes `/Multiply`. |
| `Screen` | 2 | Multiplies the complements of the colors; the result is always at least as light as either color. Writes `/Screen`. |
| `Overlay` | 3 | Multiplies or screens depending on the backdrop color. Writes `/Overlay`. |
| `Darken` | 4 | Keeps the darker of the two colors. Writes `/Darken`. |
| `Lighten` | 5 | Keeps the lighter of the two colors. Writes `/Lighten`. |
| `ColorDodge` | 6 | Brightens the backdrop to reflect the painted color. Writes `/ColorDodge`. |
| `ColorBurn` | 7 | Darkens the backdrop to reflect the painted color. Writes `/ColorBurn`. |
| `HardLight` | 8 | Multiplies or screens depending on the painted color. Writes `/HardLight`. |
| `SoftLight` | 9 | Darkens or lightens depending on the painted color, with a softer effect than `HardLight`. Writes `/SoftLight`. |
| `Difference` | 10 | Subtracts the darker of the two colors from the lighter one. Writes `/Difference`. |
| `Exclusion` | 11 | Like `Difference`, with lower contrast. Writes `/Exclusion`. |
| `Eof` | 12 | Sentinel kept from the C library. **Not a valid argument:** `SetBlendMode` throws an `HpdfException` (`ExtGStateOutOfRange`), as it does for any value outside the enumeration. |

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.SetRgbFill(1, 0.8f, 0);
page.Rectangle(100, 600, 150, 150);
page.Fill();

var multiply = new HpdfExtGState(pdf.Xref, "GS1");
multiply.SetBlendMode(HpdfBlendMode.Multiply);

page.GSave();
page.SetExtGState(multiply);
page.SetRgbFill(0, 0.6f, 1);
page.Rectangle(175, 525, 150, 150);
page.Fill();
page.GRestore();

pdf.SaveToFile("blend.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfExtGState.SetBlendMode(HpdfBlendMode mode)` | Sets `/BM` of the extended graphics state and requires PDF 1.4. |
| `HpdfPageGraphics.SetExtGState(this HpdfPage page, HpdfExtGState extGState)` | Applies the graphics state (with its blend mode) to the page. See [HpdfPageGraphics](../extensions/HpdfPageGraphics.md). |

`HpdfExtGState` (namespace `Haru.Doc`) is created with `new HpdfExtGState(pdf.Xref, localName)`. Give each
graphics state used on a page its own local name: applying a second, different state with a name already used on
that page throws `HpdfException` (`DuplicateRegistration`). `HpdfExtGState` also offers
`SetAlphaFill` and `SetAlphaStroke` for opacity.

## Related Types

- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - `SetExtGState` and the drawing operations
- [HpdfImage](../core/HpdfImage.md) - image transparency (masks, soft masks)

---

[Back to Index](../../INDEX.md)
