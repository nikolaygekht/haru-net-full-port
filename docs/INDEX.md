---
title: Haru.NET Documentation
permalink: /
---

# Haru.NET Documentation

**Version 1.1**
A complete .NET port of the Haru Free PDF Library

**Find your way:** [Table of Contents](CONTENTS.md) · [Alphabetical Index](API_INDEX.md) ·
[Getting Started](guides/GettingStarted.md)

---

## Overview

Haru.NET is a comprehensive, cross-platform PDF generation library for .NET 8 and later. It provides low-level PDF primitives for creating professional-quality PDF documents without licensing fees or restrictions. The library offers full international font support, including CJK (Chinese, Japanese, Korean) languages, complete graphics capabilities, and extensive document security features.

### What is Haru.NET?

Haru.NET is a pure .NET implementation that allows developers to programmatically create PDF documents with complete control over:

- **Document Structure** - Pages, page sizes, layouts, and metadata
- **Typography** - Standard fonts, OpenType fonts (TrueType and CFF outlines), Type1 fonts, and CID fonts for international text
- **Graphics** - Vector graphics, paths, shapes, colors, transformations, and transparency
- **Images** - PNG and JPEG image embedding with positioning and scaling
- **Forms** - Interactive AcroForm fields (text fields, checkboxes, radio buttons, etc.)
- **Security** - RC4 and AES encryption with granular permissions
- **Navigation** - Bookmarks, annotations, and document outlines

### Target Audience

Haru.NET is designed for .NET developers who need:

- Cross-platform PDF generation without licensing restrictions
- Precise control over PDF structure and appearance
- Professional-quality document output
- International language support with complex scripts
- Lightweight library with minimal dependencies

For document-level layout and formatting, consider using **PDF.Flow** library which adds a high-level layout layer on top of Haru.NET.

---

## Quick Start

### Installation

Install via NuGet package:

```bash
dotnet add package Haru
```

**Requirements:**
- .NET 8.0 or later
- No additional prerequisites

**Dependencies:**
- StbImageSharp (permissive license)

### Your First PDF

```csharp
using Haru.Doc;
using Haru.Font;

// Create a new PDF document
var pdf = new HpdfDocument();

// Create a font
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

// Add a page (default: A4 portrait)
var page = pdf.AddPage();

// Write text
page.SetFontAndSize(font, 24);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Hello, PDF World!");
page.EndText();

// Draw a rectangle
page.SetLineWidth(2);
page.Rectangle(50, 50, 500, 200);
page.Stroke();

// Save the document
pdf.SaveToFile("hello.pdf");
```

---

## Documentation Structure

All pages are listed in the [Table of Contents](CONTENTS.md); every type, property and method is in the
[Alphabetical Index](API_INDEX.md).

### Getting Started
- [Quick Start Guide](USAGE.md) - Installation and basic usage
- [Getting Started Tutorial](guides/GettingStarted.md) - Step-by-step first PDF
- [Library Architecture](STRUCTURE.md) - Design patterns and structure with UML diagrams
- [License Information](LICENSE.md) - MIT license details

### User Guides
- [Fonts Guide](guides/FontsGuide.md) - Working with Standard, TrueType, Type1, and CID/CJK fonts
- [Text Guide](guides/TextGuide.md) - Text rendering, positioning, wrapping, and formatting
- [Graphics Guide](guides/GraphicsGuide.md) - Drawing paths, shapes, colors, transformations, and transparency
- [Images Guide](guides/ImagesGuide.md) - Loading and placing PNG/JPEG images, masks and transparency
- [Forms Guide](guides/FormsGuide.md) - Creating interactive AcroForm fields
- [Annotations Guide](guides/AnnotationsGuide.md) - Sticky notes, links and review comments: shapes, lines, text markup, free text, stamps, pop-ups
- [Outlines Guide](guides/OutlinesGuide.md) - Bookmarks, destinations, and page labels
- [Encryption Guide](guides/EncryptionGuide.md) - Document security and permissions

### API Reference

#### Core Classes
- [HpdfDocument](api/core/HpdfDocument.md) - Main entry point for PDF creation
- [HpdfPage](api/core/HpdfPage.md) - Page manipulation and content
- [HpdfFont](api/core/HpdfFont.md) - Font wrapper for typography
- [HpdfImage](api/core/HpdfImage.md) - Image objects for embedding
- [HpdfCatalog](api/core/HpdfCatalog.md) - Page layout, page mode, viewer preferences, open action
- [HpdfInfo](api/core/HpdfInfo.md) - Document information (title, author, dates)
- [HpdfOutline](api/core/HpdfOutline.md) - Bookmarks
- [HpdfDestination](api/core/HpdfDestination.md) - Targets of bookmarks, links and the open action
- [HpdfPageLabel](api/core/HpdfPageLabel.md) - Page numbering shown by viewers
- [HpdfExtGState](api/core/HpdfExtGState.md) - Transparency and blend modes
- [HpdfPages](api/core/HpdfPages.md) - Page tree nodes

