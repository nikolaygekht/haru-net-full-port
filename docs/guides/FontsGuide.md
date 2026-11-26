# Fonts Guide

[← Back to Documentation Index](../INDEX.md)

This guide explains which fonts Haru.NET can use, how to load each kind, which character sets they cover and how to
choose between them. Showing, positioning and wrapping text is covered in the [Text Guide](TextGuide.md).

**Namespaces:** `Haru.Doc`, `Haru.Font`, `Haru.Font.CID` (CJK fonts)

---

## Font Kinds at a Glance

| Kind | Class | Source | Embedded | Characters |
|------|-------|--------|----------|------------|
| Standard (Base14) | `HpdfFont` | Built into every PDF viewer | Never | Latin, through a single-byte encoding |
| TrueType | `HpdfTrueTypeFont` | `.ttf` file | Optional | One single-byte code page per font object |
| Type 1 | `HpdfType1Font` | `.afm` metrics + optional `.pfb` program | When a `.pfb` is given | One single-byte code page per font object |
| Predefined CJK | `HpdfCIDFontType0` | Built-in metrics of 11 common Asian fonts | Never | Chinese, Japanese or Korean |
| Embedded CID | `HpdfCIDFont` | `.ttf` file | Always | CJK code pages, or any Unicode text with code page 65001 |

All of them end up as an `HpdfFont`, the type that `SetFontAndSize` and the measuring methods take. The loaders return
their own class; call `.AsFont()` to get the `HpdfFont`:

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

HpdfFont helvetica = pdf.GetFont("Helvetica");                       // standard font
HpdfFont roboto = HpdfTrueTypeFont
    .LoadFromFile(pdf.Xref, "Roboto", "fonts/Roboto-Regular.ttf", true)
    .AsFont();                                                         // TrueType font

page.SetFontAndSize(helvetica, 14);
page.TextOut(50, 750, "Helvetica is built into every viewer");
page.SetFontAndSize(roboto, 14);
page.TextOut(50, 720, "Roboto is embedded in the file");

pdf.SaveToFile("fonts.pdf");
```

The font files used in this guide (`Roboto-Regular.ttf`, `noto.ttf`, `noto-jp.ttf`, `a010013l.afm`/`.pfb`) are in
`cs-src/Haru.Demos/Resource/demo/ttfont` and `cs-src/Haru.Demos/Resource/demo/type1` of the repository. The examples
expect them in a `fonts` folder next to the program.

The later snippets assume the `pdf` and `page` variables of this first example.

### Rules that apply to every font

- A font belongs to the document whose `Xref` (or `HpdfDocument`) it was created with. Do not use it in another document.
- Each font has a **local name** (`"Roboto"` above), the name the page content uses to refer to it. Two different fonts
  used on the same page need different local names; otherwise `SetFontAndSize` throws `HpdfException`
  (`DuplicateRegistration`). `GetFont` chooses unique names by itself, starting with `HpdfDocument.GeneratedNamePrefix`
  (`"Haru"`); do not start your own names with it.
- Create a font once and reuse the object on every page. Every loader call adds a new font to the file, and for
  embedded fonts a new copy of the font program.
- The loader classes implement `IDisposable`. If you dispose them, do it after the document is saved.

---

## Standard Fonts

The 14 standard fonts are available in every PDF viewer, so they are never embedded and add almost nothing to the file
size.

| Family | Names |
|--------|-------|
| Helvetica (sans-serif) | `Helvetica`, `Helvetica-Bold`, `Helvetica-Oblique`, `Helvetica-BoldOblique` |
| Times (serif) | `Times-Roman`, `Times-Bold`, `Times-Italic`, `Times-BoldItalic` |
| Courier (monospaced) | `Courier`, `Courier-Bold`, `Courier-Oblique`, `Courier-BoldOblique` |
| Symbols | `Symbol`, `ZapfDingbats` |

There are two ways to get one:

```csharp
// 1. By its PostScript name; the document caches the font per name and encoding
var times = pdf.GetFont("Times-Roman");
var timesAgain = pdf.GetFont("Times-Roman");        // the same object as 'times'

// 2. With the constructor; you choose the local name
var courier = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "Mono");

