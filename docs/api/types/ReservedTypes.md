# Reserved Types

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

The enums on this page were carried over from the Haru C library for completeness. At present **no public member of
Haru.NET accepts or returns them**, so passing them anywhere is not possible and setting them has no effect. Several
of them have an equivalent that the API actually uses (often in another namespace); it is named under each enum.
Use the equivalent in new code.

| Enum | Use instead |
|------|-------------|
| [HpdfPdfAType](#hpdfpdfatype) | `HpdfDocument.SetPdfACompliance(string conformance)` |
| [HpdfDestinationType](#hpdfdestinationtype) | The `Set...` methods of `HpdfDestination` |
| [HpdfAnnotType](#hpdfannottype) | `Haru.Annotations.HpdfAnnotationType` |
| [HpdfAnnotFlags](#hpdfannotflags) | `Haru.Annotations.HpdfAnnotationFlags` |
| [HpdfAnnotHighlightMode](#hpdfannothighlightmode) | `Haru.Annotations.HpdfHighlightMode` |
| [HpdfAnnotIcon](#hpdfannoticon) | `Haru.Annotations.HpdfAnnotationIcon` |
| [HpdfAnnotIntent](#hpdfannotintent) | `Haru.Annotations.HpdfLineIntent`, `HpdfFreeTextIntent`, `HpdfPolygonIntent`, `HpdfPolyLineIntent` |
| [HpdfLineAnnotEndingStyle](#hpdflineannotendingstyle) | `Haru.Annotations.HpdfLineEndingStyle` |
| [HpdfLineAnnotCapPosition](#hpdflineannotcapposition) | `Haru.Annotations.HpdfLineCaptionPosition` |
| [HpdfStampAnnotName](#hpdfstampannotname) | `Haru.Annotations.HpdfStampName` |
| [HpdfBSSubtype](#hpdfbssubtype) | `Haru.Annotations.HpdfBorderStyle` |
| [HpdfEncoderType](#hpdfencodertype) | Used: `IHpdfEncoder.Type`, `CIDEncoder.Type` |
| [HpdfByteType](#hpdfbytetype) | Used: `IHpdfEncoder.GetByteType`, `CIDEncoder.GetByteType` |
| [HpdfInfoType](#hpdfinfotype) | The properties and methods of `HpdfInfo` (`pdf.Info`) |
| [HpdfNameDictKey](#hpdfnamedictkey) | `HpdfDocument.AttachFile()` (the `EmbeddedFiles` tree) |

Values named `Eof`, `...Eof` or `DstEof` are sentinels from the C library (the number of values) and are never
valid arguments.

---

## HpdfPdfAType

PDF/A conformance level.

| Name | Value | Description |
|------|-------|-------------|
| `PdfA1A` | 0 | PDF/A-1a (accessible). |
| `PdfA1B` | 1 | PDF/A-1b (basic). |

**Use instead:** `HpdfDocument.SetPdfACompliance(string conformance = "1B")`, which takes the level as a string.
See [HpdfDocument](../core/HpdfDocument.md).

## HpdfDestinationType

The kind of view of a destination (for links, bookmarks and the open action).

| Name | Value | Description |
|------|-------|-------------|
| `Xyz` | 0 | Position (left, top) and zoom factor. |
| `Fit` | 1 | Fit the whole page in the window. |
| `FitH` | 2 | Fit the page width, with a given top. |
| `FitV` | 3 | Fit the page height, with a given left. |
| `FitR` | 4 | Fit a rectangle in the window. |
| `FitB` | 5 | Fit the bounding box of the page contents. |
| `FitBh` | 6 | Fit the width of the bounding box, with a given top. |
| `FitBv` | 7 | Fit the height of the bounding box, with a given left. |
| `DstEof` | 8 | Sentinel; not a valid argument. |

**Use instead:** the methods of `Haru.Doc.HpdfDestination`: `SetXYZ`, `SetFit`, `SetFitH`, `SetFitV`, `SetFitR`,
`SetFitB`, `SetFitBH` and `SetFitBV`.

## HpdfAnnotType

Annotation subtype.

| Name | Value |
|------|-------|
| `TextNotes` | 0 |
| `Link` | 1 |
| `Sound` | 2 |
| `FreeText` | 3 |
| `Stamp` | 4 |
| `Square` | 5 |
| `Circle` | 6 |
| `StrikeOut` | 7 |
| `Highlight` | 8 |
| `Underline` | 9 |
| `Ink` | 10 |
| `FileAttachment` | 11 |
| `Popup` | 12 |
| `ThreeD` | 13 |
| `Squiggly` | 14 |
| `Line` | 15 |
| `Projection` | 16 |
| `Widget` | 17 |

**Use instead:** `Haru.Annotations.HpdfAnnotationType` (returned by `HpdfAnnotation.Type`). It has the same values,
except that the first one is named `Text` instead of `TextNotes`, and four more at the end (`Polygon`, `PolyLine`,
`Caret`, `Redact`).

## HpdfAnnotFlags

Annotation flags.

| Name | Value |
|------|-------|
| `Invisible` | 0 |
| `Hidden` | 1 |
| `Print` | 2 |
| `NoZoom` | 3 |
| `NoRotate` | 4 |
| `NoView` | 5 |
| `ReadOnly` | 6 |

The values are consecutive numbers, not the bit values of the PDF `/F` entry (for example, the PDF value of
`Print` is 4), so they cannot be combined or written directly.

**Use instead:** `Haru.Annotations.HpdfAnnotationFlags` with the
[HpdfAnnotation.Flags](../annotations/HpdfAnnotation.md#property-flags) property. Its values are the PDF bit values
and can be combined (`Print | Locked`); it also has `Locked`, `ToggleNoView` and `LockedContents`.
See [HpdfAnnotationFlags](../annotations/HpdfAnnotationFlags.md).

## HpdfAnnotHighlightMode

How a link annotation is highlighted when it is clicked.

| Name | Value |
|------|-------|
| `NoHighlight` | 0 |
| `InvertBox` | 1 |
| `InvertBorder` | 2 |
| `DownAppearance` | 3 |
| `HighlightModeEof` | 4 |

**Use instead:** `Haru.Annotations.HpdfHighlightMode` with `HpdfLinkAnnotation.SetHighlightMode`. Same values,
without the sentinel.

## HpdfAnnotIcon

Icon of a text (note) annotation.

| Name | Value |
|------|-------|
| `Comment` | 0 |
| `Key` | 1 |
| `Note` | 2 |
| `Help` | 3 |
| `NewParagraph` | 4 |
| `Paragraph` | 5 |
| `Insert` | 6 |
| `Eof` | 7 |

**Use instead:** `Haru.Annotations.HpdfAnnotationIcon`, the `icon` parameter of the `HpdfTextAnnotation`
constructor. Same values, without the sentinel.

## HpdfAnnotIntent

Intent of free text, line, polygon and polyline annotations.

| Name | Value |
|------|-------|
| `FreeTextCallout` | 0 |
| `FreeTextTypewriter` | 1 |
| `LineArrow` | 2 |
| `LineDimension` | 3 |
| `PolygonCloud` | 4 |
| `PolylineDimension` | 5 |
| `PolygonDimension` | 6 |

**Use instead:** `Haru.Annotations.HpdfLineIntent` (`Arrow`, `Dimension`) with `HpdfLineAnnotation.SetIntent()`, and
`Haru.Annotations.HpdfFreeTextIntent` (`Plain`, `Callout`, `TypeWriter`) with `HpdfFreeTextAnnotation.SetIntent()`,
`Haru.Annotations.HpdfPolygonIntent` (`Cloud`, `Dimension`) with `HpdfPolygonAnnotation.SetIntent()`, and
`Haru.Annotations.HpdfPolyLineIntent` (`Dimension`) with `HpdfPolyLineAnnotation.SetIntent()`. See
[HpdfLineIntent](../annotations/HpdfLineIntent.md), [HpdfFreeTextIntent](../annotations/HpdfFreeTextIntent.md),
[HpdfPolygonIntent](../annotations/HpdfPolygonIntent.md) and [HpdfPolyLineIntent](../annotations/HpdfPolyLineIntent.md).

## HpdfLineAnnotEndingStyle

Line ending style of line annotations.

| Name | Value |
|------|-------|
| `None` | 0 |
| `Square` | 1 |
| `Circle` | 2 |
| `Diamond` | 3 |
| `OpenArrow` | 4 |
| `ClosedArrow` | 5 |
| `Butt` | 6 |
| `ROpenArrow` | 7 |
| `RClosedArrow` | 8 |
| `Slash` | 9 |

**Use instead:** `Haru.Annotations.HpdfLineEndingStyle` with `HpdfLineAnnotation.SetLineEndingStyle()` and
`HpdfFreeTextAnnotation.SetLineEndingStyle()`. Same names and values. See
[HpdfLineEndingStyle](../annotations/HpdfLineEndingStyle.md).

## HpdfLineAnnotCapPosition

Caption position of line annotations.

| Name | Value |
|------|-------|
| `Inline` | 0 |
| `Top` | 1 |

**Use instead:** `Haru.Annotations.HpdfLineCaptionPosition` with `HpdfLineAnnotation.SetCaption()`. Same names and
values. See [HpdfLineCaptionPosition](../annotations/HpdfLineCaptionPosition.md).

## HpdfStampAnnotName

Predefined icon of a rubber stamp annotation.

| Name | Value |
|------|-------|
| `Approved` | 0 |
| `Experimental` | 1 |
| `NotApproved` | 2 |
| `AsIs` | 3 |
| `Expired` | 4 |
| `NotForPublicRelease` | 5 |
| `Confidential` | 6 |
| `Final` | 7 |
| `Sold` | 8 |
| `Departmental` | 9 |
| `ForComment` | 10 |
| `TopSecret` | 11 |
| `Draft` | 12 |
| `ForPublicRelease` | 13 |

**Use instead:** `Haru.Annotations.HpdfStampName` with `HpdfPage.CreateStampAnnotation()`. Same names and values.
See [HpdfStampName](../annotations/HpdfStampName.md).

## HpdfBSSubtype

Border style of an annotation.

| Name | Value |
|------|-------|
| `Solid` | 0 |
| `Dashed` | 1 |
| `Beveled` | 2 |
| `Inset` | 3 |
| `Underlined` | 4 |

**Use instead:** `Haru.Annotations.HpdfBorderStyle` with `HpdfAnnotation.SetBorderStyle`. Same values.

## HpdfEncoderType

Kind of a text encoder.

| Name | Value |
|------|-------|
| `SingleByte` | 0 |
| `DoubleByte` | 1 |
| `Uninitialized` | 2 |
| `Unknown` | 3 |

**Used by:** `IHpdfEncoder.Type` (`Haru.Encoding`; `SingleByte` for the encoders of simple fonts, such as
`HpdfEncoderFactory.GetEncoder("WinAnsiEncoding")`) and `CIDEncoder.Type` (`Haru.Font.CID`; `DoubleByte`), the port of
`HPDF_Encoder_GetType`. `Uninitialized` and `Unknown` are not returned.

## HpdfByteType

Classification of a byte in a multi-byte encoded string.

| Name | Value |
|------|-------|
| `Single` | 0 |
| `Lead` | 1 |
| `Trial` | 2 |
| `Unknown` | 3 |

`Trial` is the C library's spelling of "trail byte".

**Used by:** `GetByteType(ReadOnlySpan<byte> text, int index)` of `IHpdfEncoder` (every byte is `Single`) and of
`CIDEncoder` (the bytes of the CMap's codes: `Single`, or `Lead` and `Trial` of a two-byte code, read from the start of
the text), the port of `HPDF_Encoder_GetByteType`; `Unknown` for a position outside the text.

```csharp
var encoder = Haru.Font.CID.CIDEncoder.Create("90ms-RKSJ-H");
byte[] text = encoder.EncodeText("aあ");          // 'a' and a hiragana: 1 + 2 bytes
Console.WriteLine(encoder.GetByteType(text, 1));       // Lead
```

## HpdfInfoType

Entry of the document information dictionary.

| Name | Value |
|------|-------|
| `CreationDate` | 0 |
| `ModDate` | 1 |
| `Author` | 2 |
| `Creator` | 3 |
| `Producer` | 4 |
| `Title` | 5 |
| `Subject` | 6 |
| `Keywords` | 7 |
| `Trapped` | 8 |
| `GtsPdfx` | 9 |
| `Eof` | 10 |

**Use instead:** `HpdfInfo` (`pdf.Info`): the properties `Title`, `Author`, `Subject`, `Keywords`, `Creator`,
`Producer` and `Trapped`, the methods `SetCreationDate` / `SetModificationDate`, and `SetCustomMetadata(key, value)`
for other entries such as `GTS_PDFXVersion`. See [HpdfDocument](../core/HpdfDocument.md).

## HpdfNameDictKey

Key of the document name dictionary.

| Name | Value |
|------|-------|
| `EmbeddedFiles` | 0 |
| `Eof` | 1 |

**Use instead:** `HpdfDocument.AttachFile()`, which writes the `EmbeddedFiles` name tree of the catalog's name
dictionary when the document is saved (see [HpdfDocument](../core/HpdfDocument.md#attachfilestring-byte-string) and
[HpdfEmbeddedFile](../core/HpdfEmbeddedFile.md)). The named destinations (`HpdfDocument.CreateNamedDestination()`) and
the document JavaScript (`HpdfDocument.AddJavaScript()`) are name trees written the same way; other name trees can be
added to a name dictionary of your own with `HpdfCatalog.SetNames()`.

---

[Back to Index](../../INDEX.md)
