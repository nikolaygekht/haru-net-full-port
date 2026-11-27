# Hpdf3DArtworkState Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DArtworkState` is the state of the 3D model of an annotation after it is activated or deactivated. It is passed
to [Hpdf3DAnnotation.SetActivation()](../annotations/Hpdf3DAnnotation.md#method-setactivation) (written as `/AIS`) and
[Hpdf3DAnnotation.SetDeactivation()](../annotations/Hpdf3DAnnotation.md#method-setdeactivation) (written as `/DIS`).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Uninstantiated` | 0 | Not loaded: the annotation shows its appearance stream (the poster). Writes `/U`. The default after deactivation; not allowed after activation |
| `Instantiated` | 1 | Loaded and shown, but scripts and animations do not run. Writes `/I` |
| `Live` | 2 | Loaded and live: scripts and animations run, the reader can rotate and zoom the model. Writes `/L`. The default after activation |

`SetActivation()` with `Uninstantiated` and any call with a value outside the enum throw `HpdfException`
(`InvalidParameter`).

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
// Keep the model loaded when the page is closed, so going back to it is fast
annotation.SetDeactivation(Hpdf3DDeactivation.PageClose, Hpdf3DArtworkState.Instantiated);

pdf.SaveToFile("kept.pdf");
```

## Related Types

- [Hpdf3DAnnotation](../annotations/Hpdf3DAnnotation.md) - `SetActivation()`, `SetDeactivation()`
- [Hpdf3DActivation](Hpdf3DActivation.md), [Hpdf3DDeactivation](Hpdf3DDeactivation.md)
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