page.SetFontAndSize(times, 12);
page.TextOut(50, 690, "Times-Roman");
page.SetFontAndSize(courier, 12);
page.TextOut(50, 670, "Courier");
```

`GetFont` accepts only the 14 names above and the names of fonts registered with `RegisterFont`
(see [Using Fonts by Name](#using-fonts-by-name)). It does **not** load font files: `pdf.GetFont("Arial")` or
`pdf.GetFont("fonts/Roboto-Regular.ttf")` throws `HpdfException` ("Unknown font name"). Load files with the classes
described below.

Viewers draw standard fonts with their own built-in or substitute fonts, so the exact look can differ slightly between
viewers. The text is always readable and copyable.

### Encodings for standard fonts

A standard font prints one byte per character. The encoding decides which character each byte stands for; the
default is `WinAnsiEncoding` (Windows code page 1252, Western European). Pass another encoding name as the second
argument of `GetFont`, or a code page number as the last argument of the constructor:

```csharp
var centralEuropean = pdf.GetFont("Helvetica", "CP1250");
page.SetFontAndSize(centralEuropean, 14);
page.TextOut(50, 640, "Zażółć gęślą jaźń - Łódź");

var turkish = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesRoman, "TimesTR", 1254);
page.SetFontAndSize(turkish, 14);
page.TextOut(50, 615, "Günaydın, İstanbul");
```

| Encoding name (`GetFont`) | Code page (constructor) | Script |
|---------------------------|-------------------------|--------|
| `WinAnsiEncoding` (default), `CP1252` | 1252 | Western European |
| `CP1250`, `ISO8859-2` | 1250, 28592 | Central European |
| `CP1254`, `ISO8859-9` | 1254, 28599 | Turkish |
| `CP1257`, `ISO8859-4`, `ISO8859-13` | 1257, 28594, 28603 | Baltic |
| `ISO8859-3`, `ISO8859-10`, `ISO8859-14`, `ISO8859-15`, `ISO8859-16` | 28593, 28600, 28604, 28605, 28606 | Other Latin |
| `CP1258` | 1258 | Vietnamese |
| `CP1251`, `ISO8859-5`, `KOI8-R` | 1251, 28595, 20866 | Cyrillic |
| `CP1253`, `ISO8859-7` | 1253, 28597 | Greek |
| `CP1255`, `ISO8859-8`, `CP1256`, `ISO8859-6`, `ISO8859-11` | 1255, 28598, 1256, 28596, 28601 | Hebrew, Arabic, Thai |

Things to know:

- The standard fonts contain **Latin characters only**. The Cyrillic, Greek, Hebrew, Arabic and Thai encodings are
  accepted, but whether those characters appear depends on the viewer's substitute font. For these scripts, embed a
  TrueType font that contains them.
- Characters that the encoding cannot represent are printed as `?`.
- An encoding name that is not recognized falls back to `WinAnsiEncoding` without an error.
- `Symbol` and `ZapfDingbats` ignore the encoding: each character code selects a symbol from the font's own set.
  For example, in `Symbol` the letters `a`, `b`, `g` print α, β, γ, and in `ZapfDingbats` the digit `4` prints a
  check mark:

```csharp
page.SetFontAndSize(pdf.GetFont("Symbol"), 16);
page.TextOut(50, 585, "abg p");                  // α β γ π
page.SetFontAndSize(pdf.GetFont("ZapfDingbats"), 16);
page.TextOut(50, 560, "4 8 n");                  // check mark, cross, black square
```

---

## TrueType Fonts

Load a `.ttf` file with `HpdfTrueTypeFont.LoadFromFile`:

```csharp
var roboto = HpdfTrueTypeFont.LoadFromFile(
    pdf.Xref,                     // the document's cross-reference table
    "Roboto",                     // local name
    "fonts/Roboto-Regular.ttf",   // font file
    true)                         // embed the font in the PDF
    .AsFont();

