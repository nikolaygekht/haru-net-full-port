# HpdfCompressionMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfCompressionMode` is a set of flags (`[Flags]`, underlying type `uint`) that selects which content of the
document is compressed with the Flate (zlib/deflate) filter. Set it on the document **before adding pages**.

Only the `Text` flag has an effect: it compresses the content streams of pages (text, paths and image placement
operators). The mode applies to every page of the document, including pages added before it was set, so it can be
changed at any time before saving. Without `Text`, page content streams are written uncompressed.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No compression of page content streams. This is the default of a new document. |
| `Text` | 1 | Compress the content streams of all pages (`/Filter /FlateDecode`), and the appearance streams that the library generates for markup annotations. |
| `Image` | 2 | Compatibility value from the C library (`HPDF_COMP_IMAGE`) with no effect. Image data is compressed whatever the mode: PNG and raw images are always written with `/FlateDecode` (JPEG images keep their `/DCTDecode` data). |
| `Metadata` | 4 | Compatibility value from the C library (`HPDF_COMP_METADATA`) with no effect. |
| `All` | 15 | All flags (`Text`, `Image`, `Metadata`, plus an unused bit). In practice the same as `Text`. |

Streams outside the page contents do not depend on the mode either: embedded font files and `/ToUnicode` CMaps are
always compressed; form field appearance streams and the PDF/A streams (XMP metadata, ICC output intent profile)
are never compressed. The generated appearance streams of markup annotations follow `Text`.

## Example

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.CompressionMode = HpdfCompressionMode.All;   // also applies to pages added before

var page = pdf.AddPage();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
page.BeginText();
page.SetFontAndSize(font, 12);
page.MoveTextPos(50, 750);
page.ShowText("This content stream is compressed.");
page.EndText();

pdf.SaveToFile("compressed.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfDocument.CompressionMode { get; set; }` | The compression mode of all pages of the document. See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfDocumentExtensions.SetCompressionMode(this HpdfDocument document, HpdfCompressionMode mode)` | Same as setting `CompressionMode`. |
| `HpdfDocumentExtensions.SetCompressionMode(this HpdfDocument document, uint mode)` | Overload that takes the flags as a number (for code ported from the C API). |

[HpdfConst](HpdfConst.md) has the matching byte constants `CompNone`, `CompText`, `CompImage`, `CompMetadata` and
`CompAll`.

## Related Types

- [HpdfDocument](../core/HpdfDocument.md) - `CompressionMode`, `AddPage`
- [HpdfConst](HpdfConst.md) - `Comp*` constants

---

[Back to Index](../../INDEX.md)
