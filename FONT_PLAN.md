# Font Work Plan

Status of the font fixes found while documenting (see `DOC_STATUS.md`, "Library Issues Found While Documenting").
Update this file when a step is done; remove it when the plan is complete and the issues are closed in
`DOC_STATUS.md` and `.claude/CLAUDE.md` ("Known Issues").

Last updated: 2026-11-26 (plan reviewed against the code; steps 2-7 revised; steps 1a, 2 and 4 done)

## Done

### 1. Glyph widths of non-ASCII text — commit `5c7448d`

Text was measured as if every byte were a code of the font's built-in encoding, so accented and Cyrillic text had
wrong widths (right-aligned and justified text, `TextRect`, word wrapping).

- Measurement follows the C library: byte -> Unicode (by the font's encoding) -> width of that glyph. Each font
  builds a 256-entry width table once (`_codeWidths` / `CodeWidths`).
- Standard 14 fonts: `HpdfStandardFontWidths.cs` regenerated from `c-src/hpdf_fontdef_base14.c` (code, Unicode,
  width per glyph; lookup by Unicode for text fonts, by built-in code for Symbol and ZapfDingbats).
- TrueType and Type1: byte decoded by the font's code page, width of the glyph / AFM metric for that character.
- Type1 glyph names: `GlyphNames.GetUnicode` falls back to the full list in `HpdfGlyphNames.GetUnicode`
  (incl. `uniXXXX` / `uXXXX`), so CE glyphs such as `lslash`, `rcaron`, `ohungarumlaut` are found and drawn.
- Tests: `Haru.Test/Font/HpdfFontWidthTests.cs`. Checked visually: right-aligned Helvetica, Times CP1250,
  Roboto CP1251 and Type1 CE lines end exactly at the guide.

### 1a. Findings of the code review of step 1 (commit `5c7448d`)

Reviewed and fixed on 2026-11-26. The regenerated width tables match `c-src/hpdf_fontdef_base14.c` (all 14 fonts,
built-in codes unchanged).

- **Characters that share a glyph** (non-breaking space = `space`, soft hyphen = `hyphen`, Greek mu, Omega, Delta,
  ...): `HpdfGlyphNames.SharedGlyphs` lists the code points with one glyph name. Standard fonts add them to the
  width table when it is built (`HpdfStandardFontWidths.BuildTables`); Type1 fonts find the AFM metric by Unicode
  and then by glyph name (`HpdfType1Font.FindMetric`, two dictionaries built once instead of a linear scan), so
  the glyph is also named correctly in `/Differences` (Greek mu in CP1253 was `/uni03BC` and not drawn).
  The soft hyphen in standard fonts was a regression of step 1.
- **`/Differences` shifted glyphs** (`HpdfBasicEncoder.CreateDifferencesArray`): a code without a glyph name was
  skipped inside a run, so the following names moved down one code (seen with CP1251, CP1253, KOI8-R, ISO8859-14).
  Such a code is written as `/.notdef` now, as in the C library.
- **Romanian T with comma below**: U+021A / U+021B added to the glyph names (`Tcommaaccent` / `tcommaaccent`, the
  glyph the fonts share with U+0162 / U+0163), so ISO8859-16 text is drawn and measured in standard fonts.
- **TrueType widths**: `CodeWidths` holds rounded 1000-unit values, `/Widths` is written from it and `MeasureText`
  sums it - measured text equals the advances of a viewer. `GetCharWidth` returns 1000-unit glyph space as the
  interface documents (was font units; breaking, in the release notes). Type1 `/Widths` is written from its
  `CodeWidths` as well.
- **Cleanup**: the end-of-table row `(-1, 0xFFFF, 0)` removed from the standard width data; test fonts disposed.
- Tests: `Haru.Test/Font/HpdfFontWidthTests.cs` (shared glyphs in standard and Type1 fonts, `/Differences` of the
  encoders, widths equal to `/Widths`).

### 2. `HpdfCIDFont`: save-time hook, ToUnicode, one text-to-glyph path

The content holds glyph IDs (Identity-H, CIDToGIDMap Identity), but the ToUnicode map of the CJK code pages assumed
CID = Unicode (copied text was garbage), with 65001 a shared glyph mapped to the first code point of the cmap
(U+65E5 extracted as U+2F47), text was measured through the code page while it was drawn through the cmap, and `/W`
held every glyph of the font.

- **Save-time hook**: `IHpdfBeforeWrite` (`Haru.Xref`), registered with `HpdfXref.RegisterBeforeWrite` and called by
  `HpdfXref.WriteToStream` on every save. Step 4 uses the same hook for `FontFile2`, `BaseFont` and `FontName`.
- `HpdfCIDFont._usedGlyphs` (glyph ID -> code point as `int`, first use wins, glyph 0 skipped) is filled by
  `ConvertTextToGlyphIDs`. `UpdateUsedGlyphObjects` rewrites the ToUnicode stream (same stream object) and assigns
  `/W` (glyph 0 and the glyphs in use) from it - at load, so the objects are always valid, and in the hook; nothing
  is rewritten when no glyph was added. A font that was never used gets an empty CMap (`CMapGenerator` accepts it).
- `CMapGenerator`: code points as `int` (surrogate pairs above U+FFFF), a range stays within one high byte of the
  code and of the Unicode value.
- One text-to-glyph mapping, `GetGlyphId(Rune)`, for drawing and measuring; the code-page round trip and
  `IsMultiByteStart` are gone, the code page only sets `/Lang`. Text is measured with the rounded 1000-unit widths
  written to `/W`. `GetGlyphId` searches the cmap segments by binary search. `GetGlyphWidth` of an invalid glyph ID
  is 0 (was a value in the wrong unit).
- Removed: `BuildCIDToUnicodeMapping`, `BuildCmapBasedCIDToUnicodeMapping`, `AddUnicodeRange`.
- Characters outside the BMP: a surrogate pair is one character and one `.notdef` glyph (was two); real support
  is step 4a. `_usedGlyphs` and `CMapGenerator` already take code points.
- Tests: `Haru.Test/Font/HpdfCIDFontTextTests.cs` (PdfPig extraction for the four CJK fonts and code pages and
  65001, the shared glyph, text added between two saves, saving twice, unused font, `/W`, measuring).

### 4. Font subsetting and the `fsType` embedding check

TrueType and CID fonts were embedded in full (5-10 MB for the Noto CJK fonts; `TrueTypeSubsetter` was a stub that
was never called), and `fsType` was not checked.

- `TrueTypeSubsetter` (rewritten, static, works on the font bytes) does what the C library does
  (`hpdf_fontdef_tt.c`, `RecreateGLYF`): glyph IDs unchanged, `glyf` holds the used glyphs and the components of
  composite glyphs, `loca` rebuilt in the font's format, tables and file checksums recomputed. Tables kept: `glyf`,
  `head`, `hhea`, `hmtx`, `loca`, `maxp`, `cvt `, `fpgm`, `prep` for CID fonts; plus `cmap`, `name`, `post`, `OS/2`
  for simple TrueType fonts. A font without `glyf` / `loca` (CFF) or with damaged tables is embedded in full.
- The font program is written in the save-time hook (`UpdateFontProgram` of `HpdfCIDFont` and `HpdfTrueTypeFont`),
  always from the original file; the font data is no longer copied into the stream at load. CID: the glyphs of
  `_usedGlyphs`; simple TrueType: the glyphs of the characters of the code page. The subset tag (`ABCDEF+Name`,
  FNV-1a of the name and the glyphs) goes to `BaseFont` / `FontName` of the dictionaries.
- `EmbedSubset` (default true) on both font classes embeds the whole font when set to false.
- `TrueTypeEmbeddingRights`: restricted license (the only embedding level set) or bitmap-only embedding ->
  `TtfCannotEmbeddingFont` when a font is loaded for embedding; "no subsetting" -> embedded in full. C rejects on
  any of the three bits.
- Removed: the unused glyph tracking of `HpdfTrueTypeFont`, `TrueTypeParser.ParseLoca` / `ReadGlyphData` and their
  structures.
- Tests: `Haru.Test/Font/HpdfFontSubsetTests.cs` (sizes, names, outlines of the shown glyphs through PdfPig,
  text added between saves, saving twice, `EmbedSubset`, every `fsType` case, a font without `glyf`). Checked with
  MuPDF and fontTools: the demo pages render pixel-identically before and after (CJKDemo.pdf 18.3 MB -> 110 KB),
  the subsets parse as fonts.

## To Do

Order of work: **4a -> 3a -> 3b -> 5 -> 6 -> 7**. Step 3 concerns the rarely used non-embedded fonts; steps 5 and 6
are small and independent. Run the tests and commit after every step, as was done for step 1.

Decisions of the owner (2026-11-26): subsetting is the default with a per-font switch; `fsType` 0x0100 embeds the
whole font; an unknown encoding name throws; characters outside the BMP are supported (step 4a).

### 3. `HpdfCIDFontType0` (predefined, non-embedded CJK fonts)

**3a. ToUnicode stub**
- Remove the stub ToUnicode (a few hard-coded characters): predefined CMaps of the Adobe-GB1 / Japan1 / Korea1 /
  CNS1 collections need no ToUnicode, readers map them themselves.

**3b. Widths**
- `MeasureText` / `GetCharWidth` treat the encoded bytes as CIDs. In practice CJK characters get `DW` = 1000 by
  accident, while ASCII and half-width characters are measured wrong.
- The C# encoders (`GBKEucHEncoder`, `ETenB5HEncoder`, `Ms90RKSJHEncoder`, `EucHEncoder`, `KSCmsUHCHEncoder`) have
  no CMap: they only wrap a .NET code page. Port the code -> CID range tables from the C source by a generator, as
  was done for `HpdfStandardFontWidths`: `CMAP_ARRAY_GBK_EUC_H` (`hpdf_encoder_cns.c`), `CMAP_ARRAY_ETen_B5_H`
  (`hpdf_encoder_cnt.c`), `CMAP_ARRAY_90ms_RKSJ_H`, `CMAP_ARRAY_EUC_H` (`hpdf_encoder_jp.c`),
  `CMAP_ARRAY_KSCms_UHC_H` (`hpdf_encoder_kr.c`), each with its `*_NOTDEF_RANGE` and code space ranges.
- `CIDEncoder` gets a code -> CID lookup (binary search over the ranges, tables loaded once); the font splits the
  encoded bytes by the code space ranges, maps each code to its CID and takes the width of that CID.
- Tests: width of ASCII, half-width and full-width characters in every predefined font; extraction through PdfPig
  if it supports the predefined CMap.

### 4a. Characters outside the Basic Multilingual Plane in `HpdfCIDFont`

- The cmap parser reads format 4 only and `GetGlyphId(Rune)` returns 0 above U+FFFF (CJK Extension B, emoji).
- Parse the format 12 subtable (platform 3 encoding 10, or platform 0 encoding 4 / 6) when the font has one, next
  to format 4; `GetGlyphId(Rune)` looks a code point above U+FFFF up in its groups (binary search).
- `CMapGenerator` already writes surrogate pairs; `/W` and subsetting work on glyph IDs and need no change.
- Remove the entry from "Not Implemented" in `.claude/CLAUDE.md`; the guide's sentence about the BMP.
- Tests: a character of CJK Extension B is drawn with a glyph, measured and extracted (PdfPig) when the font has
  it (`noto-*.ttf`); a font without format 12 keeps drawing `.notdef`.

### 5. `FontDataPool` cache key and default code page

- `FontDataPool.GetOrLoadTrueType(path, codePage = 437)` caches by path only: the second load of the same file with
  another code page returns data created for the first one. The pool is public API only (nothing in the library
  calls it), and `ParsedTrueTypeData` holds bytes, name and code page - nothing in it depends on the code page.
- Keep code-page independent data in the cache and pass the code page to `HpdfTrueTypeFont.LoadFromSharedData`
  (new overload; the old signatures stay as overloads for compatibility, with 1252 as the one default -
  `HpdfTrueTypeFont.LoadFromFile` already uses 1252, the pool used 437).
- Tests: the same file loaded with two code pages gives two correctly encoded fonts.

### 6. Encoding-name parsing of `RegisterFont` / `GetFont`

- `HpdfDocument.ParseEncodingToCodePage` and `HpdfDocumentExtensions.ParseEncodingToCodePage` are two different
  parsers: a font registered as "ISO8859-2" (-> 0) or without an encoding is not found by `GetFont` (-> 28592 /
  1252). One shared parser, one rule for the default encoding.
- Found while reviewing, fix in the same step:
  - an unknown encoding name silently becomes 1252 (`catch` in `HpdfDocumentExtensions`); C reports
    `HPDF_INVALID_ENCODING_NAME` -> throw `HpdfException` (decided);
  - the lookup `FontRegistry[fontName]` without `#codePage` in `GetFont` never matches a registered font;
  - the ISO-8859-10..16 formula (`28600 + n % 10`) - check every value against the code pages .NET has;
  - the parser accepts the CJK encoder names ("90ms-RKSJ-H" -> 932, "GBK-EUC-H" -> 936, ...).
- Tests: every documented encoding name, register / get round trip for each font kind, unknown name throws.

### 7. Finish

- Docs: `guides/FontsGuide.md`, `api/core/HpdfFont.md`, CID font pages (ToUnicode, subsetting and its switch,
  `fsType` error, encoding names), `FontDataPool` page; compile the changed examples.
- `DOC_STATUS.md` (close the font items), `.claude/CLAUDE.md` "Known Issues" and "Not Implemented", release notes
  in `cs-src/nuget/config.xml` (breaking: `fsType` exception, unknown encoding name exception; the TrueType
  `GetCharWidth` change of step 1a is already there).
- Run all tests and demos, commit.
