# HpdfFreeTextIntent Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfFreeTextIntent` tells viewers how a free text annotation is meant to be shown: as a plain text box, as a text
box with a callout line, or as text typed onto the page. It is passed to
[HpdfFreeTextAnnotation.SetIntent()](HpdfFreeTextAnnotation.md#method-setintent) and written as `/IT` (PDF 1.6).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Plain` | 0 | A plain text box. Writes `/IT /FreeText` |
| `Callout` | 1 | A text box with a callout line that points at an area of the page. Writes `/IT /FreeTextCallout`. [SetCallout()](HpdfFreeTextAnnotation.md#method-setcallout) sets it |
| `TypeWriter` | 2 | Text typed onto the page. Writes `/IT /FreeTextTypeWriter`. The generated appearance has no border |

Without a call to `SetIntent()` or `SetCallout()`, `/IT` is absent, which means a plain text box. The intent does not
add or remove the callout line: `SetCallout()` adds it, `RemoveCallout()` removes it (and turns `Callout` into
`Plain`). A value outside the enum makes `SetIntent()` throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F1");
var page = pdf.AddPage();

// Text filled into a printed form, without a border
var name = page.CreateFreeTextAnnotation(new HpdfRect(150, 700, 400, 716), "Anna Schmidt", font, 12);
name.SetIntent(HpdfFreeTextIntent.TypeWriter);

pdf.SaveToFile("typewriter.pdf");
```

## Related Types

- [HpdfFreeTextAnnotation](HpdfFreeTextAnnotation.md) - `SetIntent()`, `SetCallout()`
- [HpdfLineIntent](HpdfLineIntent.md) - the intent of line annotations
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotIntent` in `Haru.Types`, an unused enum from the C library
  with the intents of all annotation types

---

[Back to Index](../../INDEX.md)
