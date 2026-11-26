---
name: haru-net-pdf
description: |
  Skill for working with Haru.NET PDF generation library. Triggers when: importing Haru.Doc,
  Haru.Font, Haru.Font.CID, Haru.Forms or Haru.Types namespaces; referencing HpdfDocument, HpdfPage,
  HpdfFont, HpdfTrueTypeFont, HpdfCIDFont or HpdfCIDFontType0 classes; working with PDF generation
  in projects using the Haru package; questions about PDF creation, fonts, graphics, or text
  rendering with Haru.NET.
packages: Haru
---

# Haru.NET PDF Generation Library

Pure C# port of libharu (the `Haru` NuGet package, .NET 8+), no native dependencies. Errors are
`HpdfException` with an `ErrorCode` (`HpdfErrorCode`).

## Quick Start

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();                       // A4 portrait; AddPage(HpdfPageSize.Letter, HpdfPageDirection.Landscape)
var font = pdf.GetFont("Helvetica");            // a standard font; see Fonts for files

page.SetFontAndSize(font, 24);
page.TextOut(100, 700, "Hello PDF!");           // BeginText + position + ShowText + EndText

pdf.SaveToFile("output.pdf");                   // or byte[] bytes = pdf.SaveToMemory(); or pdf.Save(stream)
```

## Critical Gotchas

1. **Bottom-left origin.** (0, 0) is the lower-left corner, Y grows upward, units are points
   (72 per inch; A4 is 595 x 842, Letter 612 x 792). From the top: `page.Height - offset`.
2. **Set the font first.** `ShowText` / `TextOut` without `SetFontAndSize` throws `PageFontNotFound`.
3. **Text needs a text object.** `MoveTextPos`, `ShowText` and `TextRect` only between
   `BeginText()` and `EndText()`. `TextOut(x, y, text)` opens and closes its own text object:
   call it outside `BeginText` / `EndText`.
4. **Colors are 0.0 - 1.0**, not 0 - 255: `page.SetRgbFill(1f, 0f, 0f)`.
5. **A path draws nothing until painted**: end it with `Stroke()`, `Fill()` or `FillStroke()`.
6. **`GetFont` does not read font files.** It returns one of the 14 standard fonts or a font
   registered with `RegisterFont`. Load files with the font classes below.
7. **Every `GSave()` needs a `GRestore()`** on the same page, or saving throws.
8. **A font belongs to one document.** Create it once per document and reuse it on all pages.

## Fonts

### Standard fonts (never embedded, Latin only)

```csharp
var helvetica = pdf.GetFont("Helvetica");                      // WinAnsi (code page 1252)
var bold = pdf.GetFont("Helvetica-Bold");
var centralEuropean = pdf.GetFont("Times-Roman", "CP1250");    // another single-byte encoding
var explicitName = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F1");   // own resource name
```

Names: `Courier`, `Helvetica`, `Times-Roman` (each with `-Bold`, `-Oblique` / `-Italic`,
`-BoldOblique` / `-BoldItalic`), `Symbol`, `ZapfDingbats`. Encoding names, in any letter case:
`WinAnsiEncoding`, `MacRomanEncoding`, `CP1250`..`CP1258`, `ISO8859-2`..`ISO8859-16`, `KOI8-R`.
An unknown encoding name throws `InvalidEncodingName`. For Cyrillic, Greek etc. embed a TrueType
font: the standard fonts have Latin glyphs only.

### TrueType fonts (one single-byte code page per font object)

```csharp
// LoadFromFile(xref, resourceName, path, embedding, codePage = 1252)
var roboto = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F2", "fonts/Roboto-Regular.ttf", true).AsFont();
var cyrillic = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F3", "fonts/Roboto-Regular.ttf", true, 1251).AsFont();

page.SetFontAndSize(cyrillic, 14);
page.TextOut(50, 650, "Привет, мир");
```

### Unicode and CJK: embedded CID fonts

```csharp
using Haru.Font.CID;

// Any script from one font object: code page 65001. The font file must contain the glyphs.
var unicode = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "U1", "fonts/noto.ttf", 65001).AsFont();
var japanese = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JP", "fonts/noto-jp.ttf", 932).AsFont();

