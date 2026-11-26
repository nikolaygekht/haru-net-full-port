# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project Overview

Haru.NET is a port of the Haru PDF library from C to .NET 8.0, published as the `Haru` NuGet package. The library provides comprehensive PDF generation capabilities with full international font support, including CJK languages (also Unicode via UTF-8 / Identity-H). All core features of the C library are ported and production-ready; see "Not Implemented" below for what is missing.

# Build & Test Commands

## Building
```bash
# Build the entire solution
dotnet build cs-src/Haru.sln

# Build only the library
dotnet build cs-src/Haru/Haru.csproj

# Build demos
dotnet build cs-src/Haru.Demos/BasicDemos.csproj
```

## Testing
```bash
# Run all tests
dotnet test cs-src/Haru.Test/Haru.Test.csproj

# Run a single test class
dotnet test cs-src/Haru.Test/Haru.Test.csproj --filter "FullyQualifiedName~HpdfFontTests"

# Run tests with detailed output
dotnet test cs-src/Haru.Test/Haru.Test.csproj -v normal
```

## Running Demos
```bash
# Run demo application (creates PDFs in ./pdfs folder)
dotnet run --project cs-src/Haru.Demos/BasicDemos.csproj
```

# Project Structure

```
cs-src/
├── Haru/                  # Main library
│   ├── Doc/               # Document, pages, catalog, outlines, images (HpdfImage), ExtGState
│   ├── Annotations/       # Annotations
│   ├── Forms/             # AcroForms (fields, widgets, appearance streams)
│   ├── Font/              # Font support (Base14, TrueType, Type1, CID)
│   │   ├── CID/           # CJK font support (Japanese, Chinese, Korean)
│   │   ├── TrueType/      # TrueType font parsing and embedding
│   │   └── Type1/         # PostScript Type1 font support
│   ├── Encoding/          # Text encoders
│   ├── Graphics/          # Graphics state and drawing helpers
│   ├── Objects/           # PDF primitive objects (Null, Boolean, Number, String, Name, Array, Dict, Stream)
│   ├── Png/               # Image decoding (PNG/JPEG pixels via StbImageSharp, IPngReader)
│   ├── Security/          # RC4 and AES encryption
│   ├── Streams/           # Stream handling and compression
│   ├── Types/             # Enums and constants
│   └── Xref/              # Cross-reference table
├── Haru.Test/            # Unit tests (xUnit + FluentAssertions + PdfPig)
└── Haru.Demos/           # Demo applications
    └── Resource/         # Fonts, images (auto-copied to output without "Resource" prefix)

docs/                     # Developer documentation, published as GitHub Pages (see .claude/DOCUMENTATION.md)
```

# Architecture

## Font System
The font system uses a polymorphic design with `IHpdfFontImplementation` interface:
- **HpdfStandardFontImpl** - Base14 fonts (Times, Helvetica, Courier, etc.) with embedded metrics
- **HpdfTrueTypeFont** - TrueType fonts (.ttf) with code page support (CP1251-1258)
- **HpdfType1Font** - PostScript fonts with AFM/PFB parsing
- **HpdfCIDFont** - CJK fonts using Type 0 composite fonts (CP932, CP936, CP949, CP950, UTF-8 / Identity-H)
- **HpdfCIDFontType0** - Predefined (non-embedded) CJK fonts

All fonts support:
- Font metrics (ascent, descent, x-height, bounding box)
- Text measurement with word wrapping
- Custom encodings and ToUnicode CMaps

## PDF Object Model
The library uses a complete PDF object hierarchy rooted in `HpdfObject`:
- Primitive types: `HpdfNull`, `HpdfBoolean`, `HpdfNumber`, `HpdfString`, `HpdfName`
- Collections: `HpdfArray`, `HpdfDict`
- Streams: `HpdfStream` (base class with FlateDecode compression)

Documents are managed through:
- **HpdfDocument** - Main entry point, manages Xref table
- **HpdfCatalog** - Document catalog (root object)
- **HpdfPages** - Pages tree
- **HpdfPage** - Individual pages with content streams

