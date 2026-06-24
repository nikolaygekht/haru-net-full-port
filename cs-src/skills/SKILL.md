---
name: haru-net-pdf
description: |
  Skill for working with Haru.NET PDF generation library. Triggers when: importing Haru.Doc,
  Haru.Font, or Haru.Types namespaces; referencing HpdfDocument, HpdfPage, HpdfFont classes;
  working with PDF generation in projects using the Haru package; questions about PDF creation,
  fonts, graphics, or text rendering with Haru.NET.
packages: Haru
---

# Haru.NET PDF Generation Library

Pure C# port of libharu with no native dependencies. Generate PDFs with text, graphics, fonts, and images.

## Quick Start

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

page.SetFontAndSize(font, 24);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Hello PDF!");
page.EndText();

pdf.SaveToFile("output.pdf");
```

## Core Workflow

1. Create `HpdfDocument`
2. Create fonts via `new HpdfFont(pdf.Xref, ...)`
3. Add pages with `pdf.AddPage()`
4. Draw content (always `BeginText()` / `EndText()` for text)
5. Save with `pdf.SaveToFile(path)` or `pdf.SaveToMemory()`

## Critical Gotchas

### 1. Coordinate System
**Bottom-left origin**: (0, 0) is bottom-left; Y increases upward (opposite of most graphics APIs).

```csharp
// Position text 100pt from TOP of page (not bottom)
page.BeginText();
page.MoveTextPos(50, page.Height - 100);  // Subtract from Height
page.ShowText("Text from top");
page.EndText();
```

Page dimensions in points (72 pts = 1 inch):
- A4: 595 × 842 (portrait)
- Letter: 612 × 792

### 2. Text Requires BeginText/EndText
```csharp
// ❌ WRONG - will fail/not render
page.MoveTextPos(100, 700);
page.ShowText("Text");

// ✓ CORRECT
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Text");
page.EndText();
```

### 3. Font Must Be Set Before Text
```csharp
// Always call SetFontAndSize before ShowText
page.SetFontAndSize(font, 12);
page.BeginText();
page.ShowText("Text");
page.EndText();
```

### 4. Color Values: 0.0 to 1.0 (Not 0-255)
```csharp
// ❌ WRONG
page.SetRgbFill(255, 0, 0);

// ✓ CORRECT
page.SetRgbFill(1.0f, 0.0f, 0.0f);  // Pure red
```

## Fonts

### Standard Fonts (No Embedding)
```csharp
var helvetica = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var helveticaBold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");
var times = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesRoman, "F3");
var timesBold = new HpdfFont(pdf.Xref, HpdfStandardFont.TimesBold, "F4");
var courier = new HpdfFont(pdf.Xref, HpdfStandardFont.Courier, "F5");
```

Available: Helvetica, Times-Roman, Courier (+ Bold, Italic, BoldItalic), Symbol, ZapfDingbats.

### TrueType Fonts
```csharp
using Haru.Font;

// Load .ttf file with encoding (CP1251-CP1258 supported)
var customFont = pdf.GetFont("path/to/font.ttf", "CP1252");

page.SetFontAndSize(customFont, 18);
page.BeginText();
page.MoveTextPos(50, 500);
page.ShowText("Custom font text");
page.EndText();
```

### CJK Fonts (Chinese, Japanese, Korean)
```csharp
// Load CID fonts for Asian languages
var japaneseFont = pdf.GetFont("path/to/font.ttf", "CP932");   // Japanese
var chineseFont = pdf.GetFont("path/to/font.ttf", "CP936");    // Simplified Chinese
var koreanFont = pdf.GetFont("path/to/font.ttf", "CP949");     // Korean
var traditionalFont = pdf.GetFont("path/to/font.ttf", "CP950"); // Traditional Chinese
```

### Text Measurement
```csharp
string text = "Sample Text";
float textWidth = font.MeasureText(text, fontSize);

// Center text on page
float x = (page.Width - textWidth) / 2;
page.BeginText();
page.MoveTextPos(x, y);
page.ShowText(text);
page.EndText();
```

## Graphics

### Shapes
```csharp
// Rectangle (x, y, width, height)
page.Rectangle(100, 600, 200, 100);
page.Stroke();  // Outline only

// Filled rectangle
page.SetRgbFill(0.8f, 0.2f, 0.2f);
page.Rectangle(100, 450, 200, 100);
page.Fill();

// Fill + stroke
page.Rectangle(100, 300, 200, 100);
page.FillStroke();

// Circle (x, y, radius)
page.Circle(200, 200, 50);
page.Stroke();

// Ellipse (x, y, xRadius, yRadius)
page.Ellipse(300, 200, 80, 40);
page.Fill();

// Lines
page.MoveTo(50, 100);
page.LineTo(300, 150);
page.LineTo(500, 100);
page.Stroke();
```

### Line Styles
```csharp
page.SetLineWidth(3);

// Dashed lines
ushort[] pattern = new ushort[] { 5, 3 };  // 5 on, 3 off
page.SetDash(pattern, 0);
page.MoveTo(50, 300);
page.LineTo(500, 300);
page.Stroke();

// Reset to solid
page.SetDash(Array.Empty<ushort>(), 0);
```

### Colors
```csharp
// RGB (0.0 to 1.0)
page.SetRgbFill(1.0f, 0.0f, 0.0f);     // Red fill
page.SetRgbStroke(0.0f, 0.0f, 1.0f);   // Blue stroke

