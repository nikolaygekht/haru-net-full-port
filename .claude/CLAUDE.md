# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project Overview

Haru.NET is a port of the Haru PDF library from C to .NET, published as the `Haru` NuGet package. The library provides comprehensive PDF generation capabilities with full international font support, including CJK languages (also Unicode via UTF-8 / Identity-H). The library targets **net8.0** so that .NET 8 users keep working; tests, demos and tools target net10.0. All core features of the C library are ported and production-ready; see "Not Implemented" below for what is missing.

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
Tests use xUnit v3 on Microsoft Testing Platform (MTP). `/global.json` switches `dotnet test` to MTP mode,
which requires the **.NET 10 SDK** (the library targets net8.0, all other projects net10.0). Do not raise the library target framework without a reason: it breaks .NET 8 consumers of the NuGet package.
The large test and demo fonts (`NotoSansJP-Regular.otf`, `NotoSansCJK-Regular.ttc`) are stored with **Git LFS**
(paths in `/.gitattributes`): a clone needs `git lfs install` and `git lfs pull`, else the font tests fail with a
message from `TestFonts`. New large binaries go to LFS by path, not by extension.

```bash
# Run all tests
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj

# Run a single test class / method (xUnit v3 filters; wildcards allowed)
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-class "*.HpdfFontTests"
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-method "*.NewDocument_IsVersion12"
# (the VSTest-style --filter "FullyQualifiedName~HpdfFontTests" is still accepted)

# Show every test result
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --output Detailed

# Code coverage (VSTest's --collect is not supported under MTP)
dotnet-coverage collect "dotnet test --solution cs-src/Haru.sln" -f xml -o coverage.xml

# The test project is also an executable (xUnit native switches, e.g. -class); useful to diagnose discovery
dotnet run --project cs-src/Haru.Test/Haru.Test.csproj -- -class "Haru.Test.Font.HpdfFontTests"
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
│   ├── Annotations/       # Annotations (link, text, markup, 3D, projection) and their generated appearances
│   ├── Forms/             # AcroForms (fields, widgets, appearance streams)
│   ├── ThreeD/            # 3D models (U3D, PRC), views, nodes, measurements
│   ├── Font/              # Font support (Base14, TrueType, Type1, CID)
│   │   ├── CID/           # CJK font support (Japanese, Chinese, Korean)
│   │   ├── TrueType/      # OpenType (sfnt) font files: tables, TrueType outlines, embedding of both outline kinds
│   │   ├── Cff/           # CFF outlines of OpenType fonts: parsing, desubroutinizing, writing CFF programs
│   │   └── Type1/         # PostScript Type1 font support
│   ├── Encoding/          # Text encoders
│   ├── Graphics/          # Graphics state and drawing helpers
│   ├── Objects/           # PDF primitive objects (Null, Boolean, Number, String, Name, Array, Dict, Stream)
│   ├── Png/               # Image decoding (PNG/JPEG pixels via StbImageSharp, IPngReader)
│   ├── Security/          # RC4 and AES encryption
│   ├── Streams/           # Stream handling and compression
│   ├── Types/             # Enums and constants
│   └── Xref/              # Cross-reference table
├── Haru.Test/            # Unit tests (xUnit + AwesomeAssertions + PdfPig)
└── Haru.Demos/           # Demo applications
    └── Resource/         # Fonts, images (auto-copied to output without "Resource" prefix)

docs/                     # Developer documentation, published as GitHub Pages (see .claude/DOCUMENTATION.md)
```

# Architecture

## Font System
The font system uses a polymorphic design with `IHpdfFontImplementation` interface:
- **HpdfStandardFontImpl** - Base14 fonts (Times, Helvetica, Courier, etc.) with embedded metrics
- **HpdfTrueTypeFont** - OpenType fonts (.ttf, .otf; TrueType font or, for CFF outlines, Type1 font with a Type1C program) with code page support (CP1251-1258)
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
- **CodePagesEncodingProvider** - Code page support for international fonts; part of the shared framework since .NET Core 3.0, so no `System.Text.Encoding.CodePages` package