#### Extension Methods
- [HpdfPageGraphics](api/extensions/HpdfPageGraphics.md) - Low-level graphics operations
- [HpdfPageShapes](api/extensions/HpdfPageShapes.md) - High-level shape drawing
- [HpdfPageText](api/extensions/HpdfPageText.md) - Text operations
- [HpdfPageExtensions](api/extensions/HpdfPageExtensions.md) - Convenience methods
- [HpdfDocumentExtensions](api/extensions/HpdfDocumentExtensions.md) - Document-level operations

#### Annotations (namespace `Haru.Annotations`)
- [HpdfAnnotation](api/annotations/HpdfAnnotation.md) - Base class: border, color and flags
- [HpdfLinkAnnotation](api/annotations/HpdfLinkAnnotation.md) - Web links and links to pages
- [HpdfMarkupAnnotation](api/annotations/HpdfMarkupAnnotation.md) - Base class of the review comments: author, subject, date, opacity, pop-up; generated appearances
  - [HpdfTextAnnotation](api/annotations/HpdfTextAnnotation.md) - Sticky notes
  - [HpdfGeometricAnnotation](api/annotations/HpdfGeometricAnnotation.md) - Base class of squares, circles and lines: interior color
  - [HpdfShapeAnnotation](api/annotations/HpdfShapeAnnotation.md) - Squares and circles, cloudy borders
  - [HpdfLineAnnotation](api/annotations/HpdfLineAnnotation.md) - Lines: endings, leader lines, captions
  - [HpdfTextMarkupAnnotation](api/annotations/HpdfTextMarkupAnnotation.md) - Highlight, underline, squiggly, strikeout
  - [HpdfFreeTextAnnotation](api/annotations/HpdfFreeTextAnnotation.md) - Text boxes on the page, callouts
  - [HpdfStampAnnotation](api/annotations/HpdfStampAnnotation.md) - Rubber stamps
- [HpdfPopupAnnotation](api/annotations/HpdfPopupAnnotation.md) - Pop-up window of a markup annotation
- Enums: [HpdfAnnotationType](api/annotations/HpdfAnnotationType.md), [HpdfAnnotationFlags](api/annotations/HpdfAnnotationFlags.md),
  [HpdfAnnotationIcon](api/annotations/HpdfAnnotationIcon.md), [HpdfBorderStyle](api/annotations/HpdfBorderStyle.md),
  [HpdfHighlightMode](api/annotations/HpdfHighlightMode.md), [HpdfLineEndingStyle](api/annotations/HpdfLineEndingStyle.md),
  [HpdfLineCaptionPosition](api/annotations/HpdfLineCaptionPosition.md), [HpdfLineIntent](api/annotations/HpdfLineIntent.md),
  [HpdfFreeTextIntent](api/annotations/HpdfFreeTextIntent.md), [HpdfStampName](api/annotations/HpdfStampName.md)

#### Types and Enumerations
Geometry and color (namespace `Haru.Types`):
- [HpdfRect](api/types/HpdfRect.md) - Rectangle by its edges (annotations, widgets)
- [HpdfBox](api/types/HpdfBox.md) - Page boxes (CropBox, BleedBox, TrimBox, ArtBox) and font bounding boxes
- [HpdfPoint](api/types/HpdfPoint.md), [HpdfTransMatrix](api/types/HpdfTransMatrix.md), [HpdfDashMode](api/types/HpdfDashMode.md)
- [HpdfRgbColor](api/types/HpdfRgbColor.md), [HpdfCmykColor](api/types/HpdfCmykColor.md)
- Not used by the public API at present: [HpdfPoint3D](api/types/HpdfPoint3D.md), [Hpdf3DMatrix](api/types/Hpdf3DMatrix.md),
  [HpdfDate](api/types/HpdfDate.md), [HpdfTextWidth](api/types/HpdfTextWidth.md)

Page sizes (namespace `Haru.Doc`):
- [HpdfPageSize](api/types/HpdfPageSize.md), [HpdfPageDirection](api/types/HpdfPageDirection.md), [HpdfPageSizes](api/types/HpdfPageSizes.md)

Enumerations:
- Document: [HpdfPageLayout](api/types/HpdfPageLayout.md), [HpdfPageMode](api/types/HpdfPageMode.md),
  [HpdfViewerPreferences](api/types/HpdfViewerPreferences.md), [HpdfPageNumStyle](api/types/HpdfPageNumStyle.md),
  [HpdfTransitionStyle](api/types/HpdfTransitionStyle.md), [HpdfCompressionMode](api/types/HpdfCompressionMode.md)
