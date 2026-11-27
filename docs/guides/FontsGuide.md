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
| TrueType | `HpdfTrueTypeFont` | `.ttf` or `.otf` file, or a font of a `.ttc` collection | Optional | One single-byte code page per font object |
| Type 1 | `HpdfType1Font` | `.afm` metrics + optional `.pfb` program | When a `.pfb` is given | One single-byte code page per font object |
| Predefined CJK | `HpdfCIDFontType0` | Built-in metrics of 11 common Asian fonts | Never | Chinese, Japanese or Korean |
| Embedded CID | `HpdfCIDFont` | `.ttf` or `.otf` file, or a font of a `.ttc` collection | Always | CJK code pages, or any Unicode text with code page 65001 |

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
`cs-src/Haru.Demos/Resource/demo/ttfont` and `cs-src/Haru.Demos/Resource/demo/type1` of the repository, the OpenType
fonts with CFF outlines (`Puritan2.otf`, `NotoSansJP-Regular.otf`) in `cs-src/Haru.Demos/Resource/demo/otfont`, the
font collection `NotoSansAndSpaceMono.ttc` in `cs-src/Haru.Test/Resources/Fonts`. The examples
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
| `MacRomanEncoding` | 10000 | Western European (Mac OS Roman) |
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
- Encoding names are not case sensitive; `ISO-8859-2` and `1250` are accepted for `ISO8859-2` and `CP1250`.
  A name that is not recognized throws `HpdfException` (`InvalidEncodingName`).
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