Test dependencies:
- **xUnit v3** (`xunit.v3`, MTP v2) - Test framework; the test project is an executable (`OutputType=Exe`).
  No `Microsoft.NET.Test.Sdk` / `xunit.runner.visualstudio` (VSTest only); IDE test explorers need MTP support
  (Visual Studio 2022 17.14+ / 2026, Rider 2025.1+, VS Code C# Dev Kit)
- **AwesomeAssertions 9.x** - Fluent assertion library (Apache-2.0 fork of FluentAssertions; `using AwesomeAssertions;`).
  Do not add FluentAssertions back: 8.0+ requires a paid commercial license. Use `.Should()` style only, no `Assert.*`.
- **Moq** - Mocking framework
- **PdfPig** - Reads generated PDFs back in integration tests

Test code is built with analyzers as errors: dispose `IDisposable` objects (e.g. `using var image = ...`), and
note that `Encoding` inside test namespaces resolves to `Haru.Test.Encoding` — use `System.Text.Encoding`.

# Code Quality Rules

## Licensing and File Headers
- Do **not** add copyright or license header comments to source files (`.cs`, `.csproj`, scripts). The license
  lives only in the root `LICENSE` (MIT; original Haru authors Takeshi Kanno and Antony Dovgal, C# port
  Copyright (c) 2016-2026 Nikolay Gekht). Keep the original authors' lines wherever the license text is quoted.
- A short `/* ... */` comment describing the file's purpose is fine; it must not contain copyright text.
- Keep the original libharu ZLIB/LIBPNG notice in `LICENSE`, `docs/LICENSE.md` and `cs-src/nuget/readme.md` —
  that license forbids removing it.

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
- **DOC_PLAN.md** / **DOC_STATUS.md** - Documentation plan and progress; update `DOC_STATUS.md` when public API changes.
  Compile every `csharp` example of a page you write or change against `cs-src/Haru` (scratch project outside the repo):
  the older pages contained invented API until the 2026-11-26 audit. `DOC_STATUS.md` also lists library issues found
  while documenting.

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
- Image masks (`SetColorMask`, `SetMaskImage` → `/Mask`), page labels, JavaScript (document scripts, the open
  script, JavaScript links): 1.3 (RC4-40 / R2 needs only 1.1; named destinations and the name dictionary need only 1.2)
- Transparency (PNG alpha / `SetSoftMask` → `/SMask`, ExtGState alpha and blend modes), CID fonts, RC4-128 (R3), PDF/A-1: 1.4
- AcroForms, page layouts `TwoPageLeft` / `TwoPageRight`, page mode `UseOC`: 1.5
- AES encryption (R4), viewer preference `PrintScalingNone`, page mode `UseAttachments`: 1.6
- Annotations: each type and entry raises the version it needs (table on `docs/api/annotations/HpdfMarkupAnnotation.md`);
  e.g. Polygon/Caret 1.5, Redact 1.7, 3D 1.6, a highlight 1.4 (blend mode), document attachments 1.4, `/UF` 1.7
- Adobe ExtensionLevel 3 (`HpdfXref.RequireAdobeExtensionLevel`, written as `/Extensions /ADBE` in the catalog, needs 1.7):
  PRC models, 3D measurements, projection annotations - the PDF 2.0 features that Adobe's supplement to 1.7 added first

When adding a feature that needs a newer PDF version, call `RequireVersion` rather than assigning `Version`.

## Image Transparency
- PNG alpha channel → automatic `/SMask` (an image's color type is detected from all pixels; fully gray or fully opaque images are stored without the unused channels)
- `HpdfImage.SetColorMask` (color key, 8-bit DeviceRGB or DeviceGray; a gray image gets the intersection of the three ranges), `SetMaskImage` (1-bit stencil; light mask pixels hide), `SetSoftMask` (grayscale opacity)
- An image can carry only one mask; whole-image opacity is done via `HpdfExtGState.SetAlphaFill` before `DrawImage`

## Not Implemented
Checked against the exported functions of `c-src/hpdf.h` on 2026-11-26; keep this list current when adding features,
so the code does not have to be re-audited. Missing C API functions are given in parentheses.

Text and page:
- Multiple / shared content streams (`HPDF_Page_New_Content_Stream`, `HPDF_Page_Insert_Shared_Content_Stream`),
  page-level XObject helpers (`HPDF_Page_CreateXObjectFromImage`, `HPDF_Page_CreateXObjectAsWhiteRect`)
- User units, per-page metadata
- Page thumbnails (`/Thumb`): left out on purpose. Tried on 2026-11-27 with a demo of checkerboard thumbnails: Adobe
  Acrobat ignores embedded thumbnails and draws its own, so `SetThumbnail` was removed again (commit `93b7cec` and
  its revert); the library cannot draw pages, generation would need a renderer such as PDFium

Document:
- A deeper page tree than the two levels of `HPDF_SetPagesConfiguration` (ported 2026-11-27 as
  `HpdfDocument.SetPagesConfiguration`: the root holds nodes of at most N pages)
- Name trees other than EmbeddedFiles, Dests and JavaScript (AP, Pages, Templates, IDS, URLS, Renditions); links to
  other files (GoToR) and actions other than GoTo, URI and JavaScript (no outline actions, no additional actions /AA).
  The JavaScript open action is done (`HpdfDocument.SetOpenJavaScript`, 2026-11-27).
  Done on 2026-11-27: document attachments (`HpdfDocument.AttachFile`), named destinations
  (`CreateNamedDestination`), document JavaScript (`AddJavaScript`), JavaScript links
  (`HpdfPage.CreateJavaScriptLinkAnnotation`, the C `HPDF_LinkAnnot_SetJavaScript`)
- PDF/A beyond basic PDF/A-1b (no font-embedding or transparency checks)

Annotations: all annotation functions of the C library are ported (2026-11-27: markup annotations, pop-ups, flags,
3D annotations, views, nodes, 3D measurements 3DC and PD3, projection annotations), plus types it does not have
(Polygon, PolyLine, Ink, Caret, FileAttachment, Redact, rich text `/RC`, replies `/IRT`). Not done:
- Sound annotations (deprecated in PDF 2.0; left out on purpose), Movie, Screen, PrinterMark, TrapNet, Watermark, RichMedia
- 3D measurements other than 3DC and PD3 (LD3, RD3, AD3 - not in the C library either), 3D animations, 3D JavaScript
  beyond `/OnInstantiate`, the PDF 2.0 `/Path` of polygons, redaction overlay appearances (`/RO`)
- A generator of 3D models: the 3D demo uses the PRC example of OpTeX pdfextra (`Resource/demo/3d/README.md`)

Images, color and fonts:
- Loading CCITT G3/G4 coded data (fax / TIFF files; not in the C library either). Done on 2026-11-27: 1-bit images
  coded with CCITT G4 (`HpdfImage.LoadRaw1BitImageFromMem`, `Streams/CcittG4Encoder`, also used by
  `LoadRawImageFromMem` for 1-bit gray), the interpolation flag (`HpdfImage.Interpolate`; PDF/A-1 saving fails with it)
- ICCBased color spaces (`HPDF_LoadIccProfileFromFile`); ICC is supported only in the PDF/A output intent
- Proportional vertical metrics (GPOS `vpal`) and Latin text turned on its side in vertical writing (done by the
  layout engine in Adobe applications and CSS `text-orientation: mixed`; the `vrt2` sideways glyphs are not used);
  neither is in the C library, which has no vertical writing for embedded fonts
- Predefined CMaps that the C library does not have either (the Unicode ones, `UniGB-UCS2-H` ...): all 16 CMaps of
  the C library are there; `HpdfCIDFontType0` needs a `CIDEncoder` with the code to CID ranges of another CMap
- Fonts with CFF2 outlines (variable OpenType fonts), CFF fonts with an Expert charset, and a simple font
  (`HpdfTrueTypeFont`) of a CID-keyed CFF font (all CJK `.otf`): `UnsupportedFontType`. CFF outlines themselves are
  supported (not in the C library, which reads TrueType outlines only). Accented glyphs of a CFF font with glyph names
  made of two glyphs (`endchar` with four arguments, seac) work in a simple font (Type1C keeps the names) but not in a
  CID font, whose CID-keyed program has no names to find the components by; none of the test fonts has such glyphs,
  the composition (base and shifted accent, without hints) is not written

Not ported on purpose (replaced by .NET idioms): error handler / `HPDF_GetError*` (exceptions), memory management
(`HPDF_Free*`, `HPDF_NewEx`, MMgr), `HPDF_Use*Fonts` / `HPDF_Use*Encodings` (fonts and encodings are always available),
`HPDF_CreateJavaScript` / `HPDF_LoadJSFromFile` (scripts are strings: `AddJavaScript`, `CreateJavaScriptLinkAnnotation`,
`Hpdf3DModel.SetOnInstantiate`).

Ported on 2026-11-26 (no longer missing): `HpdfPageText.TextRect`, `HpdfPage.Rotate`, `EoFillStroke` /
`ClosePathEoFillStroke`, `SetFlat`, `HpdfCatalog.ViewerPreferences`, `HpdfCatalog.SetOpenAction`; on 2026-11-27
`HPDF_LoadTTFontFromFile2` (`HpdfTrueTypeFont.LoadFromCollection`, `HpdfCIDFont.LoadFromTrueTypeCollection`).

Documentation status is tracked in `DOC_STATUS.md`: every public type has a reference page and there are 10 guides,
published at https://nikolaygekht.github.io/haru-net-full-port/; open are the Phase 3 items.

## Known Issues
- The open code issues are listed in `DOC_STATUS.md`, "Library Issues Found While Documenting". The font plan of
  2026-11-26 (`FONT_PLAN.md`) is complete and removed; what it did is in the git history (commits `5c7448d` to
  `25755f7`), the rules that came out of it are under "Implementation Rules Learned the Hard Way".
- AcroForms: every widget has its own `/AP`. Text and choice fields get it when the document is saved
  (`HpdfAcroForm.PrepareForWriting` -> `HpdfField.GenerateAppearances`), drawn with the standard font named by `/DA`
  (Helv, TiRo, Cour, ... - `Forms/Appearance/HpdfFormFonts`), which also go to the form's `/DR` (plus `ZaDb` when
  the form has buttons). Limits: only standard 14 fonts and WinAnsi text (a `/DA` with another font name leaves the
  widget without `/AP`), no comb fields, no list-box scrolling (`/TI`). `NeedAppearances` stays on by default, so
  interactive viewers may redraw fields their own way. Checkboxes, radio buttons and signature fields draw their
  `/AP` at widget creation (frame from `/MK`, redrawn by `HpdfWidgetAnnotation.SetAppearanceCharacteristics`).
- 3D, verified in Adobe Acrobat (2026-11-27): a projection annotation with an area makes Acrobat crash, so it is
  written with `/Rect [0 0 0 0]` (zero rect or Hidden opens; /S, /AP made no difference). A 3D view without a camera
  uses the model's camera: fine for U3D files that have one, but a PRC file usually has none and the model is then
  out of sight - every view of a PRC model needs `SetCamera` (no views at all: Acrobat fits the model itself).
  Artwork lighting of a model without lights is black. The 3D measurement `/C` is `[r g b]` (Adobe supplement),
  `/TRL` is the name string.
- Free text annotations: the font of `/DA` goes to the `/DR` of an existing AcroForm (`HpdfAnnotation
  .DefaultAppearanceFont`, collected by `HpdfDocument.PrepareAnnotations` on every save); no form is created for it
  (the user's decision), so without a form a viewer that redraws the annotation on editing may substitute a font.

## Implementation Rules Learned the Hard Way
- Text strings: `new HpdfString(string)` writes a PDF text string (ASCII as is, otherwise UTF-16BE with BOM `FE FF`);
  `HpdfString.GetText()` decodes it. Byte strings that must stay ASCII (URI actions) are built from bytes
  (`HpdfLinkAnnotation` percent-encodes non-ASCII URIs). Never decode an `HpdfString` with `Encoding.UTF8` directly.
- Saving is repeatable: `HpdfDocument.Save` may run any number of times and must write the same file for an unchanged
  document. Code that runs while saving assigns dictionary entries (`dict[key] = ...`) instead of `Add`, and creates
  its objects once (PDF/A metadata stream is reused and refreshed, the document ID is kept).
- Annotations with a generated `/AP` (the markup annotations but Text and Projection, which have none, and 3D
  annotations; pop-ups have none either) draw it in `HpdfDocument.Save` before
  anything is written (`PrepareAnnotations` -> `HpdfAnnotation.PrepareForWriting`), not in an `IHpdfBeforeWrite`
  hook: hooks run in registration order, fonts register before the annotations that use them, and a font writes its
  subset, `/W` and `/ToUnicode` from the glyphs encoded until then. The PDF header is written before the hooks too, so
  annotation setters call `RequireVersion` themselves. The stream (`HpdfAnnotationAppearance`) is created with the
  annotation and redrawn on every save, in page coordinates (`/BBox` = `/Rect`); geometry is kept in fields and
  `/Rect`, `/RD` are derived from it, never read back. `/CA` goes into the stream (ExtGState): viewers ignore it
  when an `/AP` exists. Graphics state operators (`w`, colors, `d`) come before a path, never between its
  construction and painting. Shared pieces live once: colors `HpdfAnnotationColor`, `/RD` `HpdfRectDifference`,
  `/QuadPoints` `HpdfQuad` (written in viewer order UL, UR, LL, LR), `/BE` clouds `HpdfCloudyBorder` (any polygon,
  also concave), line endings `HpdfLineEndings`, line wrapping `Font/HpdfTextLines`, the Tj operand
  `HpdfFont.WriteTextOperand`. `PrepareAnnotations` also enforces PDF/A-1 (only PDF 1.4 annotation types but
  FileAttachment, no opacity below 1) and checks that a reply (`/IRT`) is on the page of the annotation it answers.
- The name trees of the catalog's name dictionary (EmbeddedFiles, Dests, JavaScript) are written in one place,
  `Doc/HpdfNameDictionary` (`SetTree`, called from `HpdfDocument.ApplyNameTrees` on every save): one sorted `/Names`
  node per tree, into the user's `/Names` dictionary when one is set; a tree without entries is not written. A
  `/Dest` that goes to a named destination is `HpdfNamedDestination.CreateReference()`, the same text string as the
  tree key. Script streams and JavaScript actions come from `Doc/HpdfJavaScript` (also the 3D `/OnInstantiate`).
  PdfPig 0.1.16 did not resolve the named destinations of an encrypted test file (Cyrillic name, RC4-128; MuPDF did):
  check encrypted names with another reader.
- Stream filters with parameters: `HpdfStreamObject` writes `/Filter` as an array, so `/DecodeParms` must be an array
  of the same length (a dictionary next to a filter array is ignored by MuPDF, which then decoded G4 data as 1D).
- 1-bit images (`HpdfImage.LoadRaw1BitImageFromMem`, CCITT G4): the encoder treats 1 bits as black, and `/BlackIs1
  true` makes the decoder output black as 1, which DeviceGray shows as white. The C parameter `black_is1` is that
  filter entry, so C's `true` shows the 0 bits black; the C# parameter is named by its meaning (the user's decision):
  `oneIsBlack` (default false), written as `/BlackIs1 !oneIsBlack`. Verified bit for bit with PdfPig and MuPDF.
- RC4: every string and stream is encrypted from the start of its object's key stream (`HpdfEncrypt.Encrypt` calls
  `HpdfArc4.Reset`, which restores the permutation, not only the indices).