## Extension Methods Pattern
The library uses extension methods to organize functionality:
- **HpdfPageGraphics** - Path operations, colors, transformations
- **HpdfPageShapes** - High-level shapes (circles, ellipses, arcs)
- **HpdfPageText** - Text operations
- **HpdfDocumentExtensions** - Document-level operations

# Dependencies

- **System.IO.Compression** - Replaces zlib for PDF stream compression
- **StbImageSharp** - PNG/JPEG image parsing (replaced SixLabors.ImageSharp)
- **System.Text.Encoding.CodePages** - Code page support for international fonts

Test dependencies:
- **xUnit** - Test framework
- **FluentAssertions 7.x** - Assertion library (must stay below 8.0)
- **Moq** - Mocking framework
- **PdfPig** - Reads generated PDFs back in integration tests

Test code is built with analyzers as errors: dispose `IDisposable` objects (e.g. `using var image = ...`), and
note that `Encoding` inside test namespaces resolves to `Haru.Test.Encoding` — use `System.Text.Encoding`.

# Code Quality Rules

## Non-Negotiable
- Apply SOLID principles
- Find root causes, no workarounds
- Consider the whole scenario, not just the problem point
- No performance anti-patterns
- Load non-changing data once, not repeatedly

## C to .NET Porting Guidelines
- Rely on .NET GC instead of explicit C-style memory management
- Replace C structs with classes/records as appropriate
- Use null-conditional operators instead of manual null checks
- Prefer `Span<T>` and `Memory<T>` for buffer operations
- Use `System.IO.Compression.DeflateStream` instead of zlib

# Session Management

- **LAST.md** / **REMAINING.md** (repo root) - Status and remaining work. They were removed in Nov 2025 (commit `5a7fd9d`); if present, read at session start and update before ending. The "Not Implemented" list below is the current source of truth.
- **cs-src/Haru.Demos/CLAUDE.md** - Demo project information (resources, folder structure)
- **.claude/DOCUMENTATION.md** - Standards for the developer documentation in `docs/` (published as GitHub Pages)
- **DOC_PLAN.md** / **DOC_STATUS.md** - Documentation plan and progress; update `DOC_STATUS.md` when public API changes

# Key Implementation Notes

## Resource Paths in Demos
Resources in `cs-src/Haru.Demos/Resource/` are auto-copied to output without "Resource" prefix:
- Source: `Resource/demo/ttfont/noto.ttf`
- Runtime: `demo/ttfont/noto.ttf`

Demo PDFs are saved to `./pdfs/` folder relative to the executable.

## PDF Version Management
The document version is `max(HpdfDocument.Version as set by the user, HpdfXref.RequiredVersion)`.
Any feature that needs a newer PDF version calls `xref.RequireVersion(...)` when it is used — the requirement
is never lowered, and setting `Version` lower than the content needs has no effect. Current requirements:
- Transparency (PNG alpha / `SetSoftMask` → `/SMask`, ExtGState alpha and blend modes), CID fonts, RC4-128 (R3), PDF/A-1: 1.4
- AcroForms: 1.5
- AES encryption (R4): 1.6

When adding a feature that needs a newer PDF version, call `RequireVersion` rather than assigning `Version`.

## Image Transparency
- PNG alpha channel → automatic `/SMask` (an image's color type is detected from all pixels; fully gray or fully opaque images are stored without the unused channels)
- `HpdfImage.SetColorMask` (color key, 8-bit DeviceRGB only), `SetMaskImage` (1-bit stencil; light mask pixels hide), `SetSoftMask` (grayscale opacity)
- An image can carry only one mask; whole-image opacity is done via `HpdfExtGState.SetAlphaFill` before `DrawImage`

## Not Implemented
- CCITT G3/G4 and 1-bit image loading, image interpolation flag
- Name dictionary (named destinations, embedded files, JavaScript), file attachments (ExData / FileSpec)
- User units, page thumbnails, per-page metadata
- PDF/A beyond basic PDF/A-1b (no font-embedding or transparency checks)
- 3D annotations (U3D)

## Known Issues
- Signature fields work in Adobe Acrobat but not Chrome/Edge
- Checkboxes and radio buttons work in Chrome, not in Edge; in Acrobat they update only on blur

# C Source Reference

Original Haru C library source is in `c-src/` folder. Use for reference when porting features or debugging compatibility issues.
