# HpdfAnnotationIcon Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfAnnotationIcon` selects the icon a viewer shows for a sticky note ([HpdfTextAnnotation](HpdfTextAnnotation.md)).
It is passed to `HpdfPage.CreateTextAnnotation()` and written as the `/Name` of the annotation. The viewer draws the
icon; its look differs between viewers.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Comment` | 0 | A speech bubble, for general comments. Writes `/Name /Comment`. |
| `Key` | 1 | A key, for important remarks. Writes `/Name /Key`. |
| `Note` | 2 | A note sheet. The default of `CreateTextAnnotation()`. Writes `/Name /Note`. |
| `Help` | 3 | A question mark, for questions. Writes `/Name /Help`. |
| `NewParagraph` | 4 | A mark for "start a new paragraph here". Writes `/Name /NewParagraph`. |
| `Paragraph` | 5 | A paragraph sign. Writes `/Name /Paragraph`. |
| `Insert` | 6 | A caret, for "insert text here". Writes `/Name /Insert`. |

A value outside the enum makes `CreateTextAnnotation()` throw `HpdfException` (`AnnotInvalidIcon`); no annotation
is created.

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

float x = 50;
foreach (var icon in Enum.GetValues<HpdfAnnotationIcon>())
{
    page.CreateTextAnnotation(new HpdfRect(x, 750, x + 20, 770), $"The {icon} icon", icon);
    x += 60;
}

pdf.SaveToFile("icons.pdf");
```

## Related Types

- [HpdfTextAnnotation](HpdfTextAnnotation.md) - the sticky note
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotIcon` in `Haru.Types`, an unused copy of this enum from the
  C library

---

[Back to Index](../../INDEX.md)
