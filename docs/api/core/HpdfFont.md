# HpdfFont Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Font`

---

## Overview

`HpdfFont` is a unified wrapper for all font types in Haru.NET. It provides a consistent interface for working with Standard (Base14), TrueType, Type1, and CID (CJK) fonts. All text rendering in PDF requires a font object.

### Purpose

- **Unified font interface** - Single class for all font types
- **Text measurement** - Calculate text width for layout
- **Font metrics** - Access ascent, descent, x-height, bounding box
- **Character widths** - Get individual character dimensions
- **CJK support** - Handle complex scripts with CID fonts

### Font Types

Haru.NET supports four font types, all wrapped by `HpdfFont`:

1. **Standard Fonts** (Base14) - Built into all PDF readers, no embedding required
2. **TrueType Fonts** (.ttf) - Custom fonts with code page support
3. **Type1 Fonts** (.afm/.pfb) - PostScript fonts
4. **CID Fonts** - Composite fonts for CJK (Chinese, Japanese, Korean) languages

### Basic Usage

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();

// Standard font (no embedding)
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var page = pdf.AddPage();
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Text in Helvetica");
page.EndText();

pdf.SaveToFile("output.pdf");
```

---

## Constructors

### HpdfFont(HpdfXref, HpdfStandardFont, string, int)

Creates a standard PDF font (Base14).

```csharp
public HpdfFont(HpdfXref xref, HpdfStandardFont standardFont, string localName, int codePage = 1252)
```

**Parameters:**
- `xref` - Cross-reference table from HpdfDocument
- `standardFont` - One of 14 standard fonts
- `localName` - Resource name (e.g., "F1", "F2"); should not start with `HpdfDocument.GeneratedNamePrefix` (`"Haru"`), the prefix of the names `GetFont` generates
- `codePage` - Code page used to encode text (default: 1252, WinAnsiEncoding). A standard font has single-byte encodings only: 1250 to 1258, 10000, 20866 and the ISO 8859 code pages (28591 to 28606); any other code page throws `HpdfException` (`InvalidEncoderType`)

**Example:**

```csharp
var pdf = new HpdfDocument();

// Create different standard fonts
var helvetica = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var times = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesRoman, "F2");
var courier = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F3");

// Bold variants
var helveticaBold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F4");
var timesBold = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesBold, "F5");

// Italic variants
var helveticaOblique = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaOblique, "F6");
var timesItalic = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesItalic, "F7");
```

### Other Constructors

The other constructors wrap fonts loaded from files. The CID constructors are internal:

```csharp
public HpdfFont(HpdfTrueTypeFont ttFont)         // TrueType fonts
public HpdfFont(HpdfType1Font type1Font)         // Type1 fonts
internal HpdfFont(HpdfCIDFont cidFont)           // CID fonts (CIDFontType2)
internal HpdfFont(HpdfCIDFontType0 cidFontType0) // CID fonts (CIDFontType0)
```

**Usage:** Load custom fonts with their font classes and call `AsFont()`, which creates the `HpdfFont`:

```csharp
// TrueType font (embedded, code page 1252)
var customFont = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F2", "path/to/font.ttf", true, 1252).AsFont();

// CID font for Japanese (namespace Haru.Font.CID)
var japaneseFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "F3", "path/to/jp-font.ttf", 932).AsFont();
```

---

## Properties

### Dict

Gets the underlying PDF dictionary object.

```csharp
public HpdfDict Dict { get; }
```

**Usage:** For low-level PDF manipulation (advanced).

### BaseFont

Gets the PostScript name of the font.

```csharp
public string BaseFont { get; }
```

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
Console.WriteLine(font.BaseFont);  // "Helvetica"

var times = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesBold, "F2");
Console.WriteLine(times.BaseFont);  // "Times-Bold"
```

### LocalName

Gets the local resource name used to reference this font in page resources.