// CMYK (for print)
page.SetCmykFill(1.0f, 0.0f, 0.0f, 0.0f);  // Cyan
page.SetCmykStroke(0.0f, 1.0f, 0.0f, 0.0f); // Magenta

// Grayscale (0.0 = black, 1.0 = white)
page.SetGrayFill(0.5f);
page.SetGrayStroke(0.0f);
```

### Graphics State
```csharp
// Save/restore state for temporary changes
page.GSave();
page.SetLineWidth(5);
page.SetRgbStroke(1, 0, 0);
// ... draw ...
page.GRestore();  // Restore previous state
```

## Images

```csharp
// Load images
var pngImage = pdf.LoadPngImageFromFile("image.png");
var jpegImage = pdf.LoadJpegImageFromFile("photo.jpg");

// Draw at position with size (x, y, width, height)
page.DrawImage(pngImage, 100, 600, 200, 150);

// Maintain aspect ratio
float aspectRatio = (float)jpegImage.Height / jpegImage.Width;
float width = 300;
float height = width * aspectRatio;
page.DrawImage(jpegImage, 100, 400, width, height);
```

## Pages

```csharp
using Haru.Types;

// Standard sizes with orientation
var page1 = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);
var page2 = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Landscape);
var page3 = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Portrait);

// Custom size (points)
var page4 = pdf.AddPage();
page4.SetSize(600, 800);  // Custom dimensions
```

## Document Metadata

```csharp
pdf.Info.SetTitle("Document Title");
pdf.Info.SetAuthor("Author Name");
pdf.Info.SetSubject("Subject");
pdf.Info.SetKeywords("keyword1, keyword2");
pdf.Info.SetCreator("Application Name");
```

## Compression

```csharp
// Enable compression (smaller file sizes)
pdf.SetCompressionMode(HpdfCompressionMode.All);
```

## Save Options

```csharp
// To file
pdf.SaveToFile("output.pdf");

// To memory (e.g., for HTTP response)
byte[] pdfBytes = pdf.SaveToMemory();
```

## Common Patterns

### Centered Text
```csharp
string text = "Centered Text";
float fontSize = 24;
page.SetFontAndSize(font, fontSize);
float textWidth = font.MeasureText(text, fontSize);

page.BeginText();
page.MoveTextPos((page.Width - textWidth) / 2, page.Height / 2);
page.ShowText(text);
page.EndText();
```

### Page Border
```csharp
page.SetLineWidth(2);
page.Rectangle(50, 50, page.Width - 100, page.Height - 100);
page.Stroke();
```

### Multiple Text Lines
```csharp
float y = page.Height - 100;
float lineHeight = 20;

page.SetFontAndSize(font, 12);
foreach (var line in lines)
{
    page.BeginText();
    page.MoveTextPos(50, y);
    page.ShowText(line);
    page.EndText();
    y -= lineHeight;
}
```

### Reuse Fonts Across Pages
```csharp
// Create font once
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var page1 = pdf.AddPage();
page1.SetFontAndSize(font, 12);
// ... use font ...

var page2 = pdf.AddPage();
page2.SetFontAndSize(font, 12);  // Reuse same font
// ... use font ...
```

## Extension Methods Organization

Haru.NET uses extension methods for functionality:
- **Haru.Doc** namespace: `HpdfDocument`, `HpdfPage`, `HpdfFont`
- **HpdfPageGraphics** (extension methods): Lines, curves, colors, transformations
- **HpdfPageShapes** (extension methods): Circles, ellipses, arcs
- **HpdfPageText** (extension methods): Text operations
- **HpdfDocumentExtensions** (extension methods): Font/image loading

Import `using Haru.Doc;` and `using Haru.Font;` to access all methods.

## Troubleshooting

**Text doesn't appear:**
- Check `SetFontAndSize()` was called before `BeginText()`
- Verify coordinates are within page bounds (not negative, not > page dimensions)

**Wrong position:**
- Remember (0,0) is bottom-left; use `page.Height - y` to position from top

**Color not working:**
- Use 0.0-1.0 range, not 0-255

**Shapes not visible:**
- Must call `Stroke()`, `Fill()`, or `FillStroke()` after drawing path

## Advanced Features

### Encryption
```csharp
// RC4 or AES encryption
pdf.SetPassword("owner-password", "user-password");
pdf.SetEncryptionMode(HpdfEncryptMode.R3, 128);
```

### Annotations & Links
```csharp
// Create text annotation
var annot = page.CreateTextAnnot(
    new HpdfRect(100, 100, 200, 150),
    "Annotation text",
    new HpdfEncoder(pdf.Xref, "StandardEncoding", "E1")
);

// Create web link
var link = page.CreateURILinkAnnot(
    new HpdfRect(100, 500, 300, 520),
    "https://example.com"
);
```

### Outlines (Bookmarks)
```csharp
var outline = pdf.CreateOutline(null, "Chapter 1", pdf.Encoder);
var dest = page.CreateDestination();
dest.SetXYZ(0, page.Height, 0);
outline.SetDestination(dest);
```

## Resources

- GitHub: https://github.com/nikolaygekht/haru-net-by-claude
- Demos: https://github.com/nikolaygekht/haru-net-by-claude/tree/main/cs-src/Haru.Demos
- Original libharu: https://github.com/libharu/libharu

## License

ZLIB/LIBPNG License - free for commercial and non-commercial use.
