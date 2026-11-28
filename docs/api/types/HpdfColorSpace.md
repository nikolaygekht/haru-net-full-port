# HpdfColorSpace Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfColorSpace` names the PDF color spaces. In Haru.NET it is used to describe the pixel format of raw image data
passed to `HpdfImage.LoadRawImageFromMem`, to report which color space the current fill and stroke colors of a page
use (`page.GraphicsState.FillColorSpace` / `StrokeColorSpace`), and as the alternate color space of an
[HpdfIccProfile](../core/HpdfIccProfile.md).

The three device color spaces and `IccBased` are used. The other values are kept from the C library; they are not
produced by the library and `LoadRawImageFromMem` rejects them.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `DeviceGray` | 0 | One component per pixel or color (gray level). Written as `/DeviceGray`. Set by `SetGrayFill` / `SetGrayStroke`; the initial color space of a page. |
| `DeviceRgb` | 1 | Three components: red, green, blue. Written as `/DeviceRGB`. Set by `SetRgbFill` / `SetRgbStroke`. |
| `DeviceCmyk` | 2 | Four components: cyan, magenta, yellow, black. Written as `/DeviceCMYK`. Set by `SetCmykFill` / `SetCmykStroke`. |
| `CalGray` | 3 | CIE-based calibrated gray. Not supported. |
| `CalRgb` | 4 | CIE-based calibrated RGB. Not supported. |
| `Lab` | 5 | CIE L\*a\*b\*. Not supported. |
| `IccBased` | 6 | The color space of an ICC profile (`[/ICCBased profile]`). Set by `SetIccFill` / `SetIccStroke`; images get it with `HpdfImage.SetIccProfile`. Not accepted by `LoadRawImageFromMem`. |
| `Separation` | 7 | Spot color. Not supported. |
| `DeviceN` | 8 | Multiple spot colorants. Not supported. |
| `Indexed` | 9 | Palette (color table). Not supported. |
| `Pattern` | 10 | Pattern (tiling or shading). Not supported. |
| `Eof` | 11 | Sentinel kept from the C library. **Not a valid argument.** |

`LoadRawImageFromMem` throws an `HpdfException` (`InvalidParameter`) for every value other than `DeviceGray`,
`DeviceRgb` and `DeviceCmyk`.

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A 2 x 2 RGB image, 8 bits per component: red, green / blue, white
byte[] pixels =
{
    255, 0, 0,    0, 255, 0,
    0, 0, 255,    255, 255, 255
};
using var image = HpdfImage.LoadRawImageFromMem(pdf.Xref, "Im1", pixels, 2, 2, HpdfColorSpace.DeviceRgb, 8);
page.DrawImage(image, 100, 600, 100, 100);

page.SetCmykFill(0, 1, 1, 0);
Console.WriteLine(page.GraphicsState.FillColorSpace);  // DeviceCmyk

pdf.SaveToFile("colorspace.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfImage.LoadRawImageFromMem(HpdfXref xref, string localName, byte[] data, uint width, uint height, HpdfColorSpace colorSpace, byte bitsPerComponent)` | Creates an image from uncompressed pixel data; `colorSpace` gives the number of components per pixel (1, 3 or 4). See [HpdfImage](../core/HpdfImage.md). |
| `HpdfGraphicsState.FillColorSpace { get; set; }` | Color space of the current fill color (`page.GraphicsState`), updated by the `Set...Fill` methods. |
| `HpdfGraphicsState.StrokeColorSpace { get; set; }` | Color space of the current stroke color, updated by the `Set...Stroke` methods. |

## Related Types

- [HpdfRgbColor](HpdfRgbColor.md) - RGB color value
- [HpdfCmykColor](HpdfCmykColor.md) - CMYK color value
- [HpdfImage](../core/HpdfImage.md) - image loading
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - color operations

---

[Back to Index](../../INDEX.md)