```csharp
public string LocalName { get; }
```

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
Console.WriteLine(font.LocalName);  // "F1"
```

**Note:** Each font on a page needs a unique local name: `SetFontAndSize` throws `HpdfException` (`DuplicateRegistration`) when the page already uses a different font object with the same name. Fonts from `pdf.GetFont` are named `HaruF` followed by a number (prefix `HpdfDocument.GeneratedNamePrefix`), so names of your own that do not start with `Haru` never clash with them.

### EncodingCodePage

Gets the code page used to encode text for this font.

```csharp
public int? EncodingCodePage { get; }
```

**Values:**
- `1252` - Windows Latin 1 (Western European; the default for standard fonts)
- `1250` - Windows Latin 2 (Central European)
- `1251` - Windows Cyrillic
- `1253` - Windows Greek
- `1254` - Windows Turkish
- `1255` - Windows Hebrew
- `1256` - Windows Arabic
- `1257` - Windows Baltic
- `1258` - Windows Vietnamese
- `932` - Japanese (Shift_JIS)
- `936` - Simplified Chinese (GBK)
- `949` - Korean (KS C 5601)
- `950` - Traditional Chinese (Big5)
- `65001` - UTF-8 (CID fonts, any Unicode text)

**Example:**

```csharp
var standardFont = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
Console.WriteLine(standardFont.EncodingCodePage);  // 1252

var trueTypeFont = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F2", "font.ttf", true, 1251).AsFont();
Console.WriteLine(trueTypeFont.EncodingCodePage);  // 1251
```

### IsCIDFont

Gets whether this is a CID font (Type 0 composite font).

```csharp
public bool IsCIDFont { get; }
```

**Usage:** CID fonts are used for CJK (Chinese, Japanese, Korean) languages.

**Example:**

```csharp
var latinFont = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
Console.WriteLine(latinFont.IsCIDFont);  // false

var japaneseFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JP", "jp-font.ttf", 932).AsFont();
Console.WriteLine(japaneseFont.IsCIDFont);  // true
```

---

## Methods

### MeasureText(string, float)

Calculates the width of text in user space units (points).

```csharp
public float MeasureText(string text, float fontSize)
```

**Parameters:**
- `text` - Text to measure
- `fontSize` - Font size in points

**Returns:** Width in points

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

string text = "Hello, World!";
float fontSize = 24;

// Measure text width
float width = font.MeasureText(text, fontSize);
Console.WriteLine($"Text width: {width} points");

// Center text on page
float x = (page.Width - width) / 2;
float y = page.Height / 2;

page.SetFontAndSize(font, fontSize);
page.BeginText();
page.MoveTextPos(x, y);
page.ShowText(text);
page.EndText();
```

**Common Uses:**
- Centering text
- Right-aligning text
- Text wrapping
- Table column sizing
- Layout calculations

