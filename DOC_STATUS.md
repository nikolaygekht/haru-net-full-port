# Haru.NET Documentation Progress Status

**Last Updated:** 2026-11-27
**Overall Progress:** Phases 1 and 2 complete: 128 pages - every public type has a reference page, 10 guides.
Phase 3 (migration guide, troubleshooting, performance) not started.

Standards: [.claude/DOCUMENTATION.md](.claude/DOCUMENTATION.md). Plan: [DOC_PLAN.md](DOC_PLAN.md).

---

## Verification (2026-11-26)

All pages were checked against the code:

- Every `csharp` example in `docs/` was compiled against `cs-src/Haru` (scratch projects outside the repository);
  the new pages' examples were also run.
- Invented API was removed from the older pages (e.g. `Info.SetTitle()` -> `Info.Title`, `Catalog.SetPageLayout()`
  -> `PageLayout`, `AcroForm.CreateTextField()`, `GetFont(path)` for font files -> `HpdfTrueTypeFont.LoadFromFile`
  / `HpdfCIDFont.LoadFromTrueTypeFile` / `HpdfType1Font.LoadFromFile`, wrong `HpdfRect` and enum values, invented
  members in the STRUCTURE.md diagrams).
- All relative links and `#anchors` resolve; pages not written yet are listed as plain text "(planned)".

When the public API changes, compile the examples of the affected pages again and update this file.

---

## Publishing (GitHub Pages)

| Item | Status | Notes |
|------|--------|-------|
| `docs/` at repository root, `_config.yml`, Mermaid include, home page | ✅ Done | Theme `jekyll-theme-primer`, `jekyll-relative-links` |
| Broken links on the site | ✅ Done | No links to missing files or directories |
| Enable Pages in repository settings | ✅ Done | Deploy from branch `main`, `/docs`; published at https://nikolaygekht.github.io/haru-net-full-port/ |

---

## Phase 1: Core Documentation