page.SetFontAndSize(roboto, 14);
page.TextOut(50, 530, "Roboto: Grüße aus Köln!");
```

| Parameter | Description |
|-----------|-------------|
| `xref` | `pdf.Xref` of the document that will use the font |
| `localName` | Name the page content uses for the font; unique per page |
| `filePath` | Path of the `.ttf` file; a missing file throws `HpdfException` (`FileNotFound`) |
| `embedding` | `true` copies the font file into the PDF (compressed); `false` stores only its name and metrics |
| `codePage` | Optional single-byte code page of the text, 1252 by default |

### Embedding

With `embedding: true` the PDF displays the same on every computer. With `embedding: false` the PDF stays small, but
the viewer must find a font with the same name on the reader's computer; if it cannot, it substitutes another font.
Embed fonts unless you control the computers that will display the document.

An embedded font is stored as a **subset**: only the glyph outlines the document can use are copied, and the font
name in the PDF gets a six-letter tag (`ABCDEF+Roboto-Regular`), as the PDF format asks for subsets. For a TrueType
font these are the glyphs of the characters of its code page; for a CID font the glyphs of the text that was actually
shown. The subset is written when the document is saved, so it does not matter when the text is added. To store the
whole font file instead - for example when the PDF is meant to be edited later with the same font - switch
subsetting off before saving:

```csharp
var roboto = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F1", "fonts/Roboto-Regular.ttf", true);
roboto.EmbedSubset = false;   // store the whole font file
var font = roboto.AsFont();
```

`HpdfCIDFont` has the same `EmbedSubset` property. A font whose embedding permissions forbid subsetting is stored
whole in any case (see [Font Licensing](#font-licensing)), and so is a font without TrueType outlines.

### Code pages

A TrueType font object prints one byte per character, so it covers one single-byte code page (up to 256 characters).
The code page is chosen when the font is loaded; the default is 1252 (Western European):

| Code page | Script |
|-----------|--------|
| 1250 | Central European (Polish, Czech, Hungarian, ...) |
| 1251 | Cyrillic (Russian, Ukrainian, Bulgarian, ...) |
| 1252 | Western European (default) |
| 1253 | Greek |
| 1254 | Turkish |
| 1255 | Hebrew |
| 1256 | Arabic |
| 1257 | Baltic |
| 1258 | Vietnamese |

Other single-byte code pages that .NET supports (for example 28595 for ISO-8859-5) work as well. The font file must
contain the characters; Noto Sans (`noto.ttf`) covers Latin, Cyrillic and Greek:

```csharp
var cyrillic = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "NotoCyr", "fonts/noto.ttf", true, 1251).AsFont();
var greek = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "NotoGrk", "fonts/noto.ttf", true, 1253).AsFont();

page.SetFontAndSize(cyrillic, 14);
page.TextOut(50, 500, "Привет, мир!");
page.SetFontAndSize(greek, 14);
page.TextOut(50, 475, "Γειά σου κόσμε!");
```

Characters outside the font's code page are printed as `?`. Each font object embeds its own copy of the font file, so
the example above stores `noto.ttf` twice. To mix several scripts with one embedded copy, load the file as a CID font
with code page 65001 instead (see [Unicode text with code page 65001](#unicode-text-with-code-page-65001)).

### Loading from a stream

To load a font from an embedded resource or a database, use `LoadFromStream`. It takes the parsed stream and the same
bytes as an array (the array is what gets embedded):

```csharp
byte[] data = File.ReadAllBytes("fonts/Roboto-Regular.ttf");   // or from a resource
using var stream = new MemoryStream(data);
var fromStream = HpdfTrueTypeFont.LoadFromStream(pdf.Xref, "RobotoS", stream, data, true, 1252).AsFont();

page.SetFontAndSize(fromStream, 14);
page.TextOut(50, 445, "Loaded from a stream");
```

### Many documents with the same fonts

A font object belongs to one document, but the font file can be read once for all of them. `FontDataPool` keeps the
files it has loaded in memory, by path, for the whole process; `LoadFromSharedData` creates a font of a document from
the cached data:

```csharp
ParsedTrueTypeData roboto = FontDataPool.GetOrLoadTrueType("fonts/Roboto-Regular.ttf");

foreach (string name in new[] { "invoice-1.pdf", "invoice-2.pdf" })
{
    var doc = new HpdfDocument();
    var western = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F1", roboto, true).AsFont();
    var cyrillic = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F2", roboto, true, 1251).AsFont();
    // ...
    doc.SaveToFile(name);
}

