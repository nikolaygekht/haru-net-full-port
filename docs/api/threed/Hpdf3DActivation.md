# Hpdf3DActivation Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DActivation` tells when a 3D annotation is activated, that is when the viewer loads the model and shows it
instead of the annotation's appearance (the poster). It is passed to
[Hpdf3DAnnotation.SetActivation()](../annotations/Hpdf3DAnnotation.md#method-setactivation) and written as `/A` of the
activation dictionary `/3DA`.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `PageOpen` | 0 | When the page that contains the annotation is opened (the default). Writes `/A /PO` |
| `PageVisible` | 1 | When the page becomes visible, for example by scrolling. Writes `/A /PV` |
| `Explicit` | 2 | When the user activates it, for example by clicking the poster. Writes `/A /XA` |

A value outside the enum makes `SetActivation()` throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.ThreeD;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var model = pdf.Load3DModel("part.prc");

// Show the poster until the reader clicks it
var annotation = page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part");
annotation.SetActivation(Hpdf3DActivation.Explicit);

pdf.SaveToFile("click-to-activate.pdf");
```

## Related Types

- [Hpdf3DAnnotation](../annotations/Hpdf3DAnnotation.md) - `SetActivation()`
- [Hpdf3DDeactivation](Hpdf3DDeactivation.md), [Hpdf3DArtworkState](Hpdf3DArtworkState.md)
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
