# HpdfIccProfile Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfIccProfile` is an ICC color profile embedded in the document. A profile describes what color numbers mean on a
device (a monitor, a printing condition, a camera), so a viewer with color management reproduces the colors the same
everywhere. In a PDF it is the color space `[/ICCBased profile]` of images and of fill and stroke colors, and the
profile of the PDF/A output intent.

---

## Getting an Instance

The class has no public constructor:

| Member | Gives |
|--------|-------|
| `HpdfDocument.LoadIccProfile(string path)` | A profile from a file (`.icc`, `.icm`); `HPDF_LoadIccProfileFromFile` of the C library |
| `HpdfDocument.LoadIccProfile(byte[] data)` | A profile from memory (copied); `HPDF_ICC_LoadIccFromMem` |
| `HpdfDocument.SRgbProfile` | The built-in sRGB profile |
| `HpdfImage.IccProfile` | The profile of an image, also one carried over from a PNG or JPEG file |

The component count is read from the profile header (the C library takes it as an argument). Profiles of gray (1
component), RGB (3) and CMYK (4) color spaces are supported; other profiles (Lab, multi-color) throw `HpdfException`
(`InvalidIccComponentNum`), data that is not an ICC profile `InvalidParameter`.

**One copy per document.** The document keeps its profiles by content: loading the same profile again, by
`LoadIccProfile` or from image files, returns the object already embedded, so a profile is written once however many
images and colors use it. A profile is embedded (a compressed stream with `/N` and `/Alternate`) when it is loaded,
whether it is used or not; `HpdfDocument.IccProfiles` lists them.

**The built-in sRGB profile** is the 456-byte ICC v2 profile `sRGB-v2-micro.icc` of
[Compact-ICC-Profiles](https://github.com/saucecontrol/Compact-ICC-Profiles) by Clinton Ingram, released under CC0
(public domain). Colors in it look the same as device RGB in practice; use it where a profile is required, such as
the PDF/A output intent, which uses it by default.

**PDF versions.** A profile raises the document version to what its ICC version needs when it is used (ISO 32000-1,
8.6.5.5): version 2.0 and 2.1 need PDF 1.3, other version 2 and 3 profiles 1.4, version 4.0 1.5, 4.1 1.6, 4.2 and
later 1.7.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var adobeRgb = pdf.LoadIccProfile("AdobeRGB1998.icc");

// An image whose numbers are Adobe RGB values
using var photo = pdf.LoadPngImageFromFile("photo-adobergb.png");
photo.SetIccProfile(adobeRgb);          // not needed if the PNG embeds the profile itself
page.DrawImage(photo, 50, 400, 300, 200);

// A fill color in the same space
page.SetIccFill(adobeRgb, 0.1f, 0.5f, 0.9f);
page.Rectangle(50, 650, 300, 50);
page.Fill();

Console.WriteLine($"{adobeRgb.ComponentCount} components, ICC {adobeRgb.ProfileVersion}, class {adobeRgb.DeviceClass}");
pdf.SaveToFile("icc.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [ComponentCount](#property-componentcount) | `int` | 1 (gray), 3 (RGB) or 4 (CMYK) |
| [AlternateColorSpace](#property-alternatecolorspace) | `HpdfColorSpace` | The device color space a reader uses without the profile |
| [DeviceClass](#property-deviceclass) | `string` | The profile class: `mntr`, `prtr`, `scnr`, ... |
| [ProfileVersion](#property-profileversion) | `Version` | The ICC version of the profile |
| [LocalName](#property-localname) | `string` | The resource name on pages |
| [StreamObject](#property-streamobject) | `HpdfStreamObject` | The embedded profile stream |

---

## Detailed Descriptions

#### Property: *ComponentCount*

**Declaration:**
```csharp
public int ComponentCount { get; }
```

**Description:**
The number of color components of the profile's color space, read from the header and written as `/N`: 1 for gray,
3 for RGB, 4 for CMYK. An image or color that uses the profile has as many components.

---

#### Property: *AlternateColorSpace*

**Declaration:**
```csharp
public HpdfColorSpace AlternateColorSpace { get; }
```

**Description:**
`DeviceGray`, `DeviceRgb` or `DeviceCmyk`, written as `/Alternate`: what a reader without color management uses
instead of the profile.

---

#### Property: *DeviceClass*

**Declaration:**
```csharp
public string DeviceClass { get; }
```

**Description:**
The profile class of the header without trailing spaces: `mntr` (monitor), `prtr` (output device, printer), `scnr`
(input device), `spac` (color space) and others. A PDF/A output intent needs `mntr` or `prtr`.

---

#### Property: *ProfileVersion*

**Declaration:**
```csharp
public Version ProfileVersion { get; }
```

**Description:**
The ICC version of the profile, such as 2.1 or 4.3 (major and minor; the bug-fix digit is left out). PDF/A-1 allows
version 2 profiles only.

---

#### Property: *LocalName*

**Declaration:**
```csharp
public string LocalName { get; }
```

**Description:**
The name of the color space in the `/ColorSpace` resources of the pages that draw with the profile
(`SetIccFill`, `SetIccStroke`): `HaruCS` followed by a number.

---

#### Property: *StreamObject*

**Declaration:**
```csharp
public HpdfStreamObject StreamObject { get; }
```

**Description:**
The profile stream, an indirect object: `/N`, `/Alternate` and the profile data, compressed with Flate. It is the
second element of the ICC-based color spaces and the `/DestOutputProfile` of a PDF/A output intent.

---

## Remarks

- **PDF/A.** The PDF/A output intent carries a profile: the built-in sRGB, or the one passed to
  [HpdfDocument.SetPdfACompliance(conformance, profile, outputCondition)](HpdfDocument.md#setpdfacompliancestring-hpdficcprofile-string).
  PDF/A-1 allows ICC version 2 profiles only: saving fails for a version 4 profile set with `SetIccProfile` or used by
  `SetIccFill` / `SetIccStroke`, while a version 4 profile carried over from an image file is left out (the image is
  written in its device color space; the profile stream stays in the file, and the PDF version raised by the profile
  stays too).
- **Viewers.** Color management is up to the viewer: color-managed viewers apply the profiles (checked with MuPDF,
  whose colors match LittleCMS); some viewers show ICC-based colors as device colors.

---

## Related Types

- [HpdfDocument](HpdfDocument.md) - `LoadIccProfile()`, `SRgbProfile`, `IccProfiles`, `SetPdfACompliance()`
- [HpdfImage](HpdfImage.md) - `SetIccProfile()`, `IccProfile`
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - `SetIccFill()`, `SetIccStroke()`
- [HpdfColorSpace](../types/HpdfColorSpace.md) - `IccBased`
- [Images Guide](../../guides/ImagesGuide.md) - "Color Profiles"

---

[Back to Index](../../INDEX.md)