- Objects that depend on how the document was used are finished right before writing: the object implements
  `IHpdfBeforeWrite` (`Haru.Xref`) and registers with `HpdfXref.RegisterBeforeWrite`; `HpdfXref.WriteToStream` calls it
  on every save. `HpdfCIDFont` writes `/ToUnicode` and `/W` this way from the glyphs shown (`_usedGlyphs`, filled by
  `ConvertTextToGlyphIDs`; the content holds glyph IDs). The rules of repeatable saving apply to such code.
- Embedded fonts are subsets by default (`EmbedSubset`), written in the `IHpdfBeforeWrite` hook from the original
  font file by `TrueTypeSubsetter`: glyph IDs never change (unused glyphs become empty), so content, `/W`, the cmap
  and a re-save stay valid; the font name gets a deterministic tag (`ABCDEF+Name`) in the dictionaries only -
  `BaseFont` of the font object stays the plain name. `fsType` is checked by `TrueTypeEmbeddingRights`.
- A TrueType font file is read in one place, `Font/TrueType/TrueTypeFontFile` (tables, names, cmap formats 4 and
  12, glyph widths, 1000-unit metrics; immutable, so `FontDataPool` shares one parsed file between fonts and
  threads). `HpdfTrueTypeFont` and `HpdfCIDFont` only build their PDF objects on it: the common descriptor entries
  come from `TrueTypeFontDescriptor`, the embedded font program (rights check, subset, name tag) from
  `TrueTypeFontProgram`. Do not parse font tables in a font class. Everything a font writes to a PDF from the font
  file is in 1000-unit glyph space (`TrueTypeFontFile.Scale`), the descriptor too; cap height and x-height are the
  tops of `H` and `x` (libharu), else `sCapHeight` / `sxHeight` of an OS/2 table of version 2 or later.