The width does not include character spacing, word spacing or horizontal scaling. To find how many characters fit into a given width, use the overload `MeasureText(text, fontSize, width, charSpace, wordSpace, wordWrap, out realWidth)` from `HpdfFontExtensions`: it stops after a line feed or a carriage return that is not followed by a line feed, and ignores `wordSpace` for CID fonts, to which viewers do not apply word spacing. See [Measuring Text](../extensions/HpdfPageText.md#measuring-text).

### GetAscent()

Gets the font ascent in 1000-unit glyph space.

```csharp
public int GetAscent()
```

**Returns:** Ascent value (maximum height above baseline)

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
int ascent = font.GetAscent();
Console.WriteLine($"Ascent: {ascent}");  // e.g., 718

// Convert to user space for 12pt font
float ascentPoints = (ascent * 12) / 1000f;  // ~8.6 points
```

### GetDescent()

Gets the font descent in 1000-unit glyph space.

```csharp
public int GetDescent()
```

**Returns:** Descent value (maximum depth below baseline, negative)

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
int descent = font.GetDescent();
Console.WriteLine($"Descent: {descent}");  // e.g., -207

// Convert to user space for 12pt font
float descentPoints = (descent * 12) / 1000f;  // ~-2.5 points

// Calculate line height
int ascent = font.GetAscent();
float lineHeight = ((ascent - descent) * 12) / 1000f;  // ~11.1 points
```

### GetXHeight()

Gets the font x-height in 1000-unit glyph space.

```csharp
public int GetXHeight()
```

**Returns:** X-height (height of lowercase 'x')

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
int xHeight = font.GetXHeight();
Console.WriteLine($"X-Height: {xHeight}");  // e.g., 523

// Convert to user space for 12pt font
float xHeightPoints = (xHeight * 12) / 1000f;  // ~6.3 points
```

**Usage:** Useful for vertical alignment and typography calculations.

### GetBBox()

Gets the font bounding box in 1000-unit glyph space.

```csharp
public HpdfBox GetBBox()
```

**Returns:** `HpdfBox` with Left, Bottom, Right, Top coordinates

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var bbox = font.GetBBox();

Console.WriteLine($"BBox: [{bbox.Left}, {bbox.Bottom}, {bbox.Right}, {bbox.Top}]");
// e.g., [-166, -225, 1000, 931]

// Calculate maximum width and height for this font
float maxWidth = ((bbox.Right - bbox.Left) * 12) / 1000f;
float maxHeight = ((bbox.Top - bbox.Bottom) * 12) / 1000f;
```

### GetCharWidth(byte)

Gets the width of a single character in 1000-unit glyph space.

```csharp
public float GetCharWidth(byte charCode)
```

**Parameters:**
- `charCode` - Character code (byte value)

**Returns:** Character width

**Example:**

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F1");

// Courier is monospaced - all chars should have same width
byte charA = (byte)'A';
byte charB = (byte)'B';

float widthA = font.GetCharWidth(charA);
float widthB = font.GetCharWidth(charB);

Console.WriteLine($"'A' width: {widthA}");  // 600 (in Courier)
Console.WriteLine($"'B' width: {widthB}");  // 600 (same)

// Convert to user space for 12pt font
float widthPoints = (widthA * 12) / 1000f;  // 7.2 points
```

### ConvertTextToGlyphIDs(string)

Converts text to glyph IDs for CID fonts.

```csharp
public byte[] ConvertTextToGlyphIDs(string text)
```

**Parameters:**
- `text` - Text to convert

**Returns:** Byte array of glyph IDs

**Usage:** For CID fonts with CIDToGIDMap=Identity. Typically handled internally by the library. An embedded CID
font remembers the glyphs converted here as "in use": its widths, its ToUnicode map and its embedded subset are
written for them when the document is saved.

### EncodeText(string)

Encodes text to the bytes written to the content stream, using the font's encoding.

```csharp
public byte[] EncodeText(string text)
```

**Parameters:**
- `text` - Text to encode

**Returns:** The encoded bytes; text measurement and output use the same byte sequence.

---

## Standard Fonts (Base14)

The 14 standard PDF fonts available through `HpdfStandardFont` enum:

### Sans-Serif Fonts (Helvetica)

```csharp
HpdfStandardFont.Helvetica           // Normal weight
HpdfStandardFont.HelveticaBold       // Bold
HpdfStandardFont.HelveticaOblique    // Italic
HpdfStandardFont.HelveticaBoldOblique // Bold + Italic
```

**Characteristics:**
- Modern, clean appearance
- Good for headings and body text
- Similar to Arial

### Serif Fonts (Times)

```csharp
HpdfStandardFont.TimesRoman          // Normal weight
HpdfStandardFont.TimesBold           // Bold
HpdfStandardFont.TimesItalic         // Italic
HpdfStandardFont.TimesBoldItalic     // Bold + Italic
```

**Characteristics:**
- Classic, formal appearance
- Excellent for body text
- Similar to Times New Roman

### Monospace Fonts (Courier)

```csharp
HpdfStandardFont.Courier             // Normal weight
HpdfStandardFont.CourierBold         // Bold
HpdfStandardFont.CourierOblique      // Italic
HpdfStandardFont.CourierBoldOblique  // Bold + Italic
```

**Characteristics:**
- Fixed-width characters
- Perfect for code listings
- Similar to Courier New

### Symbol Fonts

```csharp
HpdfStandardFont.Symbol              // Greek letters and math symbols
HpdfStandardFont.ZapfDingbats        // Decorative symbols and dingbats
```

**Usage:**
- Symbol: Mathematical notation, Greek alphabet
- ZapfDingbats: Checkmarks, arrows, bullets, decorative elements

---

## Loading Custom Fonts

Load TrueType, Type1 and CID fonts with their font classes and call `AsFont()` to get the `HpdfFont`.
`pdf.GetFont(name, encoding)` (from `HpdfDocumentExtensions`) does not read font files: it returns a Base14 font by its
PostScript name (e.g. `"Helvetica-Bold"`) or a font registered earlier with `pdf.RegisterFont(font)`.

Embedded TrueType and CID fonts are stored as subsets (only the glyph outlines the document can use), written when
the document is saved; set `EmbedSubset = false` on the `HpdfTrueTypeFont` or `HpdfCIDFont` object to embed the whole
file. A font whose embedding permissions forbid embedding throws `HpdfException` (`TtfCannotEmbeddingFont`) when it
is loaded. To load one font file for many documents, see `FontDataPool`. All of this is described in the
[Fonts Guide](../../guides/FontsGuide.md#embedding).

### TrueType Fonts (.ttf)

```csharp
// Western European (Latin 1), embedded
var font1 = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "Arial1252", "fonts/arial.ttf", true, 1252).AsFont();