FontDataPool.Clear();   // release the cached files when the batch is done
```

The cached file does not depend on a code page: pass the code page to `LoadFromSharedData`, or to
`GetOrLoadTrueType(path, codePage)`, which returns data for that code page over the same cached bytes (1252 when
none is given). `FontDataPool.GetOrLoadType1` and `HpdfType1Font.LoadFromSharedData` do the same for Type 1 fonts.
The pool is thread-safe; a file that changes on disk is not reloaded until `Clear` is called.

---

## Type 1 Fonts

A PostScript Type 1 font comes as two files: the `.afm` file with the metrics and the `.pfb` file with the glyphs.

```csharp
var urwGothic = HpdfType1Font.LoadFromFile(
    pdf.Xref,
    "URW",                    // local name
    "fonts/a010013l.afm",     // metrics (required)
    "fonts/a010013l.pfb",     // font program; null = do not embed
    1251)                     // code page (1252 by default)
    .AsFont();

page.SetFontAndSize(urwGothic, 14);
page.TextOut(50, 415, "URW Gothic: Привет!");
```

- With a `.pfb` file the font program is embedded. Without it (`null`, or the overload without `pfbPath`), only the
  metrics are used and the viewer must have the font installed.
- Code pages work as for TrueType fonts: one single-byte code page per font object.
- The font name stored in the PDF comes from the `FontName` entry of the `.afm` file (`URWGothicL-Book` here).

---

## CJK Fonts

Chinese, Japanese and Korean text needs composite (CID) fonts, which use two bytes per character. Haru.NET offers two
kinds: predefined fonts that are not embedded, and TrueType fonts that are embedded. Both require PDF 1.4; the
document version is raised automatically.

### Predefined CJK fonts

`HpdfCIDFontType0.Create` uses built-in metrics of common Windows Asian fonts. Nothing is embedded, so the PDF is
small, but the reader needs the font, or a viewer that substitutes one (Adobe Acrobat downloads Asian font packs;
other viewers use their own fonts).

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var simSun = HpdfCIDFontType0.Create(
    pdf,                      // the document
    "SimSun",                 // predefined font name
    "SimSun",                 // local name
    936,                      // code page (GBK)
    "GBK-EUC-H")              // CMap encoding name
    .AsFont();

page.SetFontAndSize(simSun, 20);
page.TextOut(50, 750, "你好世界");

pdf.SaveToFile("simsun.pdf");
```

Use the code page and encoding name that belong to the font. The encoding name selects the encoder, which encodes
the text and knows which glyph (CID) of the font each character code stands for; the widths of the font are looked up
by it. An encoder object can also be passed as the last argument of `Create`.

| Font names | Language | Code page | Encoding name | Encoder |
|------------|----------|-----------|---------------|---------|
| `SimSun`, `SimHei` | Simplified Chinese | 936 | `GBK-EUC-H` | `GBKEucHEncoder` |
| `MingLiU` | Traditional Chinese | 950 | `ETen-B5-H` | `ETenB5HEncoder` |
| `MS-Gothic`, `MS-Mincho`, `MS-PGothic`, `MS-PMincho` | Japanese | 932 | `90ms-RKSJ-H` | `Ms90RKSJHEncoder` |
| `Dotum`, `DotumChe`, `Batang`, `BatangChe` | Korean | 949 | `KSCms-UHC-H` | `KSCmsUHCHEncoder` |

An unknown font name throws `ArgumentException`; an encoding name other than the four above (and `EUC-H`, the
Japanese EUC encoding with `EucHEncoder`) throws `HpdfException` (`InvalidEncodingName`) unless an encoder is passed.
One encoder object can be shared by several fonts.

### Embedded CID fonts