- A font of a TrueType collection (`.ttc`) is a `TrueTypeFontFile` too (`Parse(data, fontIndex)`): `Data` is the
  whole collection, shared by its fonts, and `DirectoryOffset` is where the font's table directory is (table offsets
  are from the start of the file in both kinds of files). Code that reads the table directory takes that offset
  (`TrueTypeSubsetter`); never write `Data` of a font in a collection to a PDF: the whole font is
  `TrueTypeSubsetter.ExtractFont` (`TrueTypeFontProgram`). `Parse(data)` of a collection throws `InvalidTtcFile`.
- CFF outlines (`TrueTypeFontFile.Cff`, `Haru.Font.Cff.CffFont`, parsed once and immutable: offsets into the bytes of
  the file, the charstrings are not copied) are embedded as a CFF program in `/FontFile3`, never as the OpenType file:
  `CffFontProgram` (`Type1C` for a simple font, which is a Type1 font; `CIDFontType0C` for a CID font, whose descendant
  is `CIDFontType0` without `/CIDToGIDMap`). The font classes take the program from `EmbeddedFontProgram.Create`
  (`IEmbeddedFontProgram`, also `TrueTypeFontProgram`); a font with neither outline kind cannot be embedded
  (`TtfMissingTable`). `CffSubsetWriter` stores the glyphs one after the other and maps them back to the glyph IDs
  of the font file: a CID-keyed program (also made of a font with glyph names) gets the charset new glyph -> CID =
  original glyph ID with ROS Adobe-Identity-0, a Type1C program keeps the glyph names (the font's `/Encoding` is a
  `/Differences` array of the real charset names for every code). So the content, `/W`, `/W2` and `/ToUnicode` hold
  glyph IDs for both outline kinds. A subset is desubroutinized (`CffCharStringFlattener`: subroutines are inlined
  per glyph, with the stack and the stem count of the call, so hint masks get their length; seac components are
  added): measured smaller than pruning subroutines by index (a CJK FD has up to 30000 local subroutines). A
  charstring it cannot follow (arithmetic operators) makes the program be written whole. Vertical origins of CFF
  fonts come from `VORG`. Check a change with fontTools: the outline (RecordingPen) of every glyph of the program
  must equal the one of the original glyph.