// Cyrillic
var font2 = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "Arial1251", "fonts/arial.ttf", true, 1251).AsFont();

// Greek
var font3 = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "Arial1253", "fonts/arial.ttf", true, 1253).AsFont();
```

### Type1 Fonts (.afm/.pfb)

```csharp
// Metrics from the AFM file; the PFB file is embedded
var type1Font = HpdfType1Font.LoadFromFile(pdf.Xref, "URW", "fonts/a010013l.afm", "fonts/a010013l.pfb", 1252).AsFont();
```

### CJK Fonts (CID)

`HpdfCIDFont` is in the `Haru.Font.CID` namespace.

```csharp
// Japanese
var jpFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JP", "fonts/noto-jp.ttf", 932).AsFont();

// Simplified Chinese
var cnFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "CN", "fonts/noto-sc.ttf", 936).AsFont();

// Traditional Chinese
var twFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "TW", "fonts/noto-tc.ttf", 950).AsFont();

// Korean
var krFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "KR", "fonts/noto-kr.ttf", 949).AsFont();

// Any language: Unicode text (UTF-8 / Identity-H)
var unicodeFont = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "U1", "fonts/noto.ttf", 65001).AsFont();
```

**Note:** CJK fonts require PDF 1.4 or higher. The library automatically upgrades the PDF version.

---

## Complete Examples

### Using Multiple Standard Fonts

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// Create various standard fonts
var helvetica = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var helveticaBold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");
var times = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesRoman, "F3");
var courier = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F4");

float y = page.Height - 100;

// Helvetica
page.SetFontAndSize(helvetica, 16);
page.BeginText();
page.MoveTextPos(50, y);
page.ShowText("Helvetica Normal");
page.EndText();

y -= 30;

// Helvetica Bold
page.SetFontAndSize(helveticaBold, 16);
page.BeginText();
page.MoveTextPos(50, y);
page.ShowText("Helvetica Bold");
page.EndText();

y -= 50;

// Times Roman
page.SetFontAndSize(times, 16);
page.BeginText();
page.MoveTextPos(50, y);
page.ShowText("Times Roman");
page.EndText();

y -= 50;

// Courier (monospace)
page.SetFontAndSize(courier, 14);
page.BeginText();
page.MoveTextPos(50, y);
page.ShowText("Courier Monospace for code: int x = 42;");
page.EndText();

pdf.SaveToFile("standard-fonts.pdf");
```

### Text Measurement and Centering

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");
var page = pdf.AddPage();

