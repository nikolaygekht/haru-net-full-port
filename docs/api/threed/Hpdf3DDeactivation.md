# Hpdf3DDeactivation Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DDeactivation` tells when a 3D annotation is deactivated, after which the viewer shows the annotation's
appearance (the poster) again or keeps the model in the state chosen. It is passed to
[Hpdf3DAnnotation.SetDeactivation()](../annotations/Hpdf3DAnnotation.md#method-setdeactivation) and written as `/D` of
the activation dictionary `/3DA`.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `PageClose` | 0 | When the page that contains the annotation is closed (the default). Writes `/D /PC` |
| `PageInvisible` | 1 | When the page is no longer visible. Writes `/D /PI` |
| `Explicit` | 2 | When the user deactivates it, for example from the context menu of the model. Writes `/D /XD` |

A value outside the enum makes `SetDeactivation()` throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.ThreeD;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var model = pdf.Load3DModel("part.prc");

var annotation = page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part");
// Load the model (without running its animations) when the page becomes visible, unload it when it scrolls away
annotation.SetActivation(Hpdf3DActivation.PageVisible, Hpdf3DArtworkState.Instantiated);
annotation.SetDeactivation(Hpdf3DDeactivation.PageInvisible);

pdf.SaveToFile("visible.pdf");
```

## Related Types

- [Hpdf3DAnnotation](../annotations/Hpdf3DAnnotation.md) - `SetDeactivation()`
- [Hpdf3DActivation](Hpdf3DActivation.md), [Hpdf3DArtworkState](Hpdf3DArtworkState.md)
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