| File | Status | Notes |
|------|--------|-------|
| INDEX.md, LICENSE.md, USAGE.md, STRUCTURE.md | ✅ Complete | Audited 2026-11-26 |
| guides/GettingStarted.md | ✅ Complete | Audited 2026-11-26 |
| api/core/HpdfDocument.md | ✅ Complete | Catalog (PageLayout, PageMode, ViewerPreferences, SetOpenAction), Info members and Unicode text, repeatable saving, real form API; `AttachFile`, `AttachedFiles`, `Load3DModel`, Adobe extension level, PDF/A rules (2026-11-27); `CreateNamedDestination`, `NamedDestinations`, `AddJavaScript` (2026-11-27) |
| api/core/HpdfPage.md | ✅ Complete | `Rotate`, widget example, `HpdfRect` by edges; `Annotations` and the markup annotation methods (2026-11-27); polygon, polyline, ink, caret, redaction, file attachment, 3D and projection annotations (2026-11-27); links to named destinations, JavaScript links (2026-11-27) |
| api/core/HpdfFont.md | ✅ Complete | Font file loaders, Type1, UTF-8 CID |
| api/core/HpdfImage.md | ✅ Complete | Loading (PNG/JPEG/raw), drawing, transparency: PNG alpha, `SetColorMask`, `SetMaskImage`, `SetSoftMask`, ExtGState opacity |
| api/extensions/HpdfPageGraphics.md | ✅ Complete | Incl. `SetFlat`, `EoFillStroke`, `ClosePathEoFillStroke`, `IDrawable`, ExtGState |
| api/extensions/HpdfPageShapes.md | ✅ Complete | `Arc` angle convention (clockwise from 12 o'clock, as in the C library) |
| api/extensions/HpdfPageText.md | ✅ Complete | Incl. `TextRect`, measuring text, `HpdfPageExtensions` helpers |
| guides/FontsGuide.md | ✅ Complete | Standard, TrueType, Type1, CID/CJK fonts; UTF-8 / Identity-H section |
| guides/GraphicsGuide.md | ✅ Complete | Paths, shapes, colors, transformations, clipping |
| guides/TextGuide.md | ✅ Complete | Positioning, wrapping with `TextRect`, spacing, rendering modes |
| guides/ImagesGuide.md | ✅ Complete | Can build on HpdfImage.md |

## Phase 2: Extended Documentation

| File | Status | Notes |
|------|--------|-------|
| guides/FormsGuide.md | ✅ Complete | All field types, `HpdfFieldAlignment`, default appearances and fonts, generated `/AP`, `NeedAppearances`, frames |
| guides/EncryptionGuide.md | ✅ Complete | Passwords (rules as in the C library), permissions, R2/R3/R4 |
| guides/AnnotationsGuide.md | ✅ Complete | Notes, web links, links to pages, borders, highlight modes; markup annotations (2026-11-27): review comments, generated appearances, squares and circles, lines, text markup, free text and callouts, stamps, pop-ups, flags, PDF/A, PDF versions; polygons and polylines, ink, carets, cloudy free text, redaction, file attachments, replies, groups and rich text, 3D (2026-11-27) |
| guides/ThreeDGuide.md | ✅ Complete | New (2026-11-27): models (U3D, PRC), views, camera, lighting, cross sections, nodes, activation, poster, comments that show a view, measurements and projection annotations, scripts, versions, viewer support, differences from the C library |
| guides/OutlinesGuide.md | ✅ Complete | Bookmark trees, destinations, open action, page labels |
| api/extensions/HpdfDocumentExtensions.md, HpdfPageExtensions.md | ✅ Complete | |
| api/forms/* | ✅ Complete | 17 pages: one per public type of `Haru.Forms` and `Haru.Forms.Appearance` (incl. `HpdfHighlightMode` on the widget page) |
| api/types/* | ✅ Complete | 33 pages: structs, constant classes, page sizes, used enums; `ReservedTypes.md` lists 15 enums kept from the C library that no public member uses; `HpdfPoint3D` and `Hpdf3DMatrix` are used by the 3D API (2026-11-27) |
| api/core: HpdfCatalog, HpdfDestination, HpdfExtGState, HpdfOutline, HpdfInfo, HpdfPageLabel, HpdfPages, HpdfEmbeddedFile, HpdfNamedDestination | ✅ Complete | `HpdfEmbeddedFile`, `HpdfNamedDestination` new (2026-11-27) |
| api/threed/* | ✅ Complete | New (2026-11-27), 11 pages: `Hpdf3DModel`, `Hpdf3DView`, `Hpdf3DNode`, `Hpdf3DMeasure`, `Hpdf3DCommentNote`, `Hpdf3DPerpendicularMeasure`, `Hpdf3DFormat`, `Hpdf3DLighting`, `Hpdf3DActivation`, `Hpdf3DDeactivation`, `Hpdf3DArtworkState` |
| api/annotations/* | ✅ Complete | 35 pages: `HpdfAnnotation`, link and text annotations, annotation enums; markup annotations (2026-11-27): `HpdfMarkupAnnotation`, `HpdfGeometricAnnotation`, `HpdfShapeAnnotation`, `HpdfLineAnnotation`, `HpdfTextMarkupAnnotation`, `HpdfFreeTextAnnotation`, `HpdfStampAnnotation`, `HpdfPopupAnnotation`, `HpdfAnnotationFlags`, `HpdfLineEndingStyle`, `HpdfLineCaptionPosition`, `HpdfLineIntent`, `HpdfFreeTextIntent`, `HpdfStampName`; second batch (2026-11-27): `HpdfPolygonalAnnotation`, `HpdfPolygonAnnotation`, `HpdfPolyLineAnnotation`, `HpdfInkAnnotation`, `HpdfCaretAnnotation`, `HpdfRedactAnnotation`, `HpdfFileAttachmentAnnotation`, `Hpdf3DAnnotation`, `HpdfProjectionAnnotation`, `HpdfPolygonIntent`, `HpdfPolyLineIntent`, `HpdfCaretSymbol`, `HpdfFileAttachmentIcon`, `HpdfReplyType` |

## Phase 3: Comprehensive Reference

Not started: remaining public classes and enums, examples folder, migration guide from the C library,
troubleshooting, performance.

---

## Library Issues Found While Documenting

Fixed in commits `8a97b74`, `e800e62` and `08c7f79` (2026-11-26): arcs continuing a path, outline counts, one-shot setters, version requirements
for masks and page labels, compression mode on existing pages, operator validation (GSave/GRestore, line cap/join,
rendering mode, dash pattern), text without a font, TextRect horizontal scaling, CID word spacing, JPEG stream
position, raw image validation, soft mask rules, duplicate resource names, widget/radio/choice/signature/form rules,
color setters, culture-invariant ToString, ImageDemo; password rules, destinations with a view, annotation argument
checks, PDF/A annotation print flags, forward-only PNG streams; in `08c7f79`: page tree order of `Pages`, `AddKid`
cycles, unbalanced `GSave` at save, UTC dates, page label entry for index 0, `SetXYZ`/ExtGState/page label
validation, `SetNames`, `TextOut`, reserved prefix of generated resource names, radio option states.

Markup annotations (2026-11-27, commits `b16107b` to `692042b`): the public API changed. New types
`HpdfMarkupAnnotation` (base of `HpdfTextAnnotation` now), `HpdfGeometricAnnotation`, `HpdfShapeAnnotation`,
`HpdfLineAnnotation`, `HpdfTextMarkupAnnotation`, `HpdfFreeTextAnnotation`, `HpdfStampAnnotation`,
`HpdfPopupAnnotation`, `HpdfAnnotationFlags` and the enums `HpdfLineEndingStyle`, `HpdfLineCaptionPosition`,
`HpdfLineIntent`, `HpdfFreeTextIntent`, `HpdfStampName`; new members `HpdfAnnotation.Flags`, `SetGrayColor`,
`SetNoColor`, `HpdfPage.Annotations` and the `HpdfPage.Create...Annotation` methods of the new types. Documented in
the 14 new pages, `HpdfAnnotation.md`, `HpdfTextAnnotation.md`, `HpdfAnnotationType.md`, `HpdfLinkAnnotation.md`,
`HpdfBorderStyle.md`, `api/core/HpdfPage.md`, `api/core/HpdfDocument.md` (versions, PDF/A),
`api/forms/HpdfWidgetAnnotation.md`, `api/types/HpdfPoint.md`, `HpdfRect.md`, `HpdfTextAlignment.md`,
`ReservedTypes.md` (the C enums of flags, intents, line endings, caption positions and stamp names have equivalents
now), `HpdfCompressionMode.md`, `HpdfPageGraphics.md`, `guides/AnnotationsGuide.md`, `INDEX.md` and `STRUCTURE.md`;
every example written or changed was compiled and the full programs were run.

More annotations, attachments and 3D (2026-11-27, commits `03a055f` to `fff2ed2`; `f35765e` before them updated its
pages itself): the public API changed. New types `HpdfPolygonalAnnotation`, `HpdfPolygonAnnotation`,
`HpdfPolyLineAnnotation`, `HpdfInkAnnotation`, `HpdfCaretAnnotation`, `HpdfRedactAnnotation`,
`HpdfFileAttachmentAnnotation`, `Hpdf3DAnnotation`, `HpdfProjectionAnnotation`, the enums `HpdfPolygonIntent`,
`HpdfPolyLineIntent`, `HpdfCaretSymbol`, `HpdfFileAttachmentIcon`, `HpdfReplyType` (`Haru.Annotations`),
`HpdfEmbeddedFile` (`Haru.Doc`) and the new namespace `Haru.ThreeD` (`Hpdf3DModel`, `Hpdf3DView`, `Hpdf3DNode`,
`Hpdf3DMeasure`, `Hpdf3DCommentNote`, `Hpdf3DPerpendicularMeasure`, `Hpdf3DFormat`, `Hpdf3DLighting`,
`Hpdf3DActivation`, `Hpdf3DDeactivation`, `Hpdf3DArtworkState`); `HpdfAnnotationType` got `Polygon`, `PolyLine`,
`Caret` and `Redact`; new members `HpdfMarkupAnnotation.SetInReplyTo`, `InReplyTo`, `ReplyType`, `SetRichText`,
`Set3DView`, `HpdfFreeTextAnnotation.SetCloudEffect`, `HpdfDocument.AttachFile` (2), `AttachedFiles`, `Load3DModel` (2),
`HpdfXref.RequiredAdobeExtensionLevel` and the `HpdfPage.Create...Annotation` methods of the new types (file
attachment: 2). Behavior: the `/DA` font of free text and redaction annotations goes to the `/DR` of an existing
AcroForm; PDF/A-1 saving fails for the annotation types it does not allow and for embedded files; PRC models, 3D
measurements and projection annotations declare Adobe ExtensionLevel 3. `HpdfPoint3D` and `Hpdf3DMatrix` are used by
the API now. Documented in the 27 new pages (14 in `api/annotations`, `api/core/HpdfEmbeddedFile.md`, 11 in the new
folder `api/threed`, `guides/ThreeDGuide.md`), `HpdfMarkupAnnotation.md` (members, versions, PDF/A),
`HpdfFreeTextAnnotation.md` (cloudy border, `/DR`), `HpdfAnnotation.md`, `HpdfAnnotationType.md`,
`HpdfGeometricAnnotation.md`, `HpdfBorderStyle.md`, `HpdfPopupAnnotation.md`, `HpdfLineEndingStyle.md`, the inherited
member rows of the other markup annotation pages, `api/core/HpdfPage.md`, `HpdfDocument.md`, `HpdfCatalog.md`
(name dictionary, attachments panel), `api/forms/HpdfAcroForm.md`, `api/types/HpdfPoint.md`, `HpdfRect.md`,
`HpdfTextAlignment.md`, `Hpdf3DMatrix.md`, `HpdfPoint3D.md`, `ReservedTypes.md`, `guides/AnnotationsGuide.md`,
`INDEX.md`, `STRUCTURE.md` (namespaces) and `docs/_tools/build_navigation.py` (section "3D Models"); every example
written or changed was compiled and the full programs were run.

3D checked in Adobe Acrobat (2026-11-27, commit `8a16e2a`): `HpdfPage.CreateProjectionAnnotation(text, measure)` lost
its rectangle (always `/Rect [0 0 0 0]`, Acrobat crashes on a projection annotation with an area),
`Hpdf3DMeasure.SetColor` writes `/C [r g b]` without a color space name; views without a camera use the model's camera
(a PRC model usually has none and shows nothing), `Artwork` lighting of a model without lights is black. Documented in
`HpdfProjectionAnnotation.md`, `HpdfAnnotation.md`, `HpdfMarkupAnnotation.md`, `api/core/HpdfPage.md`, `HpdfRect.md`,
the `api/threed` pages (`Hpdf3DMeasure`, `Hpdf3DView`, `Hpdf3DModel`, `Hpdf3DLighting`, `Hpdf3DCommentNote`,
`Hpdf3DPerpendicularMeasure`, `Hpdf3DNode`; the PRC examples give their views a camera or say they need one),
`guides/AnnotationsGuide.md` and `guides/ThreeDGuide.md` (camera, lighting, differences from the C library,
pitfalls); the changed examples compile.

Name dictionary (2026-11-27, commits `83efdfc` to `4d2e7cb`): the public API changed. New type
`HpdfNamedDestination` (`Haru.Doc`); new members `HpdfDocument.CreateNamedDestination`, `NamedDestinations`,
`AddJavaScript`, `HpdfPage.CreateLinkAnnotation(HpdfRect, HpdfNamedDestination)`, `CreateJavaScriptLinkAnnotation`,
`HpdfOutline.SetDestination(HpdfNamedDestination)`. Behavior: the `Dests` and `JavaScript` name trees are written next
to `EmbeddedFiles` (a tree without entries is not written, so an entry of the user's own name dictionary is kept);
JavaScript needs PDF 1.3 and fails PDF/A-1 saving; a named destination without a target fails saving. Documented in
the new page `api/core/HpdfNamedDestination.md`, `HpdfDocument.md`, `HpdfPage.md`, `HpdfOutline.md`,
`HpdfDestination.md`, `HpdfCatalog.md` (name trees), `api/annotations/HpdfLinkAnnotation.md`,
`guides/OutlinesGuide.md` ("Named Destinations"), `guides/AnnotationsGuide.md` ("Links That Run JavaScript", versions,
PDF/A), `INDEX.md` (also the outdated status line) and `STRUCTURE.md`; every example written or changed was compiled
and run.

Small gaps (2026-11-27): `HpdfImage.Interpolate` (new property, PDF/A-1 saving fails with it) documented in
`api/core/HpdfImage.md`, `HpdfDocument.md` (PDF/A) and `guides/ImagesGuide.md` ("Smoothing enlarged images"); `HpdfDocument.SetOpenJavaScript` (new method; the open action
is a destination or a script) in `HpdfDocument.md`, `HpdfCatalog.md`, `guides/AnnotationsGuide.md` and
`guides/OutlinesGuide.md`; `HpdfDocument.SetPagesConfiguration` (new method) in `HpdfDocument.md`; `HpdfFont.GetUnicodeWidth` (new, also a
default member of `IHpdfFontImplementation`) in `HpdfFont.md` and `STRUCTURE.md`; `HpdfImage.LoadRaw1BitImageFromMem`
(new; CCITT Group 4, also used by `LoadRawImageFromMem` for 1-bit gray images) in `HpdfImage.md` ("Black and White
Images") and `guides/ImagesGuide.md`; the examples compile and run.

ICC color (2026-11-27, commits `763356b` to `81abced`): the public API changed. New type `HpdfIccProfile`; new members
`HpdfDocument.LoadIccProfile` (2), `SRgbProfile`, `IccProfiles`, `SetPdfACompliance(string, HpdfIccProfile, string)`,
`HpdfImage.IccProfile`, `SetIccProfile`, `HpdfPageGraphics.SetIccFill`, `SetIccStroke`, `HpdfGraphicsState
.FillIccProfile`, `FillIccComponents`, `StrokeIccProfile`, `StrokeIccComponents`. Behavior: the PDF/A output intent
carries its ICC profile (it had none); profiles of PNG/JPEG files become the image color space. Documented in the new
page `api/core/HpdfIccProfile.md`, `HpdfDocument.md`, `HpdfImage.md`, `api/extensions/HpdfPageGraphics.md`,
`api/types/HpdfColorSpace.md`, `HpdfCompressionMode.md`, `guides/ImagesGuide.md` ("Color Profiles"),
`guides/GraphicsGuide.md` (Colors), `INDEX.md`, `STRUCTURE.md`; the examples compile.

Still open (the pages describe the current behavior):

- `HpdfCompressionMode.Image` and `Metadata` are compatibility values without effect (images are always compressed;
  PDF/A requires uncompressed XMP metadata).
- `HpdfPage.TextMatrix` does not advance after `ShowText`; `ClosePath` does not move `CurrentPos` back to the start
  of the subpath. Neither affects the output.
- `HpdfGraphicsState.RenderingMode` is an `int`, not `HpdfTextRenderingMode`.
- `HpdfImage.Dispose` releases nothing (the data lives in managed memory); the PNG palette path is unreachable because
  StbImageSharp always decodes to RGBA.
- Types not used by the API: `HpdfDate` (`ToString` is ISO 8601, `FromDateTimeOffset` writes `+` for UTC),
  `HpdfTextWidth`, `HpdfCountry`, `HpdfLang`, and the enums in `ReservedTypes.md`.
  `HpdfRect` and `HpdfBox` are identical types without conversion.
- `HpdfConst` limits and defaults (`DefFontSize`, page size limits) are not enforced; `HpdfConst.HideWindowUi` vs
  `HpdfViewerPreferences.HideWindowUI` spelling.
- The button appearance types (`Haru.Forms.Appearance`) throw `ArgumentException` rather than `HpdfException`.
- Fonts: the issues found while documenting are fixed (2026-11-26, commits `5c7448d` to `25755f7`):
  - Text is measured with the widths of the glyphs it is drawn with, in every font kind; characters that share a
    glyph (non-breaking space, soft hyphen), the `/Differences` of the standard fonts, TrueType `/Widths`.
  - `HpdfCIDFont` writes `/ToUnicode` and `/W` from the glyphs shown when the document is saved; characters above
    U+FFFF (cmap format 12); new public overload `GetGlyphId(Rune)`.
  - TrueType and CID fonts are embedded as subsets (new property `EmbedSubset`), and the `fsType` embedding
    permissions are honored (`TtfCannotEmbeddingFont`).
  - `HpdfCIDFontType0`: widths by CID through the CMap of the encoder (`CIDEncoder.GetCID` / `ReadCID` /
    `Create(name)`, the encoder argument of `Create` is optional), no ToUnicode stub, font definitions without
    the invented widths "CID 1-127 = 500".
  - `FontDataPool`: one cached file for every code page; `LoadFromSharedData(..., codePage)` overloads,
    `DefaultCodePage`.
  - `RegisterFont` / `GetFont`: one parser of encoding names, `InvalidEncodingName` for an unknown name, a
    registered font is found by its name alone.
  - A TrueType font file is read by one class for the simple TrueType font, the CID font and the font data pool
    (`Font/TrueType/TrueTypeFontFile`): the pool parses a file once for every font made from it, and a simple
    TrueType font can be made from a file that has only the cmap subtable for all of Unicode (format 12).
  - The font descriptor of a font made from a TrueType file holds `/FontBBox`, `/Ascent`, `/Descent` and
    `/CapHeight` in 1000-unit glyph space (they were in font units: right for 1000 units per em, twice too large
    for 2048); `/CapHeight` and the x-height (`GetXHeight`, new entry `/XHeight`) are the tops of `H` and `x` as in
    the C library, not `sTypoAscender`.
  - Predefined CJK fonts: all the horizontal CMaps of the C library have encoders (`GB-EUC-H`, `90msp-RKSJ-H`,
    `KSCms-UHC-HW-H` and `KSC-EUC-H` were missing); the CMaps are described in one list (`CMapDefinition`) that
    the encoders and the encoding names are made from.
  - A standard font created with a code page it has no encoding for (437, 65001, a CJK code page; through the
    `HpdfFont` constructor or `GetFont(name, "UTF-8")`) throws `InvalidEncoderType`; it was silently written with
    WinAnsiEncoding. ISO 8859-1 (28591) is written as WinAnsiEncoding on purpose.
  - Vertical writing for the predefined CJK fonts: the seven vertical CMaps of the C library (`GBK-EUC-V`,
    `GB-EUC-V`, `ETen-B5-V`, `90ms-RKSJ-V`, `EUC-V`, `KSCms-UHC-HW-V`, `KSC-EUC-V`); new `HpdfFont.WritingMode`,
    `CIDEncoder.WritingMode`, `IHpdfFontImplementation.WritingMode` (with a default implementation). The unused
    enum `Haru.Font.CID.CIDWritingMode` is removed: `Haru.Types.HpdfWritingMode` is the writing mode.
  - `TextRect` with a font of vertical writing prints columns from the right edge to the left; the alignment works
    along the column. Text is measured with the character spacing as it acts in vertical writing (a negative
    spacing moves the characters apart).
  - Vertical writing for embedded CID fonts: new overload `HpdfCIDFont.LoadFromTrueTypeFile(..., HpdfWritingMode)`
    (Identity-V, the vertical glyph forms of the font's GSUB `vert` feature, `/DW2`), new property
    `HpdfCIDFont.WritingMode`.
  - Vertical writing is documented in `guides/FontsGuide.md` ("Vertical writing"), `api/types/HpdfWritingMode.md`
    (new page), `api/core/HpdfFont.md` and `api/extensions/HpdfPageText.md`, and shown by `VerticalWritingDemo`
    (three pages that tell what has to be seen: open it in several viewers).
  - Line breaks in Chinese and Japanese text (`HpdfLineBreaking`): `MeasureText` with word wrapping and `TextRect`
    break between characters, not before a character that must not begin a line and not after one that must not
    end it; Latin and Korean words stay whole. The C library has the list of such characters for Japanese
    (`JWW_LINE_HEAD_SJIS`), but its check is commented out.
  - Vertical metrics of embedded CID fonts: the advance height of a glyph comes from the font's `vmtx` table (a
    two-em kana repeat mark, the proportional heights of Malgun Gothic), its vertical origin from the outline and
    the top side bearing; glyphs that differ from `/DW2` are written to `/W2`.
  - Fonts by name: a font is registered under its encoding (the CMap of a predefined CJK font, `Identity-H` /
    `Identity-V` of an embedded CID font, else the code page and the writing mode), so `90ms-RKSJ-H`,
    `90msp-RKSJ-H` and `90ms-RKSJ-V` of one font, or an embedded CID font for both writing modes, are registered
    side by side; a code page name finds a horizontal font of that code page. New encoding names `Identity-H` and
    `Identity-V`.
  - New public API of this work is documented in `guides/FontsGuide.md`, `api/core/HpdfFont.md` and
    `api/extensions/HpdfDocumentExtensions.md`.
  - Font collections (.ttc, 2026-11-27): a font of a TrueType collection is loaded by its index, from a file or a
    stream (new `HpdfTrueTypeFont.LoadFromCollection`, `HpdfCIDFont.LoadFromTrueTypeCollection`,
    `FontDataPool.GetOrLoadTrueTypeCollection`, `ParsedTrueTypeData.FontIndex`; `HPDF_LoadTTFontFromFile2` of the
    C library) and embedded as a font file of its own (a subset, or all the tables of the one font), never as the
    collection. A collection given to the loaders of a font file throws `InvalidTtcFile`, as does a font file given
    to the collection loaders; an index outside the collection throws `InvalidTtcIndex`. Documented in
    `guides/FontsGuide.md` ("Font collections (.ttc)", "Embedded CID fonts", "Many documents with the same fonts",
    "Pitfalls") and `api/core/HpdfFont.md` ("Loading Custom Fonts").
  - OpenType fonts with CFF outlines (.otf, .otc; 2026-11-27, not in the C library): a simple font is a Type1 font
    with a Type1C program, a CID font a CIDFontType0 font with a CIDFontType0C program (FontFile3); subsets are
    desubroutinized; vertical origins from VORG. A CID-keyed CFF font as a simple font, CFF2 and fonts without
    outlines (for embedding) throw. Documented in `guides/FontsGuide.md` ("OpenType fonts with CFF outlines (.otf)",
    "Embedding", "Embedded CID fonts", "Pitfalls") and `api/core/HpdfFont.md`; demo `OpenTypeFontDemo`.
- Markup annotations (found while documenting, 2026-11-27): fixed in `204993a` - PDF/A clears the `Invisible`,
  `Hidden` and `NoView` flags, `SetLeader(0)` removes the leader entries, a free text intent other than `Callout`
  removes the callout line, the rectangles of the new `Create...Annotation` methods are checked for NaN and infinity.
  Still open:
  - `HpdfFreeTextAnnotation.SetLineEndingStyle` writes `/LE` also without a callout line (allowed: the entry is
    meaningful only with `/CL`).
  - The text of a sticky note must not be empty; the text of the other markup annotations may be.
- More annotations, attachments and 3D (found while documenting, 2026-11-27): fixed in `5fc785f` -
  `HpdfEmbeddedFile.SetDescription` raises the version to PDF 1.6, `SetCameraMatrix` checks the matrix before it changes
  the view, objects of another document (3D model, poster, 3D annotation) and measurements of another model
  (new `Hpdf3DMeasure.Model`) are rejected, the XML comments name ExtensionLevel 3 to PDF 1.7, `SetVertices` updates
  `/Rect` at once like `SetInkList`. Still open:
  - `HpdfRedactAnnotation.SetOverlayTextRgbColor` has no gray or CMYK variant (free text has all three).
  - `HpdfMarkupAnnotation.ReplyType` is `Reply` also when `InReplyTo` is `null`.
- Fonts, still open:
  - Vertical writing: proportional vertical metrics (GPOS `vpal`) are not applied, and Latin letters stay upright
    (no rotation of Latin runs by the layout; the `vrt2` sideways glyphs are not used on purpose, as in Adobe
    applications and browsers).
  - Line breaking is not the full Unicode algorithm (UAX #14): outside Chinese and Japanese text a line breaks at
    whitespace only (no break after a hyphen, none in Thai text), and Korean breaks at spaces only.

---

[Back to Index](docs/INDEX.md)
