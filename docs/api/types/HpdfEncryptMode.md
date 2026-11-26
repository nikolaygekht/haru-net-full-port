# HpdfEncryptMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfEncryptMode` selects the revision of the PDF standard security handler used to encrypt a document, which
determines the algorithm and key length. It is passed to `HpdfDocument.SetEncryption`; the value is the revision
number written as `/R` in the encryption dictionary.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `R2` | 2 | Revision 2: RC4 with a 40-bit key. Writes `/V 1 /R 2`. Weakest; use only for compatibility with very old readers. Does not raise the document version. Revision 2 readers ignore the permission flags marked "Revision 3+" in [HpdfPermission](HpdfPermission.md). |
| `R3` | 3 | Revision 3: RC4 with a 128-bit key. Writes `/V 2 /R 3 /Length 128`. Requires **PDF 1.4**: the document version is raised automatically. The default of `SetEncryption`. |
| `R4` | 4 | Revision 4: AES with a 128-bit key (crypt filter `/StdCF` with `/CFM /AESV2` for strings and streams). Writes `/V 4 /R 4 /Length 128`. Requires **PDF 1.6**: the document version is raised automatically. Recommended for new documents. |

There is no value 0 or 1. Values outside this list are not supported (the encryption dictionary would get no `/V`
and `/R` entries) and are not rejected, so do not cast other numbers to this enum.

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.AddPage();

pdf.SetEncryption("user", "owner",
    HpdfPermission.Print | HpdfPermission.Copy,
    HpdfEncryptMode.R4);

Console.WriteLine(pdf.Version);  // Version16
pdf.SaveToFile("encrypted.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfDocument.SetEncryption(string userPassword, string ownerPassword, HpdfPermission permission = HpdfPermission.Print, HpdfEncryptMode mode = HpdfEncryptMode.R3)` | Encrypts the document; the usual way to choose the mode. See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfEncryptDict.SetEncryptionMode(HpdfEncryptMode mode, int keyLength)` | Lower-level: sets the mode and key length (in bytes) of an encryption dictionary. `SetEncryption` calls it with 5 bytes for `R2` and 16 bytes otherwise. |
| `HpdfEncrypt.Mode { get; set; }` | Lower-level (namespace `Haru.Security`): the mode used by the encryption engine. |

## Related Types

- [HpdfPermission](HpdfPermission.md) - what a user who opens the document with the user password may do
- [HpdfDocument](../core/HpdfDocument.md) - `SetEncryption`, `IsEncrypted`, `Version`

---

[Back to Index](../../INDEX.md)
