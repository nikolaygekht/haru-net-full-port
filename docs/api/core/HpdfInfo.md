# HpdfInfo Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfInfo` is the document information dictionary (`/Info` in the file trailer): the title, author, subject,
keywords, creating application, producer, dates and any custom entries that viewers show in their document
properties dialog. Text can be in any language.

---

## Getting an Instance

Every document creates its information dictionary in its constructor. Use the `Info` property of the document:

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
HpdfInfo info = pdf.Info;
```

The class also has a public constructor, `HpdfInfo(HpdfXref xref)`, which the document uses internally. Do not call
it yourself: the new dictionary is added to the cross-reference table but the trailer still refers to `pdf.Info`, so
its entries are not shown by viewers. A `null` argument throws `HpdfException` with `HpdfErrorCode.InvalidParameter`.

---

## Example

```csharp
using System;
using Haru.Doc;

var pdf = new HpdfDocument();
pdf.AddPage();

pdf.Info.Title = "Annual Report 2026";
pdf.Info.Author = "Jane Smith";
pdf.Info.Subject = "Financial results";
pdf.Info.Keywords = "report, finance, 2026";
pdf.Info.Creator = "Report Generator 3.1";
pdf.Info.SetCreationDate(DateTime.Now);
pdf.Info.SetCustomMetadata("Department", "Accounting");

