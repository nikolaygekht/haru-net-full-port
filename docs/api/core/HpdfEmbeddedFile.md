# HpdfEmbeddedFile Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfEmbeddedFile` is a file embedded in the PDF: a file specification (`/Type /Filespec`) that names the file and
holds its contents in an embedded file stream (`/Type /EmbeddedFile`). It is a **document attachment**, listed by
viewers in their attachments panel, when it is created by `HpdfDocument.AttachFile()`, and the file of a
[file attachment annotation](../annotations/HpdfFileAttachmentAnnotation.md) when an annotation shows it on a page.

---

## Getting an Instance

The class has no public constructor. Embedded files are created by:

| Method | Creates |
|--------|---------|
| `HpdfDocument.AttachFile(string path)` | A document attachment of a file on disk, named after the file without its directory, without a MIME type |
| `HpdfDocument.AttachFile(string name, byte[] data, string? mimeType = null)` | A document attachment of data in memory |
| `HpdfPage.CreateFileAttachmentAnnotation(HpdfRect rect, string fileName, byte[] data, string text, ...)` | The own file of a file attachment annotation, without a MIME type; reach it through [HpdfFileAttachmentAnnotation.File](../annotations/HpdfFileAttachmentAnnotation.md#property-file) |

What is written:

- The file specification `/F` with the name: characters outside printable ASCII (letters outside ASCII, control
  characters) become `_` in `/F`; spaces are kept. A name with such characters is also written to `/UF` as a text
  string, which raises the document version to PDF 1.7; otherwise the embedded file needs PDF 1.3.
- The embedded file stream, compressed (`/Filter /FlateDecode`), with `/Type /EmbeddedFile`, the MIME type as
  `/Subtype` (for example `/Subtype /text#2Fcsv`) and the parameters `/Params` with `/Size`, the MD5 `/CheckSum` and the
  dates set by [SetCreationDate()](#method-setcreationdate) and [SetModificationDate()](#method-setmodificationdate).
- A document attachment goes into the `EmbeddedFiles` name tree of the catalog's name dictionary when the document is
  saved (PDF 1.4; see [HpdfDocument.AttachFile()](HpdfDocument.md#attachfilestring-byte-string)).

PDF/A-1 allows no embedded files: saving a PDF/A document with a document attachment or a file attachment annotation
throws `HpdfException` (`InvalidOperation`).

---

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
pdf.AddPage();

var data = System.Text.Encoding.UTF8.GetBytes("quarter,revenue\nQ1,100\nQ2,120\n");
var file = pdf.AttachFile("revenue.csv", data, "text/csv");
file.SetDescription("The revenue table of 2026");
file.SetCreationDate(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.FromHours(1)));
file.SetModificationDate(new DateTimeOffset(2026, 11, 27, 17, 30, 0, TimeSpan.FromHours(1)));

Console.WriteLine($"{file.Name}: {file.Size} bytes, {file.MimeType}");

pdf.SaveToFile("attached.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Name](#property-name) | `string` | The file name |
| [MimeType](#property-mimetype) | `string?` | The MIME type, or `null` |
| [Size](#property-size) | `int` | The size of the file in bytes |
| [Dict](#property-dict) | `HpdfDict` | The file specification dictionary |

---

## Methods

| Name | Description |
|------|-------------|
| [SetDescription()](#method-setdescription) | Sets the description shown in the attachments panel (`/Desc`) |
| [SetCreationDate()](#method-setcreationdate) | Sets the date the file was created (`/CreationDate` of `/Params`) |
| [SetModificationDate()](#method-setmodificationdate) | Sets the date the file was last modified (`/ModDate` of `/Params`) |

---

## Detailed Descriptions

#### Property: *Name*

**Declaration:**
```csharp
public string Name { get; }
```

**Description:**
The file name, without a directory, as given or as taken from the path. Viewers show it and use it when the file is
saved. The names of document attachments are unique.

---

#### Property: *MimeType*

**Declaration:**
```csharp
public string? MimeType { get; }
```

**Description:**
The MIME type given to `AttachFile()`, such as `text/plain` or `application/pdf`, or `null`. Viewers may use it to
choose the application that opens the file.

---

#### Property: *Size*

**Declaration:**
```csharp
public int Size { get; }
```

**Description:**
The size of the file in bytes before compression, also written to `/Params /Size`.

---

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The file specification dictionary (`/Type /Filespec`, `/F`, `/UF`, `/EF`, `/Desc`), an indirect object of the
document. It is the value of `/FS` of file attachment annotations and of the entries of the `EmbeddedFiles` name tree.
Use it to add entries the class does not set.

---

#### Method: *SetDescription()*

**Declaration:**
```csharp
public void SetDescription(string description)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| description | `string` | The description; may be empty |

**Return Value:**
`void`

**Description:**
Writes `/Desc` as a PDF text string (any language), which viewers show in the attachments panel, and raises the
document version to PDF 1.6, which introduced the entry. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `description` is `null` |

**Example:**
```csharp
file.SetDescription("Rohdaten der Umfrage");
```

---

#### Method: *SetCreationDate()*

**Declaration:**
```csharp
public void SetCreationDate(DateTimeOffset date)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| date | `DateTimeOffset` | The date the file was created, with its offset from UTC |

**Return Value:**
`void`

**Description:**
Writes `/CreationDate` to the parameters of the embedded file stream as a PDF date string
(`D:20260115090000+01'00'`; an offset of zero is written as `Z`). A later call replaces it.

**Example:**
```csharp
file.SetCreationDate(File.GetCreationTimeUtc("revenue.csv"));
```

---

#### Method: *SetModificationDate()*

**Declaration:**
```csharp
public void SetModificationDate(DateTimeOffset date)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| date | `DateTimeOffset` | The date the file was last modified, with its offset from UTC |

**Return Value:**
`void`

**Description:**
Writes `/ModDate` to the parameters of the embedded file stream, in the format of
[SetCreationDate()](#method-setcreationdate). A later call replaces it.

**Example:**
```csharp
file.SetModificationDate(DateTimeOffset.Now);
```

---

## Related Types

- [HpdfDocument](HpdfDocument.md) - `AttachFile()`, `AttachedFiles`
- [HpdfFileAttachmentAnnotation](../annotations/HpdfFileAttachmentAnnotation.md) - an icon on a page that opens the file
- [HpdfCatalog](HpdfCatalog.md) - the name dictionary that holds the `EmbeddedFiles` tree
- [Annotations Guide](../../guides/AnnotationsGuide.md) - "File Attachments"

---

[Back to Index](../../INDEX.md)
