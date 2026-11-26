# Haru.NET Documentation Progress Status

**Last Updated:** 2026-11-26
**Overall Progress:** Phase 1 - 75% Complete (9/12 tasks)

---

## Publishing (GitHub Pages)

**Status:** Ready to enable — documentation moved from `cs-src/doc/` to `docs/` on 2026-11-26.

| Item | Status | Notes |
|------|--------|-------|
| `docs/` at repository root | ✅ Done | Moved with `git mv`, history preserved; all internal links are relative and unchanged |
| `docs/_config.yml` | ✅ Done | Theme `jekyll-theme-primer`, `jekyll-relative-links` (`.md` links → `.html`) |
| `docs/_includes/head-custom.html` | ✅ Done | Renders Mermaid diagrams (STRUCTURE.md) on the site |
| Home page | ✅ Done | `INDEX.md` has front matter `permalink: /` |
| Enable Pages in repository settings | ⏳ Pending | Settings → Pages → Deploy from a branch → `main`, `/docs` (requires repository admin) |
| Broken links on the site | ⏳ Pending | Links to directories (`guides/`, `api/`, `api/forms/`) and to not-yet-written pages (pending guides, HpdfImage.md, types, examples) return 404 — write the pages or link to existing ones |

---

## Content Changes Since Last Update (2026-11-26)

Library changes that the documentation must reflect:

| Change | Documented in | Status |
|--------|---------------|--------|
| PDF version: `Version` setter is a minimum, content requirements raise it automatically; transparency requires 1.4 | INDEX.md, api/core/HpdfDocument.md | ✅ Updated |
| Image transparency: PNG alpha → automatic soft mask; `HpdfImage.SetColorMask` (8-bit RGB or gray, validated; a gray image gets the intersection of the three ranges), `SetMaskImage` (stencil, light pixels hide, mask reusable, rejects an image already used as a soft mask), new `SetSoftMask` (grayscale opacity) | INDEX.md (feature list) | 🟡 Partial — needs api/core/HpdfImage.md and guides/ImagesGuide.md |
| Unicode (UTF-8 / Identity-H) for `HpdfCIDFont` | INDEX.md | 🟡 Partial — needs FontsGuide.md section |
| Font implementation class names (`HpdfTrueTypeFont`, `HpdfType1Font`, `HpdfCIDFontType0`) | INDEX.md | ✅ Updated |
| Stale "~95%" / "681 tests" figures | INDEX.md | ✅ Removed |
| New `HpdfPageText.TextRect` (text in a rectangle: wrapping, Left/Right/Center/Justify, returns characters printed) | — | ⏳ Pending — HpdfPageText.md, TextGuide.md |
| New `EoFillStroke` / `ClosePathEoFillStroke` (`B*` / `b*`) and `SetFlat` (`i`) | — | ⏳ Pending — HpdfPageGraphics.md |
| New `HpdfPage.Rotate` (/Rotate, multiples of 90) | — | ⏳ Pending — api/core/HpdfPage.md |
| New `HpdfCatalog.ViewerPreferences` (`HpdfViewerPreferences` flags) and `SetOpenAction` / `GetOpenAction`; PDF version raised for PrintScalingNone (1.6), TwoPage layouts and UseOC (1.5), UseAttachments (1.6) | — | ⏳ Pending — api/core/HpdfDocument.md (catalog section) |
| Text and choice fields get generated appearances (`/AP`) when saved; the form gets `/DR` (fonts named by `/DA`, `ZaDb` for buttons) | — | ⏳ Pending — FormsGuide.md (Phase 2) |
| Breaking: `Haru.Forms.HpdfTextAlignment` renamed to `HpdfFieldAlignment` (`HpdfTextField.SetAlignment`) | — | ⏳ Pending — FormsGuide.md, form field API pages |
| Non-ASCII text (Info, outline titles, field names/values/options) is now written correctly (UTF-16BE text strings); a document can be saved more than once | — | ⏳ Pending — mention in HpdfDocument.md (Save, Info) |

---

## Phase 1: Core Documentation (High Priority)

### ✅ Completed (9/12 tasks)

#### Mandatory Files (4/4) ✅