pdf.SaveToFile("info.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying information dictionary |
| [Title](#property-title) | `string?` | The document title (`/Title`) |
| [Author](#property-author) | `string?` | The person who created the document (`/Author`) |
| [Subject](#property-subject) | `string?` | The subject (`/Subject`) |
| [Keywords](#property-keywords) | `string?` | Keywords (`/Keywords`) |
| [Creator](#property-creator) | `string?` | The application that created the original content (`/Creator`) |
| [Producer](#property-producer) | `string?` | The application that produced the PDF (`/Producer`) |
| [Trapped](#property-trapped) | `string?` | The trapping state: `"True"`, `"False"` or `"Unknown"` (`/Trapped`) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetCreationDate()](#method-setcreationdate) | Sets `/CreationDate` |
| [SetModificationDate()](#method-setmodificationdate) | Sets `/ModDate` |
| [GetCreationDate()](#method-getcreationdate) | Returns `/CreationDate` as a PDF date string |
| [GetModificationDate()](#method-getmodificationdate) | Returns `/ModDate` as a PDF date string |
| [SetCustomMetadata()](#method-setcustommetadata) | Sets or removes an entry with a key of your choice |
| [GetCustomMetadata()](#method-getcustommetadata) | Returns the text of an entry |

---

## Detailed Descriptions

### Text values

The text properties (`Title`, `Author`, `Subject`, `Keywords`, `Creator`, `Producer`) and custom entries are stored as
PDF text strings: text made only of printable ASCII characters (plus tab, line feed and carriage return) is stored as
it is, and any other text as UTF-16BE with a byte order mark, so viewers show every Unicode character. The getters
decode the stored value back to the original text. Assigning `null` removes the entry; an empty string writes an
empty entry.

```csharp
pdf.Info.Title = "Übersicht / Обзор / 概要";
Console.WriteLine(pdf.Info.Title);   // the same text
pdf.Info.Keywords = null;            // no /Keywords entry
```

---

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The dictionary referred to by the `/Info` entry of the trailer. If the document is encrypted, its strings are
encrypted like all other strings when the document is saved.

---

#### Property: *Title*

**Declaration:**
```csharp
public string? Title { get; set; }
```

**Description:**
The document title. Many viewers show it in the window title bar. `null` when not set.

---

#### Property: *Author*

**Declaration:**
```csharp
public string? Author { get; set; }
```

**Description:**
The name of the person who created the document. `null` when not set.

---

#### Property: *Subject*

**Declaration:**
```csharp
public string? Subject { get; set; }
```

**Description:**
The subject of the document. `null` when not set.

---

#### Property: *Keywords*

**Declaration:**
```csharp
public string? Keywords { get; set; }
```

**Description:**
Keywords associated with the document, as one string (for example separated by commas). `null` when not set.

---

#### Property: *Creator*

**Declaration:**
```csharp
public string? Creator { get; set; }
```

**Description:**
The name of the application that created the original content (for example your application). `null` when not set.

---

#### Property: *Producer*

**Declaration:**
```csharp
public string? Producer { get; set; }
```

**Description:**
The name of the application that produced the PDF. A new document sets it to
`"Haru Free PDF Library (C# Port)"`; you can replace it or remove it with `null`.

---

#### Property: *Trapped*

**Declaration:**
```csharp
public string? Trapped { get; set; }
```

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | The value is not `null`, `"True"`, `"False"` or `"Unknown"` (case-sensitive) |

**Description:**
Whether the document has been modified to include trapping information for printing, written as the name `/True`,
`/False` or `/Unknown`. `null` (the default) removes the entry, which viewers treat as `Unknown`.

**Example:**
```csharp
pdf.Info.Trapped = "False";
```

---

#### Method: *SetCreationDate()*

**Declaration:**
```csharp
public void SetCreationDate(DateTime date)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| date | `DateTime` | The creation date, as local time |

**Return Value:**
`void`

**Description:**
Writes `/CreationDate` as a PDF date string `D:YYYYMMDDHHmmSS` followed by the offset of the computer's local time
zone at that date: `+HH'mm'` or `-HH'mm'`, or `Z` when the offset is zero (for example `D:20261126143045+01'00'`).

The date and time digits are written as they are and the offset is always taken from the local time zone, whatever
the `Kind` of the value. Pass a local time such as `DateTime.Now`; a UTC value (`DateTime.UtcNow`) gets the wrong
offset unless the local time zone is UTC.

A new document has no creation date: it is not set automatically, so that the same input produces the same file.

**Example:**
```csharp
pdf.Info.SetCreationDate(DateTime.Now);
Console.WriteLine(pdf.Info.GetCreationDate());   // for example D:20261126143045+01'00'
```

---

#### Method: *SetModificationDate()*

**Declaration:**
```csharp
public void SetModificationDate(DateTime date)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| date | `DateTime` | The modification date, as local time |

**Return Value:**
`void`

**Description:**
Writes `/ModDate` in the same format as [SetCreationDate()](#method-setcreationdate). It is not set automatically.

---

#### Method: *GetCreationDate()*

**Declaration:**
```csharp
public string? GetCreationDate()
```

**Return Value:**
The `/CreationDate` value as a PDF date string (for example `D:20261126143045+01'00'`), or `null` if it has not been
set.

---

#### Method: *GetModificationDate()*

**Declaration:**
```csharp
public string? GetModificationDate()
```

**Return Value:**
The `/ModDate` value as a PDF date string, or `null` if it has not been set.

---

#### Method: *SetCustomMetadata()*

**Declaration:**
```csharp
public void SetCustomMetadata(string key, string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| key | `string` | The entry name, without the leading slash (for example `"Department"`) |
| value | `string` | The text of the entry; `null` removes the entry |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `key` is `null` or empty |

**Description:**
Sets an entry of the information dictionary with a key of your choice, stored as a text string like the standard
properties. The key is not checked against the standard entries: `SetCustomMetadata("Title", ...)` sets the title,
and a key such as `"CreationDate"` replaces the date with arbitrary text. Use keys of your own for custom data.

**Example:**
```csharp
pdf.Info.SetCustomMetadata("Department", "Accounting");
pdf.Info.SetCustomMetadata("DocumentNumber", "AR-2026-001");
```

---

#### Method: *GetCustomMetadata()*

**Declaration:**
```csharp
public string? GetCustomMetadata(string key)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| key | `string` | The entry name, without the leading slash |

**Return Value:**
The text of the entry, or `null` when `key` is `null` or empty, when there is no such entry, or when the entry is not
a string (for example `"Trapped"`, which is a name).

**Example:**
```csharp
pdf.Info.SetCustomMetadata("Department", "Accounting");
string? department = pdf.Info.GetCustomMetadata("Department");   // "Accounting"
```

---

## Remarks

- **PDF/A.** With `HpdfDocument.SetPdfACompliance(...)`, the XMP metadata stream is generated from the information
  dictionary each time the document is saved, so set the information before saving.
- **Not supported:** reading the dates back as `DateTime`, and XMP metadata outside PDF/A.

---

## Related Types

- [HpdfDocument](HpdfDocument.md) - Owns the information dictionary (`pdf.Info`)
- [HpdfDate](../types/HpdfDate.md) - Date structure of the C API; `HpdfInfo` takes `DateTime` values instead
- [Encryption Guide](../../guides/EncryptionGuide.md)

---

[Back to Index](../../INDEX.md)
