# Font Work Plan

Status of the font fixes found while documenting (see `DOC_STATUS.md`, "Library Issues Found While Documenting").
Update this file when a step is done; remove it when the plan is complete and the issues are closed in
`DOC_STATUS.md` and `.claude/CLAUDE.md` ("Known Issues").

Last updated: 2026-11-26 (plan reviewed against the code; steps 2-7 revised; steps 1 - 6 done)

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
- Characters outside the BMP: a surrogate pair is one character and one glyph (was two `.notdef`); the glyphs come
  with step 4a. `_usedGlyphs` and `CMapGenerator` already take code points.
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

### 4a. Characters outside the Basic Multilingual Plane in `HpdfCIDFont`

- `ParseCmapTable` reads the format 4 subtable (BMP) and, when the font has one, the format 12 subtable for all of
  Unicode (`TrueTypeCmapFormat12`, binary search over its groups); a Microsoft subtable is preferred to a Unicode
  platform one. A font with format 12 only is accepted. `GetGlyphId(Rune)` is public.
- A surrogate pair is one character everywhere: `ConvertTextToGlyphIDs`, `MeasureText`, the width-limited
  `HpdfFontExtensions.MeasureText` (the pair is never split between lines) and the character spacing of `TextRect`
  lines. `CMapGenerator` writes the pair to ToUnicode; `/W` and the subset work on glyph IDs.
- Tests: `HpdfCIDFontTextTests` (U+20BB7 of `noto-jp.ttf` drawn, measured, extracted with its outline in the subset;
  a missing emoji is one `.notdef`; wrapping; a font with format 12 only). `Haru.Test/Font/TestFonts.cs` holds the
  shared font helpers of the font tests.

### 3. `HpdfCIDFontType0` (predefined, non-embedded CJK fonts)

- **3a**: the stub ToUnicode (a few hard-coded characters) is removed: a predefined CMap of an Adobe character
  collection needs none (PdfPig extracts the text of all four languages through the CMap).
- **3b**: widths are taken by CID. `CIDCMapData.cs` (generated) holds the code -> CID ranges of GBK-EUC-H,
  ETen-B5-H, 90ms-RKSJ-H, EUC-H and KSCms-UHC-H from `c-src/hpdf_encoder_cns.c`, `_cnt.c`, `_jp.c`, `_kr.c`
  (`CMAP_ARRAY_...`, normalized to sorted ranges that do not overlap). `CIDEncoder` has `GetCID` (binary search),
  `ReadCID` (one or two bytes, by the lead / trail byte rules of the C encoders) and `Create(name)`;
  `HpdfCIDFontType0` always has an encoder (the argument of `Create` may be omitted; an unknown CMap name without
  an encoder throws `InvalidEncodingName`). The other CMaps of the C library are listed in "Not Implemented".
- **Font definitions** (`Font/CID/Data/*.json`): commit `28af6b7` had added "CID 1-127 = 500" to all eleven fonts
  as a workaround for bytes treated as CIDs. These entries went to `/W`, gave CIDs of the proportional fonts two
  widths and full-width punctuation the width 500. The widths are regenerated from the W arrays of
  `c-src/hpdf_fontdef_cns.c`, `_cnt.c`, `_jp.c`, `_kr.c`. One deviation from C: MingLiU (Adobe-CNS1) had the
  half-width CIDs of the Adobe-GB1 fonts in libharu, which are ideographs in CNS1; it gets CIDs 13648-13742
  (codes 0x20-0x7E of ETen-B5-H) instead.
- `CreateSimSun` no longer registers a reduced, hard-coded definition over the real one.
- Tests: `Haru.Test/Font/HpdfCIDFontType0WidthTests.cs` (widths of half-width, full-width and proportional
  characters of all eleven fonts, the encoders, `/W` without double entries, extraction).

### 5. `FontDataPool` cache key and default code page

- `GetOrLoadTrueType(path, codePage)` cached by path and returned the data of the first load, with its code page,
  for every later code page. The cache entry is the font file (code-page independent);
  `ParsedTrueTypeData.WithCodePage` gives each requested code page its own object over the same bytes, so the
  existing call sequence `GetOrLoadTrueType(path, cp)` -> `LoadFromSharedData(xref, name, data, embedding)` works.
- New overloads `LoadFromSharedData(..., codePage)` of `HpdfTrueTypeFont` and `HpdfType1Font` (Type1 was fixed
  to 1252). One default code page, `FontDataPool.DefaultCodePage` = 1252 (the pool had 437).
- `HpdfTrueTypeFont.LoadFromSharedData` no longer overwrites `BaseFont` after the dictionaries are built.
- Tests: `Haru.Test/Font/FontDataPoolTests.cs`. Docs: "Many documents with the same fonts" in the fonts guide.

### 6. Encoding-name parsing of `RegisterFont` / `GetFont`

- `HpdfDocument.ParseEncodingToCodePage` and `HpdfDocumentExtensions.ParseEncodingToCodePage` were two different
  parsers: a font registered as "ISO8859-2" got code page 0, `GetFont` looked for 28592. Both use
  `Haru.Encoding.HpdfEncodingNames.GetCodePage` now: names of the PDF format, `CPnnnn` / a number, `ISO8859-n`
  (a table instead of the formula; also `ISO-8859-n`), `KOI8-R`, `UTF-8`, the CMap names of the CJK encodings and
  the names .NET knows, in any letter case.
- An unknown name throws `InvalidEncodingName` (it silently became 1252).
- `RegisterFont` without an encoding registers the font under its own code page (1252 if it has none, was 0).
- `GetFont(name)` without an encoding finds a registered font whatever its code page is (the lookup by the bare
  name never matched a key); a standard font without an encoding stays WinAnsi.
- Standard fonts accept `MacRomanEncoding` (code page 10000 was not mapped to the existing encoder).
- Tests: `Haru.Test/Font/HpdfFontRegistryTests.cs`.

## To Do

Remaining: step 7.

Decisions of the owner (2026-11-26): subsetting is the default with a per-font switch; `fsType` 0x0100 embeds the
whole font; an unknown encoding name throws; characters outside the BMP are supported (step 4a).

### 7. Finish

- Docs: `guides/FontsGuide.md`, `api/core/HpdfFont.md`, CID font pages (ToUnicode, subsetting and its switch,
  `fsType` error, encoding names), `FontDataPool` page; compile the changed examples.
- `DOC_STATUS.md` (close the font items), `.claude/CLAUDE.md` "Known Issues" and "Not Implemented", release notes
  in `cs-src/nuget/config.xml` (breaking: `fsType` exception, unknown encoding name exception; the TrueType
  `GetCharWidth` change of step 1a is already there).
- Run all tests and demos, commit.