- Security: [HpdfEncryptMode](api/types/HpdfEncryptMode.md), [HpdfPermission](api/types/HpdfPermission.md)
- Graphics and text: [HpdfLineCap](api/types/HpdfLineCap.md), [HpdfLineJoin](api/types/HpdfLineJoin.md),
  [HpdfBlendMode](api/types/HpdfBlendMode.md), [HpdfColorSpace](api/types/HpdfColorSpace.md),
  [HpdfTextRenderingMode](api/types/HpdfTextRenderingMode.md), [HpdfTextAlignment](api/types/HpdfTextAlignment.md),
  [HpdfWritingMode](api/types/HpdfWritingMode.md)
- [Reserved types](api/types/ReservedTypes.md) - Enums kept from the C library that no public member uses

Constants: [HpdfConst](api/types/HpdfConst.md), [HpdfCountry](api/types/HpdfCountry.md), [HpdfLang](api/types/HpdfLang.md)

#### Forms (namespace `Haru.Forms`)
- [HpdfAcroForm](api/forms/HpdfAcroForm.md) - The document's form: fields, default appearance, NeedAppearances
- [HpdfField](api/forms/HpdfField.md) - Base class of all fields (value, widgets, flags)
- [HpdfTextField](api/forms/HpdfTextField.md) - Single-line, multiline and password text fields
- [HpdfCheckbox](api/forms/HpdfCheckbox.md) - Checkboxes
- [HpdfRadioButton](api/forms/HpdfRadioButton.md) - Radio button groups
- [HpdfChoice](api/forms/HpdfChoice.md) - Combo boxes and list boxes
- [HpdfSignature](api/forms/HpdfSignature.md) - Unsigned signature fields
- [HpdfWidgetAnnotation](api/forms/HpdfWidgetAnnotation.md) - Placement and look of a field on a page (incl. `HpdfHighlightMode`)
- Enums: [HpdfFieldAlignment](api/forms/HpdfFieldAlignment.md), [HpdfFieldFlags](api/forms/HpdfFieldFlags.md), [HpdfFieldType](api/forms/HpdfFieldType.md)
- Button appearances (namespace `Haru.Forms.Appearance`): [ButtonElementType](api/forms/ButtonElementType.md),
  [IElementAppearance](api/forms/IElementAppearance.md), [HpdfAppearanceFactory](api/forms/HpdfAppearanceFactory.md),
  [HpdfCheckboxAppearance](api/forms/HpdfCheckboxAppearance.md), [HpdfRadioButtonAppearance](api/forms/HpdfRadioButtonAppearance.md),
  [HpdfWidgetFrame](api/forms/HpdfWidgetFrame.md)

### Code Examples
Every guide and API page contains runnable examples; the `cs-src/Haru.Demos` project in the repository builds a
PDF for each feature area.

---

## Key Features

### Font System
- **Base14 Fonts** - Standard PDF fonts (Times, Helvetica, Courier, Symbol, ZapfDingbats)
- **OpenType Fonts** - Embed .ttf, .otf and .ttc fonts (TrueType and CFF outlines, as subsets) with code page support (CP1251-1258)
- **Type1 Fonts** - PostScript fonts with AFM/PFB parsing
- **CID Fonts** - Complete CJK support (Japanese CP932, Chinese CP936/CP950, Korean CP949), horizontal and vertical writing, and Unicode (UTF-8 / Identity-H)
- **Font Metrics** - Accurate text measurement with word wrapping
- **Custom Encodings** - ToUnicode CMaps for international text

### Graphics Capabilities
- **Vector Graphics** - Lines, curves, Bézier paths, rectangles, circles, ellipses, arcs
- **Color Spaces** - RGB and CMYK color support
- **Transformations** - Rotation, scaling, translation, and arbitrary matrix transformations
- **Transparency** - Stroke/fill opacity, blend modes and graphics state management
- **Clipping Paths** - Complex clipping regions
- **Line Styles** - Width, dash patterns, cap styles, join styles

### Document Features
- **Page Management** - Multiple page sizes (A4, Letter, Legal, etc.) and orientations
- **Compression** - FlateDecode compression for efficient file sizes
- **Encryption** - RC4 and AES encryption with user/owner passwords
- **Permissions** - Granular control (print, copy, modify, etc.)
- **Metadata** - Title, author, subject, keywords, creation date
- **Navigation** - Outlines (bookmarks), annotations, links
- **PDF Versions** - Support for PDF 1.2 through 1.7