Load a `.ttf` file (or an `.otf` file, see [OpenType fonts with CFF outlines](#opentype-fonts-with-cff-outlines-otf))
with `HpdfTrueTypeFont.LoadFromFile`:

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
| `filePath` | Path of the `.ttf` or `.otf` file; a missing file throws `HpdfException` (`FileNotFound`) |
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
whole in any case (see [Font Licensing](#font-licensing)). A font file with neither TrueType nor CFF outlines cannot
be embedded: loading it for embedding throws `HpdfException` (`TtfMissingTable`).

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

### Font collections (.ttc)

A TrueType collection is one file with several fonts, usually the styles or the language variants of a family that
share glyphs (`msgothic.ttc`, `simsun.ttc`, `cambria.ttc` ...). Choose the font by its index, from 0,
with `LoadFromCollection`; the other parameters are those of `LoadFromFile`:

```csharp
var mono = HpdfTrueTypeFont.LoadFromCollection(
    pdf.Xref,
    "Mono",                            // local name
    "fonts/NotoSansAndSpaceMono.ttc",  // font collection
    1,                                 // the second font of the collection
    true,                              // embed the font in the PDF
    1252)                              // optional code page, 1252 by default
    .AsFont();

page.SetFontAndSize(mono, 14);
page.TextOut(50, 420, "Space Mono from a collection");
```

The embedded font is the one font, taken out of the collection: a subset of it, or the whole font with
`EmbedSubset = false`, never the whole collection. A collection given to `LoadFromFile`, or a font file given to
`LoadFromCollection`, throws `HpdfException` (`InvalidTtcFile`); an index the collection does not have throws
`InvalidTtcIndex`. CID fonts are loaded from a collection with `HpdfCIDFont.LoadFromTrueTypeCollection` (see
[Embedded CID fonts](#embedded-cid-fonts)). The index of a font is its place in the file; `fc-scan` or fontTools list
the fonts of a collection with their names.

Both methods also take a `Stream` instead of the path, for a collection in an embedded resource or a database. The
stream is read from its current position to its end and is left open:

```csharp
// The resource name is the default namespace of the project plus the folder and the file name
using (Stream resource = typeof(Program).Assembly.GetManifestResourceStream("MyApp.Fonts.NotoSansAndSpaceMono.ttc")!)
{
    var monoFromResource = HpdfTrueTypeFont.LoadFromCollection(pdf.Xref, "MonoR", resource, 1, true).AsFont();
    page.SetFontAndSize(monoFromResource, 14);
    page.TextOut(50, 395, "Space Mono from a resource");
}
```

### OpenType fonts with CFF outlines (.otf)

An OpenType font has TrueType outlines (most `.ttf` files and some `.otf` files) or CFF (PostScript) outlines (most
`.otf` files, among them the CJK fonts Noto Sans CJK and Source Han Sans, and the `.ttc` / `.otc` collections made
of them). Both load with the same methods; the outlines decide what the PDF gets:

| Loaded with | TrueType outlines | CFF outlines |
|-------------|-------------------|--------------|
| `HpdfTrueTypeFont` | TrueType font, font program `/FontFile2` | Type 1 font, CFF program `/FontFile3` (`Type1C`) |
| `HpdfCIDFont` | `CIDFontType2` font, `/FontFile2` | `CIDFontType0` font, CFF program `/FontFile3` (`CIDFontType0C`) |

```csharp
// A Latin font with CFF outlines, as a simple font
var puritan = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "Puritan", "fonts/Puritan2.otf", true, 1252).AsFont();

// A Japanese font with CFF outlines, as a CID font
var notoJp = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "NotoJP", "fonts/NotoSansJP-Regular.otf", 932).AsFont();

page.SetFontAndSize(puritan, 14);
page.TextOut(50, 370, "Puritan: Grüße aus Köln!");
page.SetFontAndSize(notoJp, 14);
page.TextOut(50, 345, "日本語のテキスト");
```

The CFF program is written anew for the document, never the OpenType file: a subset holds the glyphs in use with the
subroutines they call put in their place: about 250 bytes per glyph, so a page of CJK text from a 16 MB font adds
some tens of kilobytes. With
`EmbedSubset = false` the whole CFF program is stored. Text, measuring, vertical writing (with the vertical origins of
the font's `VORG` table) and copying work as with TrueType outlines.

Two limits: a CJK font with CFF outlines is *CID-keyed* - its glyphs have no names - and can only be a CID font;
`HpdfTrueTypeFont` throws `HpdfException` (`UnsupportedFontType`) for it. Use `HpdfCIDFont` with the code page of the
language, or 65001 for any text. Fonts with CFF2 outlines (variable OpenType fonts) are not supported
(`UnsupportedFontType`).

### Many documents with the same fonts

A font object belongs to one document, but the font file can be read and parsed once for all of them. `FontDataPool` keeps the
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
none is given). A font of a collection is cached with `GetOrLoadTrueTypeCollection(path, fontIndex, codePage)`; the
fonts of one collection share the bytes of the file, which is read once. `FontDataPool.GetOrLoadType1` and
`HpdfType1Font.LoadFromSharedData` do the same for Type 1 fonts.
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
    "GBK-EUC-H")              // encoding name
    .AsFont();

page.SetFontAndSize(simSun, 20);
page.TextOut(50, 750, "你好世界");

pdf.SaveToFile("simsun.pdf");
```

Use an encoding name that belongs to the font. The encoding name selects the encoder, which encodes the text with
the code page of the encoding and knows which glyph (CID) of the font each character code stands for; the widths of
the font are looked up by it. An encoder object can also be passed as the last argument of `Create`. The older
overload with a code page before the encoding name still works; the code page is not used.

| Font names | Language | Code page | Encoding name |
|------------|----------|-----------|---------------|
| `SimSun`, `SimHei` | Simplified Chinese | 936 | `GBK-EUC-H` |
| `MingLiU` | Traditional Chinese | 950 | `ETen-B5-H` |
| `MS-Gothic`, `MS-Mincho` | Japanese, fixed pitch | 932 | `90ms-RKSJ-H` |
| `MS-PGothic`, `MS-PMincho` | Japanese, proportional | 932 | `90msp-RKSJ-H` |
| `Dotum`, `Batang` | Korean, proportional | 949 | `KSCms-UHC-H` |
| `DotumChe`, `BatangChe` | Korean, fixed pitch | 949 | `KSCms-UHC-HW-H` |

A font can be used with any encoding of its language. The encodings (the predefined CMaps of the PDF format) are:

| Encoding name | Language | Code page | Text it can hold |
|---------------|----------|-----------|------------------|
| `GBK-EUC-H` | Simplified Chinese | 936 | GBK |
| `GB-EUC-H` | Simplified Chinese | 20936 | GB 2312 only |
| `ETen-B5-H` | Traditional Chinese | 950 | Big5 |
| `90ms-RKSJ-H` | Japanese | 932 | Shift-JIS; Latin characters are half-width |
| `90msp-RKSJ-H` | Japanese | 932 | Shift-JIS; Latin characters are proportional |
| `EUC-H` | Japanese | 20932 | EUC-JP |
| `KSCms-UHC-H` | Korean | 949 | UHC; Latin characters are proportional |
| `KSCms-UHC-HW-H` | Korean | 949 | UHC; Latin characters are half-width |
| `KSC-EUC-H` | Korean | 51949 | KS X 1001 only |

A character the encoding cannot hold is written as `?`. The name is found in any letter case. An unknown font name
throws `ArgumentException`; an encoding name that is not in the table throws `HpdfException`
(`InvalidEncodingName`) unless an encoder is passed. `CIDEncoder.Create(name)` returns the encoder of an encoding;
one encoder object can be shared by several fonts.

### Embedded CID fonts

`HpdfCIDFont.LoadFromTrueTypeFile` embeds an OpenType font (`.ttf`, or `.otf` with TrueType or CFF outlines, see
[OpenType fonts with CFF outlines](#opentype-fonts-with-cff-outlines-otf)) as a CID font. The text looks the same
everywhere. CJK fonts are large (the Japanese `noto-jp.ttf` is about 5 MB), but only the outlines of the glyphs that
the document shows are stored (see [Embedding](#embedding)): a page of Japanese text adds some tens of kilobytes.

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

CJK fonts often come as collections (`.ttc`) of their language variants or weights. Load a font of a collection
with `LoadFromTrueTypeCollection`, the index of the font and, optionally, the writing mode as the last argument; a
`Stream` can be given instead of the path:

```csharp
// Noto Sans, the first font of the collection, for Unicode text
var noto = HpdfCIDFont.LoadFromTrueTypeCollection(pdf, "NotoU", "fonts/NotoSansAndSpaceMono.ttc", 0, 65001).AsFont();

page.SetFontAndSize(noto, 14);
page.TextOut(50, 690, "Привет, Γειά σου, Hello");
```

Only the one font is embedded, as a subset (see [Font collections](#font-collections-ttc)).

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

- Characters missing from the font file print as the font's "missing glyph" (usually an empty box or nothing).
  Color emoji fonts have no outlines the PDF could use, so emoji usually print this way.
- Word spacing (`SetWordSpace`) has no effect on CID fonts. Character spacing works.

### Line breaks in CJK text

`TextRect` and `MeasureText` with word wrapping break Chinese and Japanese text between characters, as these
languages are written without spaces. They follow the rules of Japanese and Chinese typesetting: a line does not
begin with a closing bracket, a full stop or comma, a small kana or the prolonged sound mark, and does not end with
an opening bracket - the character before moves to the next line with it, so a line can be one character shorter than
the others. Korean separates words with spaces: Korean words, like Latin ones, are kept whole. The rules are the same
for every CID font and for both writing directions.

### Vertical writing

Chinese, Japanese and Korean text can be written from top to bottom. The direction belongs to the font: load an
embedded CID font for vertical writing, or create a predefined font with a vertical encoding. Everything else is
the same as for horizontal text.

```csharp
using Haru.Types;

var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
    pdf, "JV", "fonts/noto-jp.ttf", 932, HpdfWritingMode.Vertical).AsFont();

page.BeginText();
page.SetFontAndSize(vertical, 16);

// One column: the first character hangs under (500, 760)
page.TextOut(500, 760, "日本語、「テスト」。");

// Columns in a rectangle, from its right edge to the left, 24 points apart
page.SetTextLeading(24);
page.TextRect(300, 760, 480, 560, "吾輩は猫である。名前はまだ無い。", HpdfTextAlignment.Left);
page.EndText();
```

How vertical text behaves:

- The text position is the top of the first character, in the middle of its width. Every character moves the
  position down by its advance height. A predefined font moves every character down by the font size, narrow
  characters such as Latin letters too. An embedded font takes the advance heights from the font file (its `vmtx`
  table): in a CJK font nearly every character is one em (the font size) tall, a few are taller (the kana repeat
  mark `〱` takes two), and some fonts give characters heights of their own (Malgun Gothic). An embedded font without
  vertical metrics moves every character down by the font size. The font file also tells where each glyph hangs from
  the text position; the library writes it to the font, so a viewer places the glyphs as the font designer did.
- `MeasureText` and `TextWidth` return the height of the text: the sum of the advance heights of its characters,
  which is the font size times the number of characters in most CJK text.
- Brackets, dashes and punctuation are shown in their forms for vertical writing. An embedded font takes them from
  the font file (the `vert` feature of its GSUB table, which CJK fonts have); a font without them shows its
  horizontal forms. Latin letters and digits stay upright: the library uses `vert` as Adobe applications and web
  browsers do, not the `vrt2` feature of Microsoft Office with the sideways forms some fonts have, and it does not
  turn Latin text on its side. Proportional vertical metrics (the `vpal` feature) are not applied.
- `TextRect` lays the text out in columns: see [HpdfPageText](../api/extensions/HpdfPageText.md#textrect).
- A negative character spacing (`SetCharSpace`) moves the characters of a column apart, a positive one brings them
  closer: the spacing is added upwards, while the text runs down.
- `MoveToNextLine` and `ShowTextNextLine` move down by the leading, as in horizontal writing. To start the next
  column yourself, use `TextOut`, or `MoveTextPos(-distance, 0)`.
- `font.WritingMode` tells the direction of a font. A font keeps its direction: to write in both, load the font
  file twice.
- Copied text is the text that was written, in reading order.

A predefined font is vertical when its encoding is: each of these holds the same text as its horizontal form.

| Vertical encoding | Horizontal form | Language |
|-------------------|-----------------|----------|
| `GBK-EUC-V` | `GBK-EUC-H` | Simplified Chinese |
| `GB-EUC-V` | `GB-EUC-H` | Simplified Chinese |
| `ETen-B5-V` | `ETen-B5-H` | Traditional Chinese |
| `90ms-RKSJ-V` | `90ms-RKSJ-H` | Japanese |
| `EUC-V` | `EUC-H` | Japanese |
| `KSCms-UHC-HW-V` | `KSCms-UHC-HW-H` | Korean |
| `KSC-EUC-V` | `KSC-EUC-H` | Korean |

```csharp
var mincho = HpdfCIDFontType0.Create(pdf, "MS-Mincho", "MV", "90ms-RKSJ-V").AsFont();
```

`90msp-RKSJ-H` and `KSCms-UHC-H` have no vertical form.

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

A font is registered under its `BaseFont` (its PostScript name) and its encoding: a predefined CJK font under its
CMap (`90ms-RKSJ-V`), an embedded CID font under `Identity-H` or `Identity-V` (its code page only sets the language),
any other font under its code page. `GetFont` with the name alone returns the registered font whatever its encoding
is. When the same font is registered several times with different encodings, pass the encoding to choose one:
`pdf.GetFont("Roboto-Regular", "CP1251")` for the font loaded with code page 1251, `pdf.GetFont("NotoSans-Regular",
"UTF-8")` for an embedded CID font, `pdf.GetFont("NotoSans-Regular", "Identity-V")` for the same font loaded for
vertical writing, `pdf.GetFont("MS-Mincho", "90ms-RKSJ-V")` for a predefined font with a vertical CMap. Encodings
that share a code page (`90ms-RKSJ-H`, `90msp-RKSJ-H` and `90ms-RKSJ-V`) are registered side by side. A code page name
(`"CP932"`) also finds a registered font of that code page that writes horizontally, whatever its CMap is. Asking for
a registered font with an encoding it is not registered with throws `HpdfException` (`InvalidParameter`), as an
unknown font name does.

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
| No subsetting | The whole font is embedded (the font file, or the whole CFF program), `EmbedSubset` is ignored |
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
- **`InvalidTtcFile` exception** when a `.ttc` file is loaded: a font collection holds several fonts; load one of
  them with `LoadFromCollection` / `LoadFromTrueTypeCollection` and its index (see
  [Font collections](#font-collections-ttc)).
- **`UnsupportedFontType` exception** when a CJK `.otf` file is loaded with `HpdfTrueTypeFont`: its CFF outlines
  are CID-keyed and can only make a CID font; load it with `HpdfCIDFont` (see
  [OpenType fonts with CFF outlines](#opentype-fonts-with-cff-outlines-otf)).
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
  `CJKFontsDemo.cs`, `CIDFontType0Demo.cs`, `FontMetricsDemo.cs`, `OpenTypeFontDemo.cs`

---

[← Back to Documentation Index](../INDEX.md)
