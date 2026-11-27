# Hpdf3DLighting Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DLighting` is a lighting scheme of a 3D view: the lights of the model file, no lights, or one of the schemes of
ISO 32000-1. It is passed to [Hpdf3DView.SetLighting()](Hpdf3DView.md#method-setlighting) and written as
`/LS << /Type /3DLightingScheme /Subtype /name >>` (PDF 1.7). The values are in the order of the C library.

## Values

| Name | Value | Lights | Writes |
|------|-------|--------|--------|
| `Artwork` | 0 | The lights of the model file | `/Artwork` |
| `None` | 1 | No lights | `/None` |
| `White` | 2 | Three blue-gray infinite lights | `/White` |
| `Day` | 3 | Three light-gray infinite lights | `/Day` |
| `Night` | 4 | One yellow, one aqua and one blue infinite light | `/Night` |
| `Hard` | 5 | Three gray infinite lights | `/Hard` |
| `Primary` | 6 | One red, one green and one blue infinite light | `/Primary` |
| `Blue` | 7 | Three blue infinite lights | `/Blue` |
| `Red` | 8 | Three red infinite lights | `/Red` |
| `Cube` | 9 | Six gray infinite lights along the axes | `/Cube` |
| `CAD` | 10 | Three gray infinite lights and a light attached to the camera | `/CAD` |
| `Headlamp` | 11 | A single light attached to the camera | `/Headlamp` |

A view without a lighting scheme uses the lights of the model file. A value outside the enum makes `SetLighting()`
throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Doc;
using Haru.ThreeD;

var pdf = new HpdfDocument();
var model = pdf.Load3DModel("part.prc");

var view = model.CreateView("CAD lighting");
view.SetLighting(Hpdf3DLighting.CAD);
```

## Related Types

- [Hpdf3DView](Hpdf3DView.md) - `SetLighting()`
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