page.SetFontAndSize(japanese, 18);
page.TextOut(50, 600, "こんにちは世界");
```

Code pages 932 (Japanese), 936 (Simplified Chinese), 949 (Korean), 950 (Traditional Chinese) or
65001; the code page only names the language - every code page prints any character the font has,
including characters above U+FFFF. Text is copyable and searchable. Prefer one CID font with 65001
to several TrueType fonts with different code pages.

### Embedding: subsets and font permissions

Embedded TrueType and CID fonts are stored as **subsets** (only the glyph outlines the document
can use), written when the document is saved - a page of CJK text adds tens of kilobytes, not the
whole 5-10 MB font. To embed the whole file, set the switch on the font class before saving:

```csharp
var whole = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F4", "fonts/Roboto-Regular.ttf", true);
whole.EmbedSubset = false;                     // HpdfCIDFont has the same property
var wholeFont = whole.AsFont();
```

A font whose embedding permissions (fsType) forbid embedding throws `HpdfException` with
`TtfCannotEmbeddingFont` when it is loaded for embedding; catch it and choose another font. A
TrueType font can still be used with `embedding: false` (the viewer then needs the font).

### Predefined CJK fonts (not embedded, the viewer supplies the font)

```csharp
using Haru.Font.CID;

// Create(document, fontName, resourceName, codePage, encodingName)
var simSun = HpdfCIDFontType0.Create(pdf, "SimSun", "CN", 936, "GBK-EUC-H").AsFont();
```

| Fonts | Code page | Encoding name |
|-------|-----------|---------------|
| `SimSun`, `SimHei` | 936 | `GBK-EUC-H` |
| `MingLiU` | 950 | `ETen-B5-H` |
| `MS-Gothic`, `MS-Mincho`, `MS-PGothic`, `MS-PMincho` | 932 | `90ms-RKSJ-H` |
| `Dotum`, `DotumChe`, `Batang`, `BatangChe` | 949 | `KSCms-UHC-H` |

Another encoding name throws `InvalidEncodingName`.

### Fonts by name

```csharp
var loaded = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F5", "fonts/Roboto-Regular.ttf", true, 1251).AsFont();
pdf.RegisterFont(loaded);                           // under its BaseFont name and code page
var same = pdf.GetFont(loaded.BaseFont);            // "Roboto-Regular"; or GetFont(name, "CP1251")
```

### Many documents with the same font file

```csharp
ParsedTrueTypeData data = FontDataPool.GetOrLoadTrueType("fonts/Roboto-Regular.ttf");   // read once, cached
var shared = HpdfTrueTypeFont.LoadFromSharedData(pdf.Xref, "F6", data, true, 1251).AsFont();
```

### Measuring and wrapping

```csharp
float width = font.MeasureText("Sample Text", 12);          // points
page.SetFontAndSize(font, 12);
page.TextOut((page.Width - width) / 2, 500, "Sample Text"); // centered

// Wrapped, aligned text in a box (left, top, right, bottom); returns the number of characters printed
page.BeginText();
page.TextRect(50, 450, 300, 350, "A long paragraph that is wrapped inside the box.", HpdfTextAlignment.Justify);
page.EndText();
```

`font.GetAscent()`, `GetDescent()`, `GetXHeight()` are in 1000 units per em (multiply by size / 1000).
`SetWordSpace` has no effect on CID fonts; `SetCharSpace` works for all.

## Graphics

```csharp
page.SetLineWidth(2);
page.SetRgbStroke(0f, 0f, 1f);
page.SetRgbFill(0.8f, 0.2f, 0.2f);

page.Rectangle(100, 600, 200, 100);      // x, y (lower-left), width, height
page.Stroke();
page.Rectangle(100, 450, 200, 100);
page.FillStroke();
page.Circle(200, 350, 50);               // center, radius
page.Fill();
page.Ellipse(400, 350, 80, 40);          // center, x radius, y radius
page.Stroke();

page.MoveTo(50, 250);
page.LineTo(300, 280);
page.LineTo(500, 250);
page.Stroke();

