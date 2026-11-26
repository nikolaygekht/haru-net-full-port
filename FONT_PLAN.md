# Font Work Plan

Status of the font fixes found while documenting (see `DOC_STATUS.md`, "Library Issues Found While Documenting").
Update this file when a step is done; remove it when the plan is complete and the issues are closed in
`DOC_STATUS.md` and `.claude/CLAUDE.md` ("Known Issues").

Last updated: 2026-11-26 (plan reviewed against the code; steps 2-7 revised; step 1a added after the code review and done)

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

## To Do

Order of work: **2 -> 4 -> 3a -> 3b -> 5 -> 6 -> 7**. Steps 2 and 4 share the save-time mechanism and give the main
effect (CJK text can be copied, CJK PDFs stop being 5-10 MB); step 3 concerns the rarely used non-embedded fonts;
steps 5 and 6 are small and independent. Run the tests and commit after every step, as was done for step 1.

### 2. `HpdfCIDFont`: save-time hook, ToUnicode, one text-to-glyph path

Problems (`Font/CID/HpdfCIDFont.cs`):
- The content holds glyph IDs (Identity-H, CIDToGIDMap Identity), but for the CJK code pages the ToUnicode map
  assumes CID = Unicode, so copied / searched CJK text is garbage. With code page 65001 a glyph shared by two code
  points maps to the first one found in the cmap (U+65E5 extracts as U+2F47).
- Drawing and measuring use different paths: `ConvertTextToGlyphIDs` goes Unicode -> cmap, `MeasureText` for
  CP932 / 936 / 949 / 950 encodes the text to code-page bytes and decodes it back. A character that is not in the
  code page is drawn with its own glyph but measured as `?`.
- `/W` holds the widths of every glyph of the font (tens of thousands of numbers in an uncompressed dictionary).

Plan:
- **Save-time hook for fonts** instead of a special deferred stream object: a font finishes its objects right before
  the document is written, like `HpdfAcroForm.PrepareForWriting` and `HpdfOutline.BeforeWrite`
  (`LoadFromTrueTypeFile` already gets the `HpdfDocument`). Step 4 needs the same hook to replace `FontFile2`,
  `BaseFont` (two dictionaries) and `FontName`, so design it once. Rules for code that runs while saving apply:
  assign entries (`dict[key] = ...`), create objects once, same file for an unchanged document.
- Record the glyphs actually shown: the text-to-glyph method adds `glyphId -> code point` (first use wins, glyph 0
  skipped) to `_usedGlyphs`. Store the code point as `int`, not `ushort`, so that non-BMP support does not require
  redoing this step.
- In the hook: rebuild the ToUnicode stream from `_usedGlyphs` (reuse the stream object), write `/W` only for the
  used glyphs. Handle an empty map (font never used): `CMapGenerator.GenerateToUnicodeCMap` throws on an empty
  mapping.
- One "text -> glyphs" method used by both `ConvertTextToGlyphIDs` and `MeasureText`; remove the code-page
  round trip and `IsMultiByteStart`. Only showing text marks glyphs as used, measuring does not.
- Remove `BuildCIDToUnicodeMapping`, `BuildCmapBasedCIDToUnicodeMapping`, `AddUnicodeRange`.
- `GetGlyphId`: binary search over the cmap segments instead of a linear scan.
- Decide: characters outside the BMP (CJK Extension B, emoji). Today `GetGlyphId(ushort)`, `foreach (char c)` and
  cmap format 4 only: a surrogate pair gives two `.notdef` glyphs. Either add cmap format 12 and iterate runes
  (ToUnicode then needs UTF-16 pairs in `CMapGenerator`), or add "non-BMP characters in CID fonts" to
  "Not Implemented" in `.claude/CLAUDE.md`.
- Tests: PdfPig text extraction of "こんにちは世界" with `noto-jp.ttf` at CP932 and 65001, U+65E5 (日) extracts as
  U+65E5; Chinese / Korean code pages; measured width equals the sum of the drawn glyph widths for a character
  outside the font's code page; a font that was never used; saving twice gives the same file; text added after the
  first save is extractable after the second.

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

### 4. Font subsetting and the `fsType` embedding check

- `TrueTypeSubsetter` exists but is never called, and `BuildSubsetFont` is a stub that returns the whole font:
  TrueType and CID fonts are embedded in full (5-10 MB for the Noto CJK fonts), and the font data is copied into
  the `FontFile2` stream at load time.
- Do it as the C library does (`hpdf_fontdef_tt.c`, `RecreateGLYF` / `RecreateName`): keep glyph IDs unchanged,
  write the `glyf` data of the used glyphs only (unused glyphs get zero length; composite components are kept),
  rebuild `loca`, recompute table checksums and `head.checkSumAdjustment`, prefix the font name with a 6-letter
  subset tag (`ABCDEF+Name`) in `BaseFont` / `FontName`. No glyph renumbering, so content streams and CIDToGIDMap
  stay as they are. Remove the glyph renumbering (`glyphMapping`) from `TrueTypeSubsetter`.
- Tables kept: `glyf`, `head`, `hhea`, `hmtx`, `loca`, `maxp`, `cvt `, `fpgm`, `prep` for CID fonts; plus `cmap`
  (and `name`, `post`, `OS/2` as in C) for simple TrueType fonts, whose glyphs are found through the cmap.
  Everything else (`GSUB`, `GPOS`, `vhea`, ...) is dropped.
- A font without a `glyf` table (CFF / `OTTO`) is embedded in full, not rejected.
- Subsetting runs in the save-time hook of step 2, always from the original font data, so saving is repeatable and
  text added between two saves is covered. The subset tag is derived from the font name and the glyph set
  (deterministic). CID: glyphs from `_usedGlyphs`. Simple TrueType: the glyphs of the 256 code-page characters
  (no usage tracking, deterministic).
- Subsetting is the default, as in C; a per-font switch embeds the whole font.
- `fsType` check with embedding: `0x0002` (restricted license) or `0x0200` (bitmap embedding only) ->
  `HpdfException` `TtfCannotEmbeddingFont` (the error code already exists; the value is already parsed, `FsType`).
  `0x0100` (no subsetting) -> the font is embedded in full. This differs from C, which rejects all three bits.
  The new exception is a breaking change for users of restricted fonts: release notes.
- Tests: subset file is much smaller and still renders / extracts (PdfPig); composite glyphs keep their
  components; a font without `glyf`; restricted-license font throws; no-subsetting font is embedded in full;
  saving twice gives the same file.

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
    `HPDF_INVALID_ENCODING_NAME` -> throw `HpdfException`;
  - the lookup `FontRegistry[fontName]` without `#codePage` in `GetFont` never matches a registered font;
  - the ISO-8859-10..16 formula (`28600 + n % 10`) - check every value against the code pages .NET has;
  - decide whether the parser accepts the CJK encoder names ("90ms-RKSJ-H" -> 932, "GBK-EUC-H" -> 936, ...).
- Tests: every documented encoding name, register / get round trip for each font kind, unknown name throws.

### 7. Finish

- Docs: `guides/FontsGuide.md`, `api/core/HpdfFont.md`, CID font pages (ToUnicode, subsetting and its switch,
  `fsType` error, encoding names), `FontDataPool` page; compile the changed examples.
- `DOC_STATUS.md` (close the font items), `.claude/CLAUDE.md` "Known Issues" and "Not Implemented", release notes
  in `cs-src/nuget/config.xml` (breaking: `fsType` exception, unknown encoding name exception; the TrueType
  `GetCharWidth` change of step 1a is already there).
- Run all tests and demos, commit.
