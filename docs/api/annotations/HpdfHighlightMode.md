# HpdfHighlightMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfHighlightMode` is the visual feedback a viewer gives while the mouse button is pressed on a link or a form field
widget. It is written as `/H` in the annotation dictionary by
[HpdfLinkAnnotation.SetHighlightMode()](HpdfLinkAnnotation.md#method-sethighlightmode) and
[HpdfWidgetAnnotation.SetHighlightMode()](../forms/HpdfWidgetAnnotation.md#method-sethighlightmode). Without a call,
`/H` is absent and viewers use `InvertBox`.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `NoHighlight` | 0 | No highlighting. Writes `/H /N`. |
| `InvertBox` | 1 | The colors inside the annotation rectangle are inverted. Writes `/H /I`. The PDF default. |
| `InvertBorder` | 2 | The colors of the border are inverted. Writes `/H /O`. |
| `DownAppearance` | 3 | The annotation's down appearance (`/AP /D`) is shown. Writes `/H /P`. The library creates no down appearances, so this shows the normal appearance (if any). |

A value outside the enum makes both methods throw `HpdfException` (`InvalidParameter`); the annotation keeps its
previous mode.

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var link = page.CreateLinkAnnotation(new HpdfRect(50, 700, 250, 720), "https://example.com");
link.SetHighlightMode(HpdfHighlightMode.InvertBorder);

var accept = new HpdfCheckbox(pdf.Xref, "Accept");
var widget = accept.CreateWidget(new HpdfRect(50, 650, 65, 665));
widget.SetHighlightMode(HpdfHighlightMode.NoHighlight);
page.AddWidgetAnnotation(widget);
form.AddField(accept);

pdf.SaveToFile("highlight.pdf");
```

## Related Types

- [HpdfLinkAnnotation](HpdfLinkAnnotation.md) - links
- [HpdfWidgetAnnotation](../forms/HpdfWidgetAnnotation.md) - form field widgets
- [Reserved Types](../types/ReservedTypes.md) - `HpdfAnnotHighlightMode` in `Haru.Types`, an unused copy of this
  enum from the C library

---

[Back to Index](../../INDEX.md)
