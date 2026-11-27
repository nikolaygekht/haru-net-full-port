# Hpdf3DFormat Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DFormat` is the format of a 3D model: Universal 3D or PRC. The library finds it from the first bytes of the data
when the model is loaded and writes it as the `/Subtype` of the 3D stream; it is returned by
[Hpdf3DModel.Format](Hpdf3DModel.md#property-format).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `U3D` | 0 | Universal 3D (ECMA-363). The data starts with `U3D` and a zero byte. Writes `/Subtype /U3D`; needs PDF 1.6 |
| `PRC` | 1 | Product Representation Compact (ISO 14739-1). The data starts with `PRC`. Writes `/Subtype /PRC`; needs PDF 1.7 with Adobe ExtensionLevel 3, which the document declares in its catalog |

Data that starts with neither signature makes `HpdfDocument.Load3DModel()` throw `HpdfException` (`InvalidU3dData`).
The library does not read the model beyond the signature.

## Example

```csharp
using Haru.Doc;
using Haru.ThreeD;

var pdf = new HpdfDocument();
var model = pdf.Load3DModel("part.prc");

Console.WriteLine(model.Format);   // PRC
Console.WriteLine(pdf.Version);    // Version17
```

## Related Types

- [Hpdf3DModel](Hpdf3DModel.md) - `Format`
- [HpdfDocument](../core/HpdfDocument.md) - `Load3DModel()`
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
