# HpdfConst Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfConst` is a static class of constants carried over from `hpdf_consts.h` of the C library: default graphics
state values, limits from the PDF specification, and the integer flags that the C API used for compression,
permissions and viewer preferences.

Most of these constants are informational. The library itself reads only `LimitMaxNameLen` (a PDF name longer than
127 characters makes `HpdfName` throw an `HpdfException`). The flag groups have typed enum replacements, which
the public API uses; prefer those in new code:

| Constant group | Typed replacement |
|----------------|-------------------|
| Compression mode flags (`Comp...`) | [HpdfCompressionMode](HpdfCompressionMode.md) |
| Permission flags (`Enable...`) | [HpdfPermission](HpdfPermission.md) |
| Viewer preferences (`HideToolbar`, ...) | [HpdfViewerPreferences](HpdfViewerPreferences.md) (namespace `Haru.Doc`) |
| Default page size (`DefPageWidth`, `DefPageHeight`) | `HpdfPage.DefaultWidth`, `HpdfPage.DefaultHeight`, [HpdfPageSize](HpdfPageSize.md)`.A4` |
| Status codes (`Ok`, `NoError`) | Errors are reported with `HpdfException` and `HpdfErrorCode` |

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();

// Typed enum (preferred) and the C-style flag give the same result
pdf.SetCompressionMode(HpdfCompressionMode.All);
pdf.SetCompressionMode(HpdfConst.CompAll);   // uint overload