### Image Support
- **PNG** - Full PNG support including alpha-channel transparency
- **JPEG** - JPEG image embedding
- **Raw pixels** - Grayscale, RGB and CMYK data from memory
- **Masking** - Color key masks, stencil (1-bit) masks and grayscale soft masks
- **Positioning** - Flexible image placement and scaling

### Interactive Forms (AcroForms)
- Text fields, checkboxes, radio buttons, combo boxes
- Signature fields (works in Adobe Acrobat)
- Field flags (read-only, required, multiline, password) and maximum text length
- Requires PDF 1.5+

---

## Architecture Overview

### Main Entry Points

**HpdfDocument** - The primary class for creating PDF documents. Manages document lifecycle, page creation, font loading, image loading, encryption, and file saving.

**HpdfPage** - Represents individual pages. Provides access to page size, content streams, and serves as the target for all graphics and text operations.

### Extension Methods Pattern

Haru.NET organizes functionality using extension methods to keep the API clean and discoverable:

- **HpdfPageGraphics** - Path operations, colors, line styles, transformations
- **HpdfPageShapes** - High-level shapes (circles, ellipses, arcs)
- **HpdfPageText** - Text rendering, positioning, line spacing
- **HpdfDocumentExtensions** - Standard font lookup, compression mode

### Polymorphic Font System

The font system uses a unified interface (`IHpdfFontImplementation`) with specialized implementations:

```
HpdfFont (wrapper)
  └─ IHpdfFontImplementation
      ├─ HpdfStandardFontImpl    (Base14 fonts)
      ├─ HpdfTrueTypeFont        (TrueType .ttf)
      ├─ HpdfType1Font           (PostScript Type1)
      ├─ HpdfCIDFont             (CJK composite fonts, embedded TrueType)
      └─ HpdfCIDFontType0        (predefined CJK fonts, not embedded)
```

All fonts provide consistent text measurement, metrics, and rendering capabilities.

---

## Version Management

Haru.NET automatically manages PDF versions based on features used:

- **PDF 1.2** - Basic documents
- **PDF 1.4+** - Required for transparency (PNG alpha channel, soft masks, opacity, blend modes), CID fonts (CJK support), 128-bit RC4 encryption and PDF/A-1
- **PDF 1.5+** - Required for AcroForms
- **PDF 1.6+** - Required for AES encryption

The library automatically upgrades the PDF version when you use features that require it.
Setting `HpdfDocument.Version` explicitly acts as a minimum: the version written to the file is never lower than what the document content requires.

---

## Font Licensing

**Important:** Fonts are copyrighted material. Ensure you have the right to embed fonts in your PDFs.

**Recommended font sources:**
- Reader-supported fonts (Helvetica, Times, Courier - no embedding needed)
- Free/open-source fonts from [Google Fonts](https://fonts.google.com/)
- Properly licensed commercial fonts

---

## Support and Contributing

### Reporting Issues
Report bugs and issues on GitHub: https://github.com/nikolaygekht/haru-net-full-port/issues

### Contributing
1. Clone the repository
2. Make your changes
3. Create a pull request

### Building from Source
```bash
# Build the library
dotnet build cs-src/Haru/Haru.csproj

# Run unit tests (xUnit v3 on Microsoft Testing Platform; requires the .NET 10 SDK)
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj

# Run demos (creates PDFs in ./pdfs folder)
dotnet run --project cs-src/Haru.Demos/BasicDemos.csproj
```

---

## License

Haru.NET is licensed under the MIT License:

```
MIT License

Copyright (c) 1999-2006 Takeshi Kanno <takeshi_kanno@est.hi-ho.ne.jp>
Copyright (c) 2007-2009 Antony Dovgal <tony@daylessday.org>
Copyright (c) 2016-2026 Nikolay Gekht

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Haru.NET is a port of the original Haru Free PDF Library (libharu), which is distributed under the ZLIB/LIBPNG License.
See [LICENSE.md](LICENSE.md) for the original library's notice and complete details.

---

## Project Status

**Current Version:** 1.1 (NuGet package `Haru`)
**Status:** Production-ready; all core features of the C library are ported. Not yet available: CCITT fax images, file attachments and named destinations, 3D annotations.
**Test Coverage:** Unit and integration tests (xUnit, PdfPig)
**Platform:** .NET 8+ (Windows, Linux, macOS)

---

## Next Steps

1. Read the [Quick Start Guide](USAGE.md) for installation and basic usage
2. Follow the [Getting Started Tutorial](guides/GettingStarted.md) to create your first PDF
3. Explore the [User Guides](#user-guides) for specific features
4. Reference the [API Documentation](#api-reference) for detailed class information

---

*Last updated: 2026-11-26*
