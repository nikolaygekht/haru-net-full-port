# Haru.NET Documentation Progress Status

**Last Updated:** 2026-11-26
**Overall Progress:** 62 pages. Phase 1: all API pages done, 4 guides pending. Phase 2: forms and types reference
done, 3 guides and 7 core class pages pending.

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
| guides/FontsGuide.md | ⏳ Pending | Standard, TrueType, Type1, CID/CJK fonts; UTF-8 / Identity-H section |
| guides/GraphicsGuide.md | ⏳ Pending | Paths, shapes, colors, transformations, clipping |
| guides/TextGuide.md | ⏳ Pending | Positioning, wrapping with `TextRect`, spacing, rendering modes |
| guides/ImagesGuide.md | ⏳ Pending | Can build on HpdfImage.md |

## Phase 2: Extended Documentation

| File | Status | Notes |
|------|--------|-------|
| guides/FormsGuide.md | ✅ Complete | All field types, `HpdfFieldAlignment`, default appearances and fonts, generated `/AP`, `NeedAppearances`, frames |
| guides/EncryptionGuide.md | ⏳ Pending | |
| guides/AnnotationsGuide.md | ⏳ Pending | |
| guides/OutlinesGuide.md | ⏳ Pending | |
| api/extensions/HpdfDocumentExtensions.md, HpdfPageExtensions.md | ⏳ Pending | Members are summarized on HpdfDocument.md and HpdfPageText.md |
| api/forms/* | ✅ Complete | 17 pages: one per public type of `Haru.Forms` and `Haru.Forms.Appearance` (incl. `HpdfHighlightMode` on the widget page) |
| api/types/* | ✅ Complete | 32 pages: structs, constant classes, page sizes, used enums; `ReservedTypes.md` lists 16 enums kept from the C library that no public member uses |
| api/core: HpdfCatalog, HpdfDestination, HpdfExtGState, HpdfOutline, HpdfInfo, HpdfPageLabel, HpdfPages | ⏳ Pending | Members are summarized on HpdfDocument.md / HpdfPage.md / HpdfPageGraphics.md |

## Phase 3: Comprehensive Reference

Not started: remaining public classes and enums, examples folder, migration guide from the C library,
troubleshooting, performance.

---

## Library Issues Found While Documenting

The pages describe the current behavior. These are open in the code (see also Known Issues in `.claude/CLAUDE.md`):

- `HpdfCompressionMode.Image` and `Metadata` have no effect (images are always compressed); the mode applies only to
  pages added after it is set.
- `SetColorMask` / `SetMaskImage` write `/Mask` (PDF 1.3) without raising the document version.
- `HpdfDocumentExtensions.LoadPngImageFromFile` duplicates the instance method; called directly it reuses a resource
  name (`Im{Count}` without +1).
- `Arc` with equal angles divides by zero; `Arc` always starts a new subpath (`m`), so `MoveTo(center); Arc(...)`
  pie slices miss one radius (ShapesDemo has this pattern).
- `TextRect` / `MeasureText`: horizontal scaling is ignored; word spacing is counted for CID fonts, where viewers
  ignore it; a lone `\r` is not a line break.
- `ShowTextNextLine("")` and `SetSpacingAndShowText(w, c, "")` do nothing (no line move, no spacing change).
- No validation of `...Eof` enum values (`SetTextRenderingMode`, `SetLineCap`, `SetLineJoin`) or of `GRestore` without
  `GSave`; `Concat` does not update `GraphicsState.TransMatrix`.
- `HpdfImage.Dispose` releases nothing; `LoadJpegImage(Stream)` reads from position 0; a JPEG without a supported frame
  header throws `EndOfStreamException`.
- ImageDemo.cs: the "image w/ alpha" cell draws the masked image again.
- One-shot setters (`HpdfDict.Add` on an existing key throws): `HpdfWidgetAnnotation.SetParent` / `SetPage` (a widget
  cannot be moved), `SetBorderStyle`, `SetRgbColor` / `SetCmykColor` (second call), `HpdfPage.SetSlideShow` (second
  call). `SetBorderStyle` with an invalid style leaves a partial `/BS`.
- Forms: `HpdfChoice.SetSelectedValues` removes `/V` before throwing for several values on a single-select field;
  `ClearOptions` keeps the selection; an editable combo box cannot get a value outside the options;
  `HpdfRadioButton.CreateOption` validates the name after creating appearance streams (orphan objects, non-Hpdf
  exceptions) and a duplicate state name leaves the old widget in `/Kids`; inherited `SetValue` / `SetDefaultValue`
  write text strings on signature (`/V`) and button (`/DV`) fields; assigning `Flags` drops `Radio` / `Combo`;
  `HpdfSignature` has no `SetRequired`; a form without fields is written without `/Fields`; `new HpdfAcroForm(xref)`
  is never attached to the catalog; `DefaultAppearance` can return null.
- `SetAppearanceCharacteristics` ignores color arrays whose length is not 3 and does not range-check; rotation turns
  only generated text/choice appearances, not button or frame streams.
- Page labels (PDF 1.3) and encryption R2 (PDF 1.3) do not raise the document version; `HpdfEncryptMode` values
  outside R2..R4 are not rejected.
- `HpdfDashMode`: a fresh graphics state holds `default` (Pattern null), not `Solid`; equality differs.
- `HpdfDate` is not used by the API; `ToString` is ISO 8601, `FromDateTimeOffset` writes `+` for UTC. `HpdfRect` and
  `HpdfBox` are identical types without conversion. Struct `ToString` methods depend on the current culture.
- `HpdfPageSizes.GetSize` throws `IndexOutOfRangeException` for undefined values. Color constructors clamp, property
  setters do not, `SetRgbFill` etc. throw.
- `HpdfConst` limits and defaults (`DefFontSize`, page size limits) are not enforced or differ from real defaults;
  `HpdfConst.HideWindowUi` vs `HpdfViewerPreferences.HideWindowUI` spelling.

---

[Back to Index](docs/INDEX.md)