// Text to center
string title = "Centered Title";
float fontSize = 36;

// Measure text
float textWidth = font.MeasureText(title, fontSize);

// Calculate center position
float x = (page.Width - textWidth) / 2;
float y = page.Height - 100;

// Draw centered text
page.SetFontAndSize(font, fontSize);
page.BeginText();
page.MoveTextPos(x, y);
page.ShowText(title);
page.EndText();

// Draw vertical line at center for reference
page.SetLineWidth(0.5f);
page.SetRgbStroke(0.5f, 0.5f, 0.5f);
page.MoveTo(page.Width / 2, 0);
page.LineTo(page.Width / 2, page.Height);
page.Stroke();

pdf.SaveToFile("centered-text.pdf");
```

### Font Metrics Example

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

float fontSize = 48;
int ascent = font.GetAscent();
int descent = font.GetDescent();
int xHeight = font.GetXHeight();

// Convert to user space
float ascentPt = (ascent * fontSize) / 1000f;
float descentPt = (descent * fontSize) / 1000f;
float xHeightPt = (xHeight * fontSize) / 1000f;
float lineHeight = ascentPt + Math.Abs(descentPt);

// Draw baseline, ascent, descent, and x-height
float baselineY = 400;
float textX = 100;

// Baseline (black)
page.SetRgbStroke(0, 0, 0);
page.SetLineWidth(1);
page.MoveTo(50, baselineY);
page.LineTo(550, baselineY);
page.Stroke();

// Ascent line (red)
page.SetRgbStroke(1, 0, 0);
page.MoveTo(50, baselineY + ascentPt);
page.LineTo(550, baselineY + ascentPt);
page.Stroke();

// Descent line (blue)
page.SetRgbStroke(0, 0, 1);
page.MoveTo(50, baselineY + descentPt);
page.LineTo(550, baselineY + descentPt);
page.Stroke();

// X-height line (green)
page.SetRgbStroke(0, 0.7f, 0);
page.SetDash(new ushort[] { 3, 3 }, 0);
page.MoveTo(50, baselineY + xHeightPt);
page.LineTo(550, baselineY + xHeightPt);
page.Stroke();
page.SetDash(Array.Empty<ushort>(), 0);

// Draw text
page.SetFontAndSize(font, fontSize);
page.BeginText();
page.MoveTextPos(textX, baselineY);
page.ShowText("Typxg");  // Mix of ascenders, descenders, x-height
page.EndText();

// Labels
var smallFont = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F2");
page.SetFontAndSize(smallFont, 10);

page.BeginText();
page.MoveTextPos(560, baselineY);
page.ShowText("Baseline");
page.EndText();

page.BeginText();
page.MoveTextPos(560, baselineY + ascentPt);
page.ShowText("Ascent");
page.EndText();

page.BeginText();
page.MoveTextPos(560, baselineY + descentPt);
page.ShowText("Descent");
page.EndText();

page.BeginText();
page.MoveTextPos(560, baselineY + xHeightPt);
page.ShowText("X-Height");
page.EndText();

pdf.SaveToFile("font-metrics.pdf");
```

### Text Wrapping with MeasureText

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

float fontSize = 12;
float maxWidth = 400;
float lineSpacing = fontSize * 1.5f;
float x = 100;
float y = page.Height - 100;

string text = "This is a long paragraph that needs to be wrapped to fit within the specified width. " +
              "The MeasureText method helps us determine where to break lines for proper text wrapping.";

page.SetFontAndSize(font, fontSize);

var words = text.Split(' ');
var currentLine = "";

foreach (var word in words)
{
    var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
    float testWidth = font.MeasureText(testLine, fontSize);

    if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
    {
        // Current line is full, draw it and start new line
        page.BeginText();
        page.MoveTextPos(x, y);
        page.ShowText(currentLine);
        page.EndText();

        y -= lineSpacing;
        currentLine = word;
    }
    else
    {
        currentLine = testLine;
    }
}