| File | Status | Location | Description |
|------|--------|----------|-------------|
| **INDEX.md** | ✅ Complete | [docs/INDEX.md](docs/INDEX.md) | Main entry point with library overview, quick start, feature summary, and complete navigation |
| **LICENSE.md** | ✅ Complete | [docs/LICENSE.md](docs/LICENSE.md) | MIT license details, dependency licenses, font licensing responsibilities |
| **USAGE.md** | ✅ Complete | [docs/USAGE.md](docs/USAGE.md) | Quick start guide with installation, basic concepts, common tasks, coordinate system explanation |
| **STRUCTURE.md** | ✅ Complete | [docs/STRUCTURE.md](docs/STRUCTURE.md) | Architecture overview with 8 Mermaid UML diagrams showing class hierarchies, design patterns, and workflows |

#### Essential User Guides (1/5) 🟡

| File | Status | Location | Description |
|------|--------|----------|-------------|
| **GettingStarted.md** | ✅ Complete | [docs/guides/GettingStarted.md](docs/guides/GettingStarted.md) | Step-by-step tutorial with 9 complete examples from first PDF to business cards |
| FontsGuide.md | ⏳ Pending | docs/guides/FontsGuide.md | Complete font guide (Standard, TrueType, Type1, CID/CJK) |
| GraphicsGuide.md | ⏳ Pending | docs/guides/GraphicsGuide.md | Drawing paths, shapes, colors, transformations |
| TextGuide.md | ⏳ Pending | docs/guides/TextGuide.md | Text rendering, positioning, formatting |
| ImagesGuide.md | ⏳ Pending | docs/guides/ImagesGuide.md | Loading and placing PNG/JPEG images |

#### Core API Documentation (3/3) ✅

| File | Status | Location | Description |
|------|--------|----------|-------------|
| **HpdfDocument.md** | ✅ Complete | [docs/api/core/HpdfDocument.md](docs/api/core/HpdfDocument.md) | 600+ lines: Document class with all properties, methods, examples for pages, resources, encryption, metadata, bookmarks, forms, saving |
| **HpdfPage.md** | ✅ Complete | [docs/api/core/HpdfPage.md](docs/api/core/HpdfPage.md) | 800+ lines: Page class with sizing, boxes (Media/Crop/Bleed/Trim/Art), annotations, graphics state, transitions, complete examples |
| **HpdfFont.md** | ✅ Complete | [docs/api/core/HpdfFont.md](docs/api/core/HpdfFont.md) | 700+ lines: Font system with Standard/TrueType/Type1/CID support, text measurement, metrics, all Base14 fonts documented |

#### Extension Methods API Documentation (0/3) 🟡

| File | Status | Location | Description |
|------|--------|----------|-------------|
| HpdfPageGraphics.md | ⏳ Pending | docs/api/extensions/HpdfPageGraphics.md | Low-level graphics operations (paths, colors, line styles, transformations) |
| HpdfPageShapes.md | ⏳ Pending | docs/api/extensions/HpdfPageShapes.md | High-level shape drawing (circles, ellipses, arcs) |
| HpdfPageText.md | ⏳ Pending | docs/api/extensions/HpdfPageText.md | Text operations (BeginText/EndText, positioning, formatting, rendering modes) |

---

## Quality Metrics

### Documentation Created

- **Total Files:** 9 files
- **Total Lines:** ~3,500 lines of documentation
- **Mermaid Diagrams:** 8 comprehensive UML/flow diagrams
- **Code Examples:** 50+ complete, runnable examples
- **Cross-references:** Full navigation between all documents

### Coverage Statistics

| Category | Complete | Pending | Total | Progress |
|----------|----------|---------|-------|----------|
| Mandatory Files | 4 | 0 | 4 | 100% ✅ |
| User Guides | 1 | 4 | 5 | 20% 🟡 |
| Core API Docs | 3 | 0 | 3 | 100% ✅ |
| Extension API Docs | 0 | 3 | 3 | 0% 🔴 |
| **Phase 1 Total** | **9** | **7** | **16** | **56%** 🟡 |

### Documentation Standards Compliance

✅ All documents follow [.claude/DOCUMENTATION.md](.claude/DOCUMENTATION.md) standards:
- English language
- Markdown format
- Mermaid diagrams for architecture
- Consistent structure (Overview → Details → Examples → Related)
- Developer-focused "how to use" approach
- Runnable code examples
- Cross-linking between documents
- No emojis (professional tone)
- Clear headings and navigation

---

## Immediate Next Steps

### Priority 1: Complete Phase 1 Extension Method Docs (3 files)

These are the last remaining Phase 1 tasks to reach 100%:

