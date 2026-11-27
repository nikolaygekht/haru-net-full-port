# HpdfDocumentExtensions Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfDocumentExtensions` is a static class of extension methods for [HpdfDocument](../core/HpdfDocument.md) that
mirror functions of the C library: `GetFont` returns a Base14 font by name (or a font registered earlier), and
`SetCompressionMode` selects which content is compressed.

---

## Getting an Instance

The class is static. Its methods are extension methods: with `using Haru.Doc;` in place, call them on a document.

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
var font = pdf.GetFont("Helvetica");
pdf.SetCompressionMode(Haru.Types.HpdfCompressionMode.Text);
```

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.SetCompressionMode(HpdfCompressionMode.All);

var page = pdf.AddPage();

var title = pdf.GetFont("Helvetica-Bold");
page.SetFontAndSize(title, 24);
page.TextOut(50, 750, "Base14 fonts");

// Times with the Central European code page
var centralEuropean = pdf.GetFont("Times-Roman", "CP1250");
page.SetFontAndSize(centralEuropean, 14);
page.TextOut(50, 700, "Zażółć gęślą jaźń");

pdf.SaveToFile("document-extensions.pdf");
```

---

## Properties

`HpdfDocumentExtensions` is a static class and has no properties.

---

## Methods

| Name | Description |
|------|-------------|
| [GetFont()](#method-getfont) | Returns a Base14 font by its PostScript name, or a font registered with `RegisterFont` |
| [SetCompressionMode()](#method-setcompressionmode) | Sets `HpdfDocument.CompressionMode` (`HpdfCompressionMode` or `uint` flags) |

---

## Detailed Descriptions

#### Method: *GetFont()*

**Declaration:**
```csharp
public static HpdfFont GetFont(this HpdfDocument document, string fontName, string? encodingName = null)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| document | [HpdfDocument](../core/HpdfDocument.md) | The document |
| fontName | `string` | A Base14 PostScript name such as `"Helvetica-Bold"`, or the `BaseFont` name of a registered font |
| encodingName | `string?` | The encoding of the font: a code page, a CJK CMap or `Identity-H` / `Identity-V`. `null`: WinAnsi (code page 1252) for a Base14 font, any encoding for a registered font. See [Encoding names](#encoding-names). |

**Return Value:**
An [HpdfFont](../core/HpdfFont.md) ready for `page.SetFontAndSize(font, size)`. Calls with the same name and encoding
return the same object.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `document` is `null`, or `fontName` is `null` or empty |
| `HpdfException` (`InvalidParameter`) | `fontName` is neither a registered font (with this encoding, if one is given) nor one of the 14 Base14 names ("Unknown font name") |
| `HpdfException` (`InvalidEncodingName`) | `encodingName` is not a known encoding name |

**Description:**
`GetFont` **does not load font files.** It looks for the font in this order:

1. `document.FontRegistry` under the key `"<fontName>#<encoding key>"`. This finds fonts created by earlier
   `GetFont` calls and fonts registered with `HpdfDocument.RegisterFont(font, encoding)`, which stores a font under
   `"<font.BaseFont>#<encoding key>"`. The encoding key is the CMap name of a CJK encoding as the CMap spells it
   (`"90ms-RKSJ-V"`), else the code page, with `-V` appended for vertical writing (`"1251"`, `"65001"` for `"UTF-8"`
   and `"Identity-H"`, `"65001-V"` for `"Identity-V"`). So the names of one encoding (`"CP1251"`, `"windows-1251"`)
   give one key, and encodings that share a code page (`"90ms-RKSJ-H"`, `"90msp-RKSJ-H"`, `"90ms-RKSJ-V"`) give
   different keys. `RegisterFont` without an encoding uses the font's own: the CMap of a predefined CJK font,
   `Identity-H` or `Identity-V` for an embedded CID font, the code page of any other font.
2. A Base14 font with exactly this PostScript name (case-sensitive), which is created with the code page from
   `encodingName`.
3. If no encoding is given: a registered font with this `BaseFont` name, whatever encoding it is registered with
   (the first one found, if the font is registered with several). If a code page name is given (not a CMap name and
   not `"Identity-V"`, for example `"CP932"`): the first registered font with this name and this code page that
   writes horizontally, such as a predefined font registered under `"90ms-RKSJ-H"`.

The Base14 names:

| Family | Names |
|--------|-------|
| Courier | `Courier`, `Courier-Bold`, `Courier-Oblique`, `Courier-BoldOblique` |
| Helvetica | `Helvetica`, `Helvetica-Bold`, `Helvetica-Oblique`, `Helvetica-BoldOblique` |
| Times | `Times-Roman`, `Times-Bold`, `Times-Italic`, `Times-BoldItalic` |
| Symbol | `Symbol`, `ZapfDingbats` |

A Base14 font is created with the code page from `encodingName` and a resource name `HaruF` followed by a number
(the prefix is [HpdfDocument.GeneratedNamePrefix](../core/HpdfDocument.md#generatednameprefix), which names you choose
yourself should not start with), added to the cross-reference table and stored in `FontRegistry` for the next call. Base14 fonts are not embedded; viewers
supply them.

To use a TrueType, Type 1 or CID font through `GetFont`, load it first (for example
`HpdfTrueTypeFont.LoadFromFile(...).AsFont()`, see [HpdfFont](../core/HpdfFont.md#loading-custom-fonts)), register
it with `pdf.RegisterFont(font)`, and then ask for it by its `BaseFont` name (the PostScript name read from the font
file, for example `"Roboto-Regular"`, not the resource name given when loading). An encoding is needed only to tell
apart several registrations of one font; it must give the encoding the font is registered with:

```csharp
var roboto = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "R1", "fonts/Roboto-Regular.ttf", true, 1251).AsFont();
pdf.RegisterFont(roboto);                                     // key "Roboto-Regular#1251"

var same = pdf.GetFont(roboto.BaseFont, "CP1251");            // the registered font
var alsoSame = pdf.GetFont(roboto.BaseFont);                  // found by the name alone
// pdf.GetFont(roboto.BaseFont, "CP1250") throws "Unknown font name": not registered with this code page
```

One font in several encodings of one code page:

```csharp
var mincho = HpdfCIDFontType0.Create(pdf, "MS-Mincho", "M1", "90ms-RKSJ-H").AsFont();
var minchoVertical = HpdfCIDFontType0.Create(pdf, "MS-Mincho", "M2", "90ms-RKSJ-V").AsFont();
pdf.RegisterFont(mincho);                                     // key "MS-Mincho#90ms-RKSJ-H"
pdf.RegisterFont(minchoVertical);                             // key "MS-Mincho#90ms-RKSJ-V"

var vertical = pdf.GetFont("MS-Mincho", "90ms-RKSJ-V");       // minchoVertical
var horizontal = pdf.GetFont("MS-Mincho", "CP932");           // mincho: the horizontal font of code page 932
```

##### Encoding names

`encodingName` is converted to a code page as follows. `HpdfDocument.RegisterFont(font, encoding)` reads its
encoding name in the same way. Names are not case sensitive.

| `encodingName` | Code page |
|----------------|-----------|
| `null`, `""`, `"WinAnsiEncoding"`, `"WinAnsi"`, `"StandardEncoding"`, `"FontSpecific"` | 1252 (WinAnsi) |
| `"MacRomanEncoding"` | 10000 |
| `"UTF-8"`, `"UTF8"`, `"Identity-H"`, `"Identity-V"` | 65001 (`"Identity-V"`: an embedded CID font for vertical writing) |
| `"CP"` followed by a number, such as `"CP1251"`, or the number alone | that number |
| `"ISO8859-n"` or `"ISO-8859-n"` with n = 1..9 | 28590 + n (`"ISO8859-5"` is 28595) |
| `"ISO8859-n"` or `"ISO-8859-n"` with n = 10, 11, 13, 14, 15, 16 | 28600 + (n - 10) |
| `"KOI8-R"` | 20866 |
| CMap names of the CJK encodings: `"GBK-EUC-H"`, `"GB-EUC-H"` / `"ETen-B5-H"` / `"90ms-RKSJ-H"`, `"90msp-RKSJ-H"` / `"EUC-H"` / `"KSCms-UHC-H"`, `"KSCms-UHC-HW-H"` / `"KSC-EUC-H"` and their `-V` forms | the code page of the encoding: 936 (20936 for `GB-EUC`) / 950 / 932 / 20932 / 949 / 51949 |
| any other name known to .NET, such as `"windows-1251"` | the code page of `System.Text.Encoding.GetEncoding(name)` |
| anything else | `HpdfException` (`InvalidEncodingName`) |

For Base14 fonts, these code pages select a matching single-byte encoding: 1250 to 1258, 10000 (Mac OS Roman),
20866 (KOI8-R), 28591 to 28599, 28600, 28601 and 28603 to 28606 (28591, ISO 8859-1, is written as WinAnsi encoding,
which holds all its characters). Any other code page, including 65001 (UTF-8) and the CJK encodings, throws
`HpdfException` (`InvalidEncoderType`): Base14 fonts cannot show CJK or other multi-byte text, use a CID font for
that. A character the encoding does not hold is written as `?`. The glyphs that are actually shown come from the viewer's built-in fonts,
which usually cover Latin alphabets only; for Cyrillic, Greek and other scripts, embed a TrueType font with the
matching code page.

**Example:**
```csharp
var helvetica = pdf.GetFont("Helvetica");
var helveticaAgain = pdf.GetFont("Helvetica", "WinAnsiEncoding");
Console.WriteLine(ReferenceEquals(helvetica, helveticaAgain));   // True: both use code page 1252

var latin2 = pdf.GetFont("Helvetica", "ISO8859-2");
page.SetFontAndSize(latin2, 12);
page.TextOut(50, 500, "Příliš žluťoučký kůň");
```

---

#### Method: *SetCompressionMode()*

**Declarations:**
```csharp
public static void SetCompressionMode(this HpdfDocument document, HpdfCompressionMode mode)
public static void SetCompressionMode(this HpdfDocument document, uint mode)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| document | [HpdfDocument](../core/HpdfDocument.md) | The document |
| mode | [HpdfCompressionMode](../types/HpdfCompressionMode.md) | Compression flags; combine several with the bitwise OR operator |
| mode | `uint` | The same flags as a number (`HPDF_COMP_*` values of the C library); converted to `HpdfCompressionMode` without validation |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `document` is `null` |

**Description:**
Same as assigning `document.CompressionMode`. `HpdfCompressionMode.Text` (or `All`) compresses the content streams of
all pages with FlateDecode, including pages added before the call; without it, page content is written uncompressed.
Image data and font programs are always compressed, whatever the mode; the `Image` and `Metadata` flags exist for
compatibility with the C library. The default for a new document is `None`.

**Example:**
```csharp
pdf.SetCompressionMode(HpdfCompressionMode.Text);
pdf.SetCompressionMode(1u);   // the same, as HPDF_COMP_TEXT
```

---

## Related Types

- [HpdfDocument](../core/HpdfDocument.md) - `RegisterFont()`, `FontRegistry`, `CompressionMode`
- [HpdfFont](../core/HpdfFont.md) - Fonts and how to load font files
- [HpdfCompressionMode](../types/HpdfCompressionMode.md) - Compression flags
- [Fonts Guide](../../guides/FontsGuide.md)

---

[Back to Index](../../INDEX.md)
