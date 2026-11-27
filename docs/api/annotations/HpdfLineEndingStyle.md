# HpdfLineEndingStyle Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfLineEndingStyle` is the shape drawn at an end of a line annotation, of a polyline annotation or of the callout
line of a free text annotation (`/LE`): an arrowhead, a square, a circle, and so on. The values and their order are those of the C library
(`HPDF_LineAnnotEndingStyle`).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No line ending. Writes `/None` |
| `Square` | 1 | A square centered on the endpoint. Closed. Writes `/Square` |
| `Circle` | 2 | A circle centered on the endpoint. Closed. Writes `/Circle` |
| `Diamond` | 3 | A diamond (a square turned by 45 degrees) centered on the endpoint. Closed. Writes `/Diamond` |
| `OpenArrow` | 4 | Two short lines that form an arrowhead pointing away from the line. Writes `/OpenArrow` |
| `ClosedArrow` | 5 | A closed arrowhead (a triangle) pointing away from the line. Closed. Writes `/ClosedArrow` |
| `Butt` | 6 | A short line perpendicular to the line. Writes `/Butt` |
| `ROpenArrow` | 7 | An open arrowhead pointing back along the line (reverse). Writes `/ROpenArrow` |
| `RClosedArrow` | 8 | A closed arrowhead pointing back along the line (reverse). Closed. Writes `/RClosedArrow` |
| `Slash` | 9 | A short line at 30 degrees clockwise from the perpendicular to the line. Writes `/Slash` |

How the endings are drawn in the generated appearances:

- Their size grows with the line width: the size is 6 + 3 × width points (9 points for a line 1 point wide). The
  sides of an arrowhead are as long as the size, at an angle of 30 degrees to the line; `Butt` and `Slash` are as long
  as the size; a square, circle or diamond is two thirds of the size across.
- The outline is drawn in the color of the line. Closed endings are filled: on a line or polyline annotation with the
  interior color ([SetInteriorRgbColor()](HpdfGeometricAnnotation.md#method-setinteriorrgbcolor); unfilled without
  one), on a free text callout with the text color.
- A line or polyline annotation stops at the edge of a `Square`, `Circle` or `Diamond` ending and at the base of a
  `ClosedArrow`, so the line does not cross an unfilled ending.
- Endings are drawn solid, also on a dashed line.

A value outside the enum makes the setters throw `HpdfException` (`InvalidParameter`).

## Where It Is Used

| Member | Description |
|--------|-------------|
| [HpdfLineAnnotation.SetLineEndingStyle(start, end)](HpdfLineAnnotation.md#method-setlineendingstyle) | The endings at both ends of a line: `/LE [/start /end]` (PDF 1.4) |
| [HpdfFreeTextAnnotation.SetLineEndingStyle(style)](HpdfFreeTextAnnotation.md#method-setlineendingstyle) | The ending at the start of a callout line: `/LE /style` (PDF 1.6) |
| [HpdfPolyLineAnnotation.SetLineEndingStyle(start, end)](HpdfPolyLineAnnotation.md#method-setlineendingstyle) | The endings at the first and the last vertex of a polyline: `/LE [/start /end]` |

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

float y = 750;
foreach (var style in Enum.GetValues<HpdfLineEndingStyle>())
{
    var line = page.CreateLineAnnotation(new HpdfPoint(100, y), new HpdfPoint(300, y), style.ToString());
    line.SetLineEndingStyle(style, style);
    line.SetInteriorRgbColor(1f, 0.8f, 0.8f);
    y -= 30;
}

pdf.SaveToFile("endings.pdf");
```

## Related Types

- [HpdfLineAnnotation](HpdfLineAnnotation.md), [HpdfPolyLineAnnotation](HpdfPolyLineAnnotation.md),
  [HpdfFreeTextAnnotation](HpdfFreeTextAnnotation.md)
- [Reserved Types](../types/ReservedTypes.md) - `HpdfLineAnnotEndingStyle` in `Haru.Types`, an unused copy of this
  enum from the C library

---

[Back to Index](../../INDEX.md)