1. **HpdfPageGraphics.md** (Estimated: 600+ lines)
   - Path operations: MoveTo, LineTo, CurveTo, Rectangle, Circle, Arc
   - Color methods: SetRgbStroke, SetRgbFill, SetCmykStroke, SetCmykFill
   - Line styles: SetLineWidth, SetLineCap, SetLineJoin, SetDash
   - Path painting: Stroke, Fill, FillStroke, Clip
   - Transformations: Concat
   - Graphics state: GSave, GRestore
   - Examples: Various drawing scenarios

2. **HpdfPageShapes.md** (Estimated: 400+ lines)
   - Circle(x, y, radius)
   - Ellipse(x, y, xRadius, yRadius)
   - Arc(x, y, radius, startAngle, endAngle)
   - Examples: Drawing various shapes

3. **HpdfPageText.md** (Estimated: 700+ lines)
   - Text blocks: BeginText, EndText
   - Positioning: MoveTextPos, MoveToNextLine, SetTextMatrix
   - Rendering: ShowText, ShowTextNextLine
   - Formatting: SetFontAndSize, SetTextLeading, SetCharSpace, SetWordSpace
   - Styling: SetTextRenderingMode, SetTextRise, SetHorizontalScaling
   - Examples: Text rendering scenarios

**Estimated Time:** 4-6 hours for all three
**Completion Target:** 100% Phase 1

### Priority 2: User Guides (4 files)

After completing Phase 1 extension methods, create the remaining user guides:

4. **FontsGuide.md**
   - Comprehensive font guide
   - Standard fonts overview
   - Loading TrueType fonts
   - Type1 font usage
   - CJK/CID fonts for international text
   - Font embedding and licensing

5. **GraphicsGuide.md**
   - Drawing primitives (lines, curves, rectangles)
   - Advanced paths
   - Colors (RGB, CMYK)
   - Line styles (width, cap, join, dash patterns)
   - Transformations (translate, rotate, scale)
   - Clipping and masking

6. **TextGuide.md**
   - Text positioning and alignment
   - Multi-line text
   - Text wrapping
   - Leading and spacing
   - Text rendering modes (fill, stroke, clip)
   - International text and encodings

7. **ImagesGuide.md**
   - Loading PNG images
   - Loading JPEG images
   - Loading raw pixel data (`LoadRawImageFromMem`)
   - Image positioning and sizing
   - Aspect ratio preservation
   - Image transformations
   - Transparency: PNG alpha channel, `SetColorMask`, `SetMaskImage` (stencil), `SetSoftMask` (grayscale opacity),
     whole-image opacity via `HpdfExtGState.SetAlphaFill`; one mask per image; PDF 1.4 requirement
   - Performance considerations

---

## Phase 2: Extended Documentation (Medium Priority)

**Status:** Not Started (0/9 tasks)

### Advanced User Guides (4 files)

- FormsGuide.md - Creating interactive PDF forms (AcroForm)
- EncryptionGuide.md - Document security and permissions
- AnnotationsGuide.md - Adding annotations and links
- OutlinesGuide.md - Creating bookmarks/navigation

### Additional API Documentation (5+ files)

- HpdfDocumentExtensions.md
- HpdfPageExtensions.md
- HpdfImage.md (higher priority: masking API changed and `SetSoftMask` was added on 2026-11-26)
- Form field classes (HpdfTextField, HpdfCheckbox, etc.)
- Type documentation (HpdfPageSize, HpdfRgbColor, enums)

---

## Phase 3: Comprehensive Reference (Lower Priority)

**Status:** Not Started

- All remaining public classes and interfaces
- Complete enum documentation
- Advanced examples
- Migration guide (if migrating from C Haru library)
- Troubleshooting guide
- Performance optimization guide

---

## Documentation Structure

Current directory structure:

```
docs/
├── _config.yml                   ✅ GitHub Pages configuration
├── _includes/head-custom.html    ✅ Mermaid rendering
├── INDEX.md                      ✅ Complete
├── LICENSE.md                    ✅ Complete
├── USAGE.md                      ✅ Complete
├── STRUCTURE.md                  ✅ Complete
├── guides/
│   ├── GettingStarted.md         ✅ Complete
│   ├── FontsGuide.md             ⏳ Pending
│   ├── GraphicsGuide.md          ⏳ Pending
│   ├── TextGuide.md              ⏳ Pending
│   └── ImagesGuide.md            ⏳ Pending
├── api/
│   ├── core/
│   │   ├── HpdfDocument.md       ✅ Complete
│   │   ├── HpdfPage.md           ✅ Complete
│   │   └── HpdfFont.md           ✅ Complete
│   ├── extensions/
│   │   ├── HpdfPageGraphics.md   ⏳ Pending
│   │   ├── HpdfPageShapes.md     ⏳ Pending
│   │   ├── HpdfPageText.md       ⏳ Pending
│   │   ├── HpdfPageExtensions.md ⏳ Phase 2
│   │   └── HpdfDocumentExtensions.md ⏳ Phase 2
│   ├── types/                    ⏳ Phase 2
│   ├── forms/                    ⏳ Phase 2
│   └── annotations/              ⏳ Phase 2
└── examples/                     ⏳ Phase 3
```

