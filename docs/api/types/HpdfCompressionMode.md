# HpdfCompressionMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfCompressionMode` is a set of flags (`[Flags]`, underlying type `uint`) that selects which content of the
document is compressed with the Flate (zlib/deflate) filter. Set it on the document **before adding pages**.

Only the `Text` flag currently has an effect: it compresses the content streams of pages (text, paths and image
placement operators). The mode is copied to each page when the page is added with `AddPage`, so pages that already
exist keep the mode they were created with. Without `Text`, page content streams are written uncompressed.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No compression of page content streams. This is the default of a new document. |
| `Text` | 1 | Compress the content streams of pages added after the mode is set (`/Filter /FlateDecode`). |
| `Image` | 2 | Has no effect at present. Image data is compressed whatever the mode: PNG and raw images are always written with `/FlateDecode` (JPEG images keep their `/DCTDecode` data). |
| `Metadata` | 4 | Has no effect at present. |
| `All` | 15 | All flags (`Text`, `Image`, `Metadata`, plus an unused bit). In practice the same as `Text`. |

Streams outside the page contents do not depend on the mode either: embedded font files and `/ToUnicode` CMaps are
always compressed; form field appearance streams and the PDF/A streams (XMP metadata, ICC output intent profile)
are never compressed.

## Example

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.CompressionMode = HpdfCompressionMode.All;   // before AddPage

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
| `HpdfDocument.CompressionMode { get; set; }` | The compression mode applied to pages added afterwards. See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfDocumentExtensions.SetCompressionMode(this HpdfDocument document, HpdfCompressionMode mode)` | Same as setting `CompressionMode`. |
| `HpdfDocumentExtensions.SetCompressionMode(this HpdfDocument document, uint mode)` | Overload that takes the flags as a number (for code ported from the C API). |

[HpdfConst](HpdfConst.md) has the matching byte constants `CompNone`, `CompText`, `CompImage`, `CompMetadata` and
`CompAll`.

## Related Types

- [HpdfDocument](../core/HpdfDocument.md) - `CompressionMode`, `AddPage`
- [HpdfConst](HpdfConst.md) - `Comp*` constants

---

[Back to Index](../../INDEX.md)
