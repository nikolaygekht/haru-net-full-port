# Haru.NET Documentation Progress Status

**Last Updated:** 2026-11-26
**Overall Progress:** Phases 1 and 2 complete: 85 pages - every public type has a reference page, 9 guides.
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
| Enable Pages in repository settings | ⏳ Pending | Settings → Pages → Deploy from a branch → `main`, `/docs` (requires repository admin) |

---

## Phase 1: Core Documentation

| File | Status | Notes |
|------|--------|-------|
| INDEX.md, LICENSE.md, USAGE.md, STRUCTURE.md | ✅ Complete | Audited 2026-11-26 |
| guides/GettingStarted.md | ✅ Complete | Audited 2026-11-26 |
| api/core/HpdfDocument.md | ✅ Complete | Catalog (PageLayout, PageMode, ViewerPreferences, SetOpenAction), Info members and Unicode text, repeatable saving, real form API |
| api/core/HpdfPage.md | ✅ Complete | `Rotate`, widget example, `HpdfRect` by edges |
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
| guides/AnnotationsGuide.md | ✅ Complete | Notes, web links, links to pages, borders, highlight modes |
| guides/OutlinesGuide.md | ✅ Complete | Bookmark trees, destinations, open action, page labels |
| api/extensions/HpdfDocumentExtensions.md, HpdfPageExtensions.md | ✅ Complete | |
| api/forms/* | ✅ Complete | 17 pages: one per public type of `Haru.Forms` and `Haru.Forms.Appearance` (incl. `HpdfHighlightMode` on the widget page) |
| api/types/* | ✅ Complete | 32 pages: structs, constant classes, page sizes, used enums; `ReservedTypes.md` lists 16 enums kept from the C library that no public member uses |
| api/core: HpdfCatalog, HpdfDestination, HpdfExtGState, HpdfOutline, HpdfInfo, HpdfPageLabel, HpdfPages | ✅ Complete | |
| api/annotations/* | ✅ Complete | 7 pages: `HpdfAnnotation`, link and text annotations, annotation enums |

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

Still open (the pages describe the current behavior):

- `HpdfCompressionMode.Image` and `Metadata` are compatibility values without effect (images are always compressed;
  PDF/A requires uncompressed XMP metadata).
- `HpdfPage.TextMatrix` does not advance after `ShowText`; `ClosePath` does not move `CurrentPos` back to the start
  of the subpath. Neither affects the output.
- `HpdfGraphicsState.RenderingMode` is an `int`, not `HpdfTextRenderingMode`.
- `HpdfImage.Dispose` releases nothing (the data lives in managed memory); the PNG palette path is unreachable because
  StbImageSharp always decodes to RGBA.
- Types not used by the API: `HpdfDate` (`ToString` is ISO 8601, `FromDateTimeOffset` writes `+` for UTC),
  `HpdfPoint3D`, `Hpdf3DMatrix`, `HpdfTextWidth`, `HpdfCountry`, `HpdfLang`, and the enums in `ReservedTypes.md`.
  `HpdfRect` and `HpdfBox` are identical types without conversion.
- `HpdfConst` limits and defaults (`DefFontSize`, page size limits) are not enforced; `HpdfConst.HideWindowUi` vs
  `HpdfViewerPreferences.HideWindowUI` spelling.
- The button appearance types (`Haru.Forms.Appearance`) throw `ArgumentException` rather than `HpdfException`.
- Fonts (largest open area):
  - `HpdfCIDFont` with code pages 932/936/949/950 writes a ToUnicode map keyed by character IDs while the content
    holds glyph IDs: copied CJK text is garbage. `HpdfCIDFontType0` ToUnicode is a stub (a few characters);
    its widths treat encoded bytes as CIDs. With code page 65001, a glyph shared by two code points maps to one of
    them (e.g. U+65E5 extracts as U+2F47).
  - TrueType and CID fonts are embedded whole (`TrueTypeSubsetter` is never called); the OS/2 `fsType` embedding
    permission is not checked.
  - `FontDataPool.GetOrLoadTrueType` caches by path only, ignoring the code page; `RegisterFont` and `GetFont` parse
    encoding names differently (a font registered as "ISO8859-2" or without an encoding may not be found).


---

[Back to Index](docs/INDEX.md)