// Limits as plain numbers
Console.WriteLine(HpdfConst.LimitMaxNameLen);   // 127
Console.WriteLine(HpdfConst.DefMiterLimit);     // 10
```

---

## Status Codes

| Name | Type | Value | Notes |
|------|------|-------|-------|
| `Ok` | `int` | 0 | Success code of the C API. Not used: Haru.NET throws `HpdfException` on errors |
| `NoError` | `int` | 0 | Same as `Ok` |

## Buffer Sizes and PDF Item Lengths

Internal sizes of the C implementation. Not used by Haru.NET.

| Name | Type | Value |
|------|------|-------|
| `TmpBufSize` | `int` | 512 |
| `ShortBufSize` | `int` | 32 |
| `RealLen` | `int` | 11 |
| `IntLen` | `int` | 11 |
| `TextDefaultLen` | `int` | 256 |
| `UnicodeHeaderLen` | `int` | 2 |
| `DateTimeStrLen` | `int` | 23 |
| `ByteOffsetLen` | `int` | 10 |
| `ObjIdLen` | `int` | 7 |
| `GenNoLen` | `int` | 5 |

## Default Graphics State Values

Initial values defined by the PDF specification and the C library. The library does not read these constants; the
actual initial values come from `HpdfGraphicsState` and `HpdfCatalog`, which match them except where noted.

| Name | Type | Value | Notes |
|------|------|-------|-------|
| `DefFont` | `string` | `"Helvetica"` | A new page has no font; call `SetFontAndSize` before showing text |
| `DefPageLayout` | [HpdfPageLayout](HpdfPageLayout.md) | `SinglePage` | Value `HpdfCatalog.PageLayout` reports when none is set |
| `DefPageMode` | [HpdfPageMode](HpdfPageMode.md) | `UseNone` | Value `HpdfCatalog.PageMode` reports when none is set |
| `DefWordSpace` | `float` | 0 | |
| `DefCharSpace` | `float` | 0 | |
| `DefFontSize` | `float` | 10 | A new graphics state has font size 0 (no font set) |
| `DefHScaling` | `float` | 100 | Horizontal scaling in percent |
| `DefLeading` | `float` | 0 | |
| `DefRenderingMode` | [HpdfTextRenderingMode](HpdfTextRenderingMode.md) | `Fill` | |
| `DefRise` | `float` | 0 | |
| `DefLineWidth` | `float` | 1 | |
| `DefLineCap` | [HpdfLineCap](HpdfLineCap.md) | `ButtEnd` | |
| `DefLineJoin` | [HpdfLineJoin](HpdfLineJoin.md) | `MiterJoin` | |
| `DefMiterLimit` | `float` | 10 | |
| `DefFlatness` | `float` | 1 | |
| `DefPageNum` | `int` | 1 | |
| `BsDefWidth` | `float` | 1 | Default border style width of annotations |

## Default Page Size

| Name | Type | Value | Notes |
|------|------|-------|-------|
| `DefPageWidth` | `float` | 595.276 | A4 width in points; same value as `HpdfPage.DefaultWidth` |
| `DefPageHeight` | `float` | 841.89 | A4 height in points; same value as `HpdfPage.DefaultHeight` |

## Compression Mode Flags

Replaced by [HpdfCompressionMode](HpdfCompressionMode.md), which has the same numeric values. Because
`SetCompressionMode` has a `uint` overload, these `byte` constants can still be passed to it.

| Name | Type | Value | Typed equivalent |
|------|------|-------|------------------|
| `CompNone` | `byte` | 0x00 | `HpdfCompressionMode.None` |
| `CompText` | `byte` | 0x01 | `HpdfCompressionMode.Text` |
| `CompImage` | `byte` | 0x02 | `HpdfCompressionMode.Image` |
| `CompMetadata` | `byte` | 0x04 | `HpdfCompressionMode.Metadata` |
| `CompAll` | `byte` | 0x0F | `HpdfCompressionMode.All` |
| `CompMask` | `byte` | 0xFF | none (bit mask of the C library) |

## Permission Flags

Replaced by [HpdfPermission](HpdfPermission.md), which `HpdfDocument.SetEncryption` takes. The numeric values are
the same bits; the names differ.

| Name | Type | Value | Typed equivalent |
|------|------|-------|------------------|
| `EnableRead` | `int` | 0 | `HpdfPermission.None` |
| `EnablePrint` | `int` | 4 | `HpdfPermission.Print` |
| `EnableEditAll` | `int` | 8 | `HpdfPermission.Edit` |
| `EnableCopy` | `int` | 16 | `HpdfPermission.Copy` |
| `EnableEdit` | `int` | 32 | `HpdfPermission.EditAnnotations` |

`HpdfPermission` also has `FillForm`, `ExtractAccess`, `Assemble` and `PrintHighQuality`, which have no constant here.

## Viewer Preferences

Replaced by [HpdfViewerPreferences](HpdfViewerPreferences.md) (namespace `Haru.Doc`), set through
`HpdfCatalog.ViewerPreferences` (`pdf.Catalog.ViewerPreferences`). The numeric values are the same.

| Name | Type | Value | Typed equivalent |
|------|------|-------|------------------|
| `HideToolbar` | `int` | 1 | `HpdfViewerPreferences.HideToolbar` |
| `HideMenubar` | `int` | 2 | `HpdfViewerPreferences.HideMenubar` |
| `HideWindowUi` | `int` | 4 | `HpdfViewerPreferences.HideWindowUI` |
| `FitWindow` | `int` | 8 | `HpdfViewerPreferences.FitWindow` |
| `CenterWindow` | `int` | 16 | `HpdfViewerPreferences.CenterWindow` |
| `PrintScalingNone` | `int` | 32 | `HpdfViewerPreferences.PrintScalingNone` |

## Object Limitations

Implementation limits from the PDF specification (PDF 1.4 and later). Only `LimitMaxNameLen` and `LimitMaxGState`
are enforced by the library.

| Name | Type | Value | Notes |
|------|------|-------|-------|
| `LimitMaxInt` | `int` | 2147483647 | |
| `LimitMinInt` | `int` | -2147483647 | |
| `LimitMaxReal` | `float` | 3.4E38 | |
| `LimitMinReal` | `float` | -3.4E38 | |
| `LimitMaxStringLen` | `int` | 2147483646 | |
| `LimitMaxNameLen` | `int` | 127 | Enforced: longer names throw `HpdfException` (`NameOutOfRange`) |
| `LimitMaxArray` | `int` | 8388607 | |
| `LimitMaxDictElement` | `int` | 8388607 | |
| `LimitMaxXrefElement` | `int` | 8388607 | |
| `LimitMaxGState` | `int` | 28 | Enforced: maximum `GSave` nesting; a further `GSave` throws `HpdfException` (`ExceedGStateLimit`) |
| `LimitMaxDeviceN` | `int` | 8 | |
| `LimitMaxDeviceNV15` | `int` | 32 | DeviceN limit for PDF 1.5 |
| `LimitMaxCid` | `int` | 65535 | |
| `MaxGenerationNum` | `int` | 65535 | |

## Page Size Limits

Not enforced by the library: `HpdfPage.Width` and `Height` accept any value greater than 0 and up to 100000 points,
and `SetSize(float, float)` does not check its arguments.

| Name | Type | Value |
|------|------|-------|
| `MinPageHeight` | `float` | 3 |
| `MinPageWidth` | `float` | 3 |
| `MaxPageHeight` | `float` | 14400 |
| `MaxPageWidth` | `float` | 14400 |
| `MinMagnificationFactor` | `float` | 8 |
| `MaxMagnificationFactor` | `float` | 3200 |

## Property Value Limits

Ranges the C library checks for page and text properties. Haru.NET does not use these constants; its own checks are
described with each method in [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) and
[HpdfPageText](../extensions/HpdfPageText.md) (for example, horizontal scaling only has to be positive).

| Name | Type | Value |
|------|------|-------|
| `MinPageSize` | `float` | 3 |
| `MaxPageSize` | `float` | 14400 |
| `MinHorizontalScaling` | `float` | 10 |
| `MaxHorizontalScaling` | `float` | 300 |
| `MinWordSpace` | `float` | -30 |
| `MaxWordSpace` | `float` | 300 |
| `MinCharSpace` | `float` | -30 |
| `MaxCharSpace` | `float` | 300 |
| `MaxFontSize` | `float` | 600 |
| `MaxZoomSize` | `float` | 10 |
| `MaxLeading` | `float` | 300 |
| `MaxLineWidth` | `float` | 100 |
| `MaxDashPattern` | `float` | 100 |
| `MaxJwwNum` | `int` | 128 |

## Graphics Mode Flags

Flags for the C library's graphics mode tracking (which operators are allowed in the current state). Not used by
Haru.NET.

| Name | Type | Value |
|------|------|-------|
| `GmodePageDescription` | `int` | 0x0001 |
| `GmodePathObject` | `int` | 0x0002 |
| `GmodeTextObject` | `int` | 0x0004 |
| `GmodeClippingPath` | `int` | 0x0008 |
| `GmodeShading` | `int` | 0x0010 |
| `GmodeInlineImage` | `int` | 0x0020 |
| `GmodeExternalObject` | `int` | 0x0040 |

---

## Related Types

- [HpdfCountry](HpdfCountry.md) and [HpdfLang](HpdfLang.md) - country and language code constants from the same file

---

[Back to Index](../../INDEX.md)