- Font namespaces are layers, not font kinds: `Haru.Font.TrueType` (the OpenType / sfnt file), `Haru.Font.Cff` (the
  CFF format, used by the sfnt layer and knowing nothing of it) and `Haru.Font.Type1` (AFM/PFB) are file-format code
  that knows nothing about each other (but for sfnt -> CFF) or about the font classes; the font classes
  (`HpdfTrueTypeFont`, `HpdfType1Font` in `Haru.Font`, `HpdfCIDFont` in `Haru.Font.CID`) use the layer of the file
  they are made from. Code shared by font kinds that is not about a file format lives in `Haru.Font`
  (`ToUnicodeCMap`, `HpdfGlyphNames`).
- Encoding names (`GetFont`, `RegisterFont`) are interpreted in one place, `Haru.Encoding.HpdfEncodingNames`; an
  unknown name throws `InvalidEncodingName`. The registry key is the font name and the encoding, not the code page
  (`HpdfEncodingNames.GetRegistryKey`: the CMap of a composite font - a predefined CMap, `Identity-H` / `Identity-V`
  of an embedded CID font - else the code page plus `-V` for vertical writing), so encodings that share a code page
  are registered side by side; a code page name finds a horizontal font of that code page.
- Predefined CJK fonts (`HpdfCIDFontType0`): widths are given by CID, so every character code goes through the
  encoder's CMap (`CIDEncoder.ReadCID`; tables in the generated `CIDCMapData.cs`, from `c-src/hpdf_encoder_*.c`).
  Never treat encoded bytes as CIDs and never add width entries to `Font/CID/Data/*.json` that are not in the C
  font definitions (`c-src/hpdf_fontdef_*.c`) - they are written to `/W`. No `/ToUnicode` for a predefined CMap.
  The predefined CMaps are listed once, in `Font/CID/CMapDefinition` (name, character collection, code page, lead
  and trail bytes, code to CID table): `CIDEncoder.Create` and `HpdfEncodingNames` read it. A CMap of a narrow
  character set gets the strict code page (20936, 51949), so that a character outside it becomes `?`. The code page
  of the font is the one of its encoder; the `codePage` argument of the older `Create` overload is ignored.