// Draw remaining text
if (!string.IsNullOrEmpty(currentLine))
{
    page.BeginText();
    page.MoveTextPos(x, y);
    page.ShowText(currentLine);
    page.EndText();
}

pdf.SaveToFile("text-wrapping.pdf");
```

### Loading Custom TrueType Font

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();

// Load a custom TrueType font (embedded, code page 1252)
// Make sure the font file exists!
var customFont = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F1", "fonts/custom-font.ttf", true, 1252).AsFont();

var page = pdf.AddPage();

// Use the custom font
page.SetFontAndSize(customFont, 24);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Text in custom font");
page.EndText();

// Show font information
var infoFont = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F2");
page.SetFontAndSize(infoFont, 10);
page.BeginText();
page.MoveTextPos(100, 650);
page.ShowText($"Font: {customFont.BaseFont}");
page.EndText();

page.BeginText();
page.MoveTextPos(100, 635);
page.ShowText($"Code Page: {customFont.EncodingCodePage}");
page.EndText();

pdf.SaveToFile("custom-font.pdf");
```

---

## Best Practices

### 1. Reuse Font Objects

```csharp
// ✓ GOOD - Create once, use many times
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

for (int i = 0; i < 10; i++)
{
    var page = pdf.AddPage();
    page.SetFontAndSize(font, 12);  // Reuse same font
    // ... draw text ...
}
```

### 2. Use Appropriate Fonts for Content

```csharp
// Headings - bold sans-serif
var headingFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");

// Body text - serif
var bodyFont = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesRoman, "F2");

// Code listings - monospace
var codeFont = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F3");
```

### 3. Measure Text for Layout

```csharp
// Always measure before positioning
string text = "Important text";
float width = font.MeasureText(text, 16);

// Ensure it fits
if (width > page.Width - 100)
{
    // Use smaller font or wrap text
}
```

### 4. Handle Font Loading Errors

```csharp
HpdfFont font;
try
{
    font = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "F1", "path/to/font.ttf", true, 1252).AsFont();
}
catch (HpdfException ex) when (ex.ErrorCode == HpdfErrorCode.FileNotFound)
{
    Console.Error.WriteLine("Font file not found!");
    // Fall back to standard font
    font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
}
```

### 5. Use Standard Fonts When Possible

```csharp
// Standard fonts don't increase file size
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

// Custom fonts embed font data (larger files)
// Only use when necessary for branding or special characters
```

---

## Related Types

### HpdfStandardFont Enum

See [Standard Fonts](#standard-fonts-base14) section above.

### IHpdfFontImplementation Interface

Interface implemented by all font types:
- `HpdfStandardFontImpl` - Base14 fonts (internal)
- `HpdfTrueTypeFont` - TrueType fonts
- `HpdfType1Font` - PostScript Type1 fonts
- `HpdfCIDFont` - CJK and Unicode composite fonts (embedded TrueType)
- `HpdfCIDFontType0` - Predefined CJK fonts (not embedded)

### HpdfBox Struct

Returned by `GetBBox()`:

```csharp
public struct HpdfBox
{
    public HpdfBox(float left, float bottom, float right, float top);

    public float Left { get; set; }
    public float Bottom { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Width { get; }   // Right - Left
    public float Height { get; }  // Top - Bottom
}
```

---

## See Also

### Core Classes
- [HpdfDocument](HpdfDocument.md) - Document management
- [HpdfPage](HpdfPage.md) - Page operations

### Extension Methods
- [HpdfPageText](../extensions/HpdfPageText.md) - Text rendering operations
- `HpdfDocumentExtensions` - `GetFont(name, encoding)` for Base14 and registered fonts (see [HpdfDocument](HpdfDocument.md))

### Guides
- [Getting Started](../../guides/GettingStarted.md) - Basic tutorial
- [Fonts Guide](../../guides/FontsGuide.md) and [Text Guide](../../guides/TextGuide.md)

---

[← Back to Documentation Index](../../INDEX.md)

*Last updated: 2026-11-26*