`HpdfCIDFont.LoadFromTrueTypeFile` embeds a TrueType font as a CID font. The text looks the same everywhere. CJK
fonts are large (the Japanese `noto-jp.ttf` is about 5 MB), but only the outlines of the glyphs that the document
shows are stored (see [Embedding](#embedding)): a page of Japanese text adds some tens of kilobytes.

```csharp
var japanese = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JP", "fonts/noto-jp.ttf", 932).AsFont();

page.SetFontAndSize(japanese, 20);
page.TextOut(50, 720, "こんにちは世界");
```

The code page must be 932 (Japanese), 936 (Simplified Chinese), 949 (Korean), 950 (Traditional Chinese) or 65001
(Unicode); any other value throws `HpdfException` (`InvalidParameter`). The code page names the language of the font
(`/Lang` of the font descriptor); it does not limit the text: with every code page the font prints, and measures, any
character it has a glyph for. A character without a glyph is drawn as the font's "missing glyph" box. This includes
the characters above U+FFFF (CJK Extension B and later, for example), which are surrogate pairs in a .NET string: they
are printed, measured and copied as one character when the font has outlines for them.

### Unicode text with code page 65001

With code page 65001 a CID font prints any character the font file contains, in any script, from one embedded copy.
This is the simplest way to mix languages, and it also works for Latin, Cyrillic or Greek text that does not fit a
single code page:

```csharp
var noto = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "Noto", "fonts/noto.ttf", 65001).AsFont();

page.SetFontAndSize(noto, 16);
page.TextOut(50, 690, "English, Français, Русский, Ελληνικά, Polski: Łódź");
```

Prefer 65001 for new documents that need CJK or mixed-script text: it prints the same glyphs as the CJK code pages
and keeps the text copyable (see [Copyable Text](#copyable-text)). Limits:

- Only characters of the Basic Multilingual Plane are supported; characters outside it, such as emoji, print as empty
  glyphs.
- Characters missing from the font file print as the font's "missing glyph" (usually an empty box or nothing).
- Word spacing (`SetWordSpace`) has no effect on CID fonts. Character spacing works.

---

## Using Fonts by Name

To look up fonts by name, for example from a style table, register them with the document. `GetFont` then returns the
registered font for its `BaseFont` name and encoding:

```csharp
var robotoFont = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "RobotoR", "fonts/Roboto-Regular.ttf", true).AsFont();
pdf.RegisterFont(robotoFont);                    // registered as "Roboto-Regular" with code page 1252

var sameFont = pdf.GetFont("Roboto-Regular");    // returns robotoFont
page.SetFontAndSize(sameFont, 12);
page.TextOut(50, 665, "Found by name");
```

The registration key is the font's `BaseFont` (its PostScript name) plus its code page, so pass the matching encoding
to `GetFont`, for example `pdf.GetFont("NotoSans-Regular", "UTF-8")` for a CID font loaded with code page 65001, or
`pdf.GetFont("Roboto-Regular", "CP1251")` for a font loaded with code page 1251.

---

## Font Metrics

Font metrics are in **glyph units**, 1000 per em. Multiply by the font size and divide by 1000 to get points:

| Method | Description |
|--------|-------------|
| `GetAscent()` | Height of the tallest letters above the baseline (positive) |
| `GetDescent()` | Depth of descenders below the baseline (negative) |
| `GetXHeight()` | Height of lowercase letters such as `x` |
| `GetBBox()` | Bounding box of all glyphs (`HpdfBox` with `Left`, `Bottom`, `Right`, `Top`) |
| `MeasureText(text, size)` | Width of a string in points at the given size |

The following example draws a frame that fits a line of text exactly:

```csharp
var font = pdf.GetFont("Helvetica");
const float size = 24;
const string text = "Metrics";
float x = 50, baseline = 600;

float width = font.MeasureText(text, size);
float ascent = font.GetAscent() * size / 1000;    // above the baseline
float descent = font.GetDescent() * size / 1000;  // below the baseline (negative)

page.SetFontAndSize(font, size);
page.TextOut(x, baseline, text);

page.SetLineWidth(0.5f);
page.Rectangle(x, baseline + descent, width, ascent - descent);
page.Stroke();
```

A comfortable line spacing is about 1.2 times the font size; the distance `GetBBox().Top - GetBBox().Bottom` (scaled
the same way) is the spacing that guarantees no overlap. Measuring and wrapping text, including the overload of
`MeasureText` with word wrap, is described in the [Text Guide](TextGuide.md#measuring-text).

---

## Copyable Text

Whether text can be searched and copied from the PDF depends on how the viewer maps character codes back to Unicode:

- **Standard fonts** use standard encodings that viewers know.
- **TrueType and Type 1 fonts** get a ToUnicode map for their code page automatically.
- **Embedded CID fonts** get a ToUnicode map automatically, with every code page. It is written when the document is
  saved and covers the characters that were shown with the font. When the font draws two characters with one glyph
  (the kanji U+65E5 and the Kangxi radical U+2F47, for example), the glyph is copied as the character it was first
  shown for.
- **Predefined CJK fonts** need no ToUnicode map: their encodings are standard CMaps that viewers map to Unicode
  themselves.

No extra call is needed. Text drawn with the invisible rendering mode is still copyable.

---

## Font Licensing

Embedding a font copies it into every PDF you distribute. Fonts are licensed software, and many commercial licenses
restrict or forbid embedding. TrueType fonts carry "embedding permissions" (`fsType`) in their `OS/2` table, and
Haru.NET honors them when a font is embedded:

| Permission of the font | Result |
|------------------------|--------|
| Restricted license embedding | `HpdfException` (`TtfCannotEmbeddingFont`) when the font is loaded |
| Bitmap embedding only | `HpdfException` (`TtfCannotEmbeddingFont`): a PDF embeds outlines |
| No subsetting | The whole font file is embedded, `EmbedSubset` is ignored |
| Installable, editable or preview-and-print embedding | Embedded, as a subset by default |

A TrueType font that must not be embedded can still be used with `embedding: false`; a CID font is always embedded.
The flag does not replace the license: check the license of each font you embed. Fonts under the SIL Open Font
License, such as Noto, and under the Apache License, such as Roboto, allow embedding. The standard fonts and the
predefined CJK fonts are never embedded.

---

## Choosing a Font

| You need | Use |
|----------|-----|
| The smallest file, Western European text, any viewer | Standard font: `pdf.GetFont("Helvetica")` |
| Symbols, check marks, Greek letters for formulas | `Symbol` or `ZapfDingbats` |
| A specific typeface, one Latin, Cyrillic or Greek code page | TrueType, embedded: `HpdfTrueTypeFont.LoadFromFile(..., true, codePage)` |
| A typeface only for internal use where the font is installed | TrueType, not embedded: `LoadFromFile(..., false)` |
| An existing PostScript Type 1 font | `HpdfType1Font.LoadFromFile(xref, name, afm, pfb, codePage)` |
| Chinese, Japanese or Korean text, small file | Predefined CJK font: `HpdfCIDFontType0.Create(...)` |
| CJK text that looks the same everywhere | Embedded CID font: `HpdfCIDFont.LoadFromTrueTypeFile(...)` |
| Several scripts in one document or one line, or Unicode text in general | Embedded CID font with code page 65001 |

---

## Pitfalls

- **`GetFont` with a file path or a non-standard name** throws `HpdfException` ("Unknown font name"). Load files with
  `HpdfTrueTypeFont`, `HpdfType1Font` or `HpdfCIDFont`.
- **`?` instead of characters**: the character is not in the font object's code page or encoding. Load the font with
  a code page that contains it, or use a CID font with code page 65001.
- **Characters missing in the viewer** with a standard font and a non-Latin encoding: the standard fonts have no
  Cyrillic, Greek, Hebrew, Arabic or Thai glyphs. Embed a TrueType font.
- **`DuplicateRegistration` exception** in `SetFontAndSize`: two different fonts with the same local name are used on
  one page. Give each font its own local name.
- **Large files**: every load of an embedded font stores the font again (as a subset, unless `EmbedSubset` is off).
  Load each font once per document and reuse the `HpdfFont`; for several scripts use one CID font with code page
  65001 instead of one TrueType font per code page.
- **`TtfCannotEmbeddingFont` exception** when a font is loaded: the font's embedding permissions forbid embedding
  (see [Font Licensing](#font-licensing)).
- **Fonts from another document**: a font object can only be used in the document it was created for.

---

## See Also

- [Text Guide](TextGuide.md) - Showing, positioning, measuring and wrapping text
- [HpdfFont](../api/core/HpdfFont.md) - Font API reference
- [HpdfDocument](../api/core/HpdfDocument.md) - `RegisterFont()`, saving
- [HpdfDocumentExtensions](../api/extensions/HpdfDocumentExtensions.md) - `GetFont()`
- [HpdfPageText](../api/extensions/HpdfPageText.md) - `SetFontAndSize()` and the other text operators
- [Getting Started](GettingStarted.md) - First steps with the library
- Demos in `cs-src/Haru.Demos`: `FontDemo.cs`, `InternationalDemo.cs`, `Type1FontDemo.cs`, `CJKDemo.cs`,
  `CJKFontsDemo.cs`, `CIDFontType0Demo.cs`, `FontMetricsDemo.cs`

---

[← Back to Documentation Index](../INDEX.md)