---

## Key Accomplishments

### Comprehensive API References

All three core API references are complete with:
- Full property documentation with examples
- All methods documented with parameters, returns, and usage
- Multiple complete, runnable examples (30+ examples across the three files)
- Best practices sections
- Common mistake warnings
- Related types documentation
- Cross-references to other documents

### Architecture Documentation

STRUCTURE.md includes 8 detailed Mermaid diagrams:
1. System overview diagram
2. Document object model class hierarchy
3. Extension methods organization
4. Font interface hierarchy
5. PDF object hierarchy
6. Graphics state management
7. Document creation sequence diagram
8. Compression pipeline

### User-Focused Content

All documentation emphasizes:
- "How to use" over "what it is"
- Real-world examples from demo code
- Common use cases and patterns
- Troubleshooting common errors
- Best practices based on library design

---

## Resources and References

### Source Information

- **DOC_PLAN.md** - Original comprehensive documentation plan
- **.claude/DOCUMENTATION.md** - Documentation standards and requirements
- **Demo code** - cs-src/Haru.Demos/*.cs - Source of examples and patterns
- **Source code** - cs-src/Haru/ - API reference source
- **Tests** - cs-src/Haru.Test/ - Usage patterns and edge cases

### Documentation Standards

From .claude/DOCUMENTATION.md:
- Language: English
- Format: Markdown
- Diagrams: Mermaid (for UML, flowcharts, sequences)
- Structure: Overview → Purpose → Examples → Details → Related
- Focus: Developer-facing "how to use" documentation
- Examples: Self-contained, runnable code
- Navigation: Full cross-linking between documents

---

## Recommendations

### To Complete Phase 1 (Get to 100%)

1. **Create HpdfPageGraphics.md** - Most important extension class, covers all drawing operations
2. **Create HpdfPageShapes.md** - Simple high-level shapes, quick to document
3. **Create HpdfPageText.md** - Essential for text rendering, medium complexity

**Estimated effort:** 4-6 hours total
**Blockers:** None - all source code is available
**Dependencies:** None - these can be done in any order

### To Complete Phase 1 User Guides

4. **Create FontsGuide.md** - Leverage existing HpdfFont.md API reference
5. **Create GraphicsGuide.md** - Leverage existing HpdfPageGraphics.md (once created)
6. **Create TextGuide.md** - Leverage existing HpdfPageText.md (once created)
7. **Create ImagesGuide.md** - Simpler guide, can reference demo code

**Estimated effort:** 6-8 hours total
**Recommended order:** Do after extension methods are complete

### Long-term Strategy

- **Phase 1:** Complete ASAP for production-ready core documentation
- **Phase 2:** Tackle as advanced features are needed by users
- **Phase 3:** Create on-demand as questions arise

---

## Success Metrics

### Current Achievement

✅ **Core library documented** - Users can start using Haru.NET immediately
✅ **Main workflow covered** - Create document → Add pages → Add fonts → Draw/Write → Save
✅ **Architecture explained** - Developers understand library design
✅ **Examples provided** - 50+ runnable code samples

### Remaining for Full Phase 1

⏳ **Extension methods** - Need Graphics, Shapes, Text docs for complete drawing operations
⏳ **User guides** - Need Fonts, Graphics, Text, Images guides for comprehensive tutorials

### Phase 1 Completion Criteria

When complete, users will have:
- ✅ Quick start guide (USAGE.md)
- ✅ Step-by-step tutorial (GettingStarted.md)
- 🟡 Complete API reference for core classes (3/6 extension methods done)
- 🟡 Comprehensive user guides (1/5 done)
- ✅ Architecture understanding (STRUCTURE.md)

---

## Contact and Contribution

For questions about documentation:
- See [.claude/DOCUMENTATION.md](.claude/DOCUMENTATION.md) for standards
- See [DOC_PLAN.md](DOC_PLAN.md) for complete documentation plan
- Report documentation issues: https://github.com/nikolaygekht/haru-net-full-port/issues

---

*This status document is automatically updated as documentation progresses.*
*Last session: 2026-11-26*
