# HpdfStampName Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfStampName` selects one of the standard rubber stamps of the PDF specification. It is passed to
`HpdfPage.CreateStampAnnotation()` and written as `/Name` of the [HpdfStampAnnotation](HpdfStampAnnotation.md). The
values and their order are those of the C library (`HPDF_StampAnnotName`).

## Values

| Name | Value | `/Name` | Label | Default color |
|------|-------|---------|-------|---------------|
| `Approved` | 0 | `/Approved` | APPROVED | Green |
| `Experimental` | 1 | `/Experimental` | EXPERIMENTAL | Blue |
| `NotApproved` | 2 | `/NotApproved` | NOT APPROVED | Red |
| `AsIs` | 3 | `/AsIs` | AS IS | Blue |
| `Expired` | 4 | `/Expired` | EXPIRED | Red |
| `NotForPublicRelease` | 5 | `/NotForPublicRelease` | NOT FOR PUBLIC RELEASE | Red |
| `Confidential` | 6 | `/Confidential` | CONFIDENTIAL | Red |
| `Final` | 7 | `/Final` | FINAL | Green |
| `Sold` | 8 | `/Sold` | SOLD | Green |
| `Departmental` | 9 | `/Departmental` | DEPARTMENTAL | Blue |
| `ForComment` | 10 | `/ForComment` | FOR COMMENT | Blue |
| `TopSecret` | 11 | `/TopSecret` | TOP SECRET | Red |
| `Draft` | 12 | `/Draft` | DRAFT | Blue |
| `ForPublicRelease` | 13 | `/ForPublicRelease` | FOR PUBLIC RELEASE | Green |

The label is drawn in the generated appearance, in capitals, with the font of the stamp. The default color (green
`0.1 0.5 0.1`, red `0.75 0.1 0.1`, blue `0.1 0.2 0.6`) can be changed with the color setters of the annotation.

A value outside the enum makes `CreateStampAnnotation()` throw `HpdfException` (`InvalidParameter`); no annotation
is created.

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var bold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");
var page = pdf.AddPage();

var names = Enum.GetValues<HpdfStampName>();
for (int i = 0; i < names.Length; i++)
{
    float x = 50 + i % 3 * 170;
    float y = 760 - i / 3 * 55;
    page.CreateStampAnnotation(new HpdfRect(x, y, x + 155, y + 40), names[i], $"The {names[i]} stamp", bold);
}

pdf.SaveToFile("stampnames.pdf");
```

## Related Types

- [HpdfStampAnnotation](HpdfStampAnnotation.md) - the stamp annotation
- [Reserved Types](../types/ReservedTypes.md) - `HpdfStampAnnotName` in `Haru.Types`, an unused copy of this enum
  from the C library

---

[Back to Index](../../INDEX.md)
