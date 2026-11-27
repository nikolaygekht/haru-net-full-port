# HpdfBorderStyle Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfBorderStyle` is the style of the border drawn around an annotation. It is passed to
[HpdfAnnotation.SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle) and written as `/S` in the border style
dictionary `/BS`. The border is drawn around links and form field widgets. The generated appearances of squares,
circles, lines, polygons, polylines, ink and free text use its width and draw it dashed for `Dashed`; they draw the
other styles solid. Sticky notes, text markup, carets, stamps, redactions, file attachments, pop-ups, 3D and
projection annotations have no border.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Solid` | 0 | A solid rectangle around the annotation. Writes `/S /S`. With width 0 there is no border. |
| `Dashed` | 1 | A dashed rectangle. Writes `/S /D` and the dash array `/D [dashOn dashOff]`. |
| `Beveled` | 2 | A rectangle that looks raised (embossed). Writes `/S /B`. |
| `Inset` | 3 | A rectangle that looks pressed in (engraved). Writes `/S /I`. |
| `Underlined` | 4 | A single line along the bottom of the rectangle. Writes `/S /U`. |

A value outside the enum makes `SetBorderStyle()` throw `HpdfException` (`InvalidParameter`).

For form widgets the generated appearance draws its own frame (see
[HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md)); how far viewers follow `/BS` for widgets and links varies,
`Beveled` and `Inset` in particular.

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

float y = 750;
foreach (var style in Enum.GetValues<HpdfBorderStyle>())
{
    var link = page.CreateLinkAnnotation(new HpdfRect(50, y, 250, y + 25), "https://example.com");
    link.SetBorderStyle(style, 2);
    link.SetRgbColor(0f, 0.3f, 0.8f);
    y -= 40;
}

pdf.SaveToFile("borders.pdf");
```

## Related Types

- [HpdfAnnotation](HpdfAnnotation.md) - `SetBorderStyle()`
- [HpdfLinkAnnotation](HpdfLinkAnnotation.md), [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md),
  [HpdfShapeAnnotation](HpdfShapeAnnotation.md), [HpdfLineAnnotation](HpdfLineAnnotation.md),
  [HpdfFreeTextAnnotation](HpdfFreeTextAnnotation.md)
- [Reserved Types](../types/ReservedTypes.md) - `HpdfBSSubtype` in `Haru.Types`, an unused enum from the C library

---

[Back to Index](../../INDEX.md)