- Vertical writing is a property of the font (`HpdfFont.WritingMode`, `Haru.Types.HpdfWritingMode`), which a
  predefined CJK font takes from its CMap: a `-V` CMap is its `-H` CMap plus the CIDs of the vertical glyphs
  (`CMapDefinition.VerticalCidRanges`; libharu's `CMAP_ARRAY_EUC_V` gives its CIDs with a `0x` prefix by mistake,
  the generator reads them as decimal). A vertical font writes `/DW2 [880 -1000]`, and `MeasureText` returns the
  advance along the column (1000 per character), as `HPDF_Type0Font_TextWidth` does. The content stream needs
  nothing special: the reader lays the glyphs out from `/WMode` of the CMap.
- An embedded CID font is vertical when it is loaded so (`HpdfCIDFont.LoadFromTrueTypeFile(..., HpdfWritingMode)`,
  not in the C library): `/Encoding /Identity-V`, `/DW2 [originY -1000]` (`CIDVerticalMetrics`, shared with the
  predefined fonts; the origin is the typographic ascender when the typographic metrics span one em, else 880),
  and the glyphs go through the `vert` feature of the font's GSUB table (`TrueTypeFontFile.GetVerticalGlyphId`;
  single substitutions, lookups applied in order, scripts and languages not told apart). Not `vrt2`: the two
  features are exclusive (OpenType spec, Adobe), Adobe and browsers use `vert`, Microsoft Office `vrt2`, and
  Noto CJK / Source Han have a `vrt2` of one lookup, a subset of `vert`; applying both is against the spec. `GetGlyphId`
  returns the vertical glyph, so drawing, measuring, `/W`, `/W2`, `/ToUnicode` and the subset all see the same
  glyph. The advance height of a glyph is the one of `vmtx` (one em without `vhea`/`vmtx`), in `MeasureText` and in
  the file; the vertical origin is the top of the outline plus the top side bearing (`TrueTypeFontFile
  .GetVerticalOriginY`, as HarfBuzz does for TrueType outlines). Glyphs in use that differ from `/DW2` go to `/W2`
  (`[cid [-advance width/2 originY ...]]`), written before saving like `/W`. `vpal` is not applied.