page.SetDash(new ushort[] { 5, 3 }, 0);  // 5 on, 3 off
page.MoveTo(50, 220);
page.LineTo(500, 220);
page.Stroke();
page.SetDash(Array.Empty<ushort>(), 0);  // solid again

page.SetCmykFill(1f, 0f, 0f, 0f);        // CMYK; SetGrayFill(0.5f) / SetGrayStroke(0f) for gray

page.GSave();                            // temporary state: colors, line width, transformation
page.SetRgbStroke(1f, 0f, 0f);
page.Rectangle(50, 50, page.Width - 100, page.Height - 100);
page.Stroke();
page.GRestore();
```

Transparency: `var gs = new HpdfExtGState(pdf.Xref, "GS1"); gs.SetAlphaFill(0.5f); page.SetExtGState(gs);`

## Images

```csharp
var png = pdf.LoadPngImageFromFile("image.png");     // alpha channel becomes transparency
var jpeg = pdf.LoadJpegImageFromFile("photo.jpg");   // LoadPngImage(stream) / LoadJpegImage(stream)

float imageWidth = 300;
float imageHeight = imageWidth * jpeg.Height / jpeg.Width;      // keep the aspect ratio
page.DrawImage(jpeg, 100, 400, imageWidth, imageHeight);        // x, y of the lower-left corner
```

## Pages, Metadata, Compression

```csharp
var landscape = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Landscape);
var custom = pdf.AddPage(600, 800);                  // points
custom.SetSize(HpdfPageSize.Letter, HpdfPageDirection.Portrait);

pdf.Info.Title = "Document Title";
pdf.Info.Author = "Author Name";
pdf.Info.Subject = "Subject";
pdf.Info.Keywords = "keyword1, keyword2";

pdf.SetCompressionMode(HpdfCompressionMode.All);     // compress page content
```

## Encryption

```csharp
pdf.SetEncryption(
    userPassword: "reader",
    ownerPassword: "S3cret-Owner",                   // required, different from the user password
    permission: HpdfPermission.Print | HpdfPermission.Copy,
    mode: HpdfEncryptMode.R4);                       // R2 = RC4-40, R3 = RC4-128, R4 = AES-128
```

## Links, Notes, Bookmarks

```csharp
// Rectangles are (left, bottom, right, top)
page.CreateLinkAnnotation(new HpdfRect(100, 500, 300, 520), "https://example.com");
page.CreateTextAnnotation(new HpdfRect(100, 440, 120, 460), "A note");

var bookmark = pdf.CreateOutline("Chapter 1");       // bookmark.CreateChild("Section 1.1") for a subtree
var destination = page.CreateDestination();
destination.SetFit();                                // or SetXYZ(left, top, zoom)
bookmark.SetDestination(destination);
```

## Forms (AcroForm)

```csharp
using Haru.Forms;

var form = pdf.GetOrCreateAcroForm();
var name = new HpdfTextField(pdf.Xref, "Name");
name.SetValue("Jane Doe");
page.AddWidgetAnnotation(name.CreateWidget(new HpdfRect(150, 700, 400, 720)));
form.AddField(name);
```

Also `HpdfCheckbox`, `HpdfRadioButton`, `HpdfChoice`, `HpdfSignature` in `Haru.Forms`.

## Troubleshooting

- **Text does not appear:** font not set, or coordinates outside the page (remember the origin).
- **`?` or empty boxes instead of letters:** the font's code page or the font file lacks the
  character - use a CID font with code page 65001 and a font file that has the script.
- **`InvalidEncodingName`:** misspelled encoding name in `GetFont` / `RegisterFont` /
  `HpdfCIDFontType0.Create`.
- **`TtfCannotEmbeddingFont`:** the font file forbids embedding; use another font.
- **"Unknown font name" from `GetFont`:** not a standard font name and not registered - load the
  file with a font class.
- **Shapes not visible:** the path was not painted (`Stroke` / `Fill`).

## Resources

- Documentation (guides and API reference): https://nikolaygekht.github.io/haru-net-full-port/
- Source and demos: https://github.com/nikolaygekht/haru-net-full-port (`cs-src/Haru.Demos`)

MIT license (C# port); the original libharu is under the ZLIB/LIBPNG license.