- In vertical writing the character spacing (Tc) is added upwards while the text runs down: a negative Tc moves
  the characters apart. Code that measures or justifies text converts with `HpdfFont.GetSpacingAlongText`
  (`HpdfFontExtensions.MeasureText`, `TextRect`). Horizontal scaling (Tz) does not change the advance of a column,
  only the width of the glyphs. `TextRect` with a vertical font lays out columns from the right edge to the left
  (`TextRectVertical`), one text leading apart; the line breaking is shared with the horizontal layout
  (`TryTakeLine`). This layout is not in the C library, whose `HPDF_Page_TextRect` ignores the writing mode.
- Where a line may break is decided in one place, `HpdfFontExtensions.MeasureText` (used by `TextRect` and the form
  appearances): after whitespace, and with `wordWrap` between characters of Chinese and Japanese text where
  `HpdfLineBreaking.IsBreakAllowed` lets it (line-start and line-end forbidden characters, by Unicode code point;
  Hangul is not in the set, Korean words stay whole). A forbidden break is simply not a break opportunity, so the
  character before moves to the next line. `wordWrap: false` stays "anywhere": it is the fallback when nothing fits.
- `HpdfCIDFont` maps text to glyphs in one place (`GetGlyphId(Rune)`), for drawing and for measuring; the code page
  only sets `/Lang`. Text is iterated by runes: a surrogate pair is one character and one glyph (cmap format 12),
  also where text is measured with a width limit (`HpdfFontExtensions.MeasureText`). Measure with the rounded 1000-unit widths that are written to the file (`/Widths`, `/W`).
- `HpdfTextAlignment` (`Haru.Types`: Left, Right, Center, Justify, C order) is for page text (`TextRect`);
  `HpdfFieldAlignment` (`Haru.Forms`: values equal `/Q`) is for form fields. Do not cast one to the other.

# C Source Reference

Original Haru C library source is in `c-src/` folder. Use for reference when porting features or debugging compatibility issues.
