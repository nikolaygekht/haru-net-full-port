# HpdfDate Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfDate` holds a date and time with an optional time zone offset, field by field, like `HPDF_Date` of the C
library. It converts to and from `DateTime` and `DateTimeOffset`.

This type is **not used by the public API at present**: no public member takes or returns an `HpdfDate`. Document
dates are set with `DateTime` values: `HpdfInfo.SetCreationDate(DateTime)` and
`HpdfInfo.SetModificationDate(DateTime)` (through `pdf.Info`, see [HpdfDocument](../core/HpdfDocument.md)), which
write the PDF date string `D:YYYYMMDDHHmmSS` plus the local time zone offset themselves.

---

## Getting an Instance

```csharp
using Haru.Types;

var d1 = new HpdfDate(2026, 11, 26, 14, 30, 0);                // no time zone
var d2 = new HpdfDate(2026, 11, 26, 14, 30, 0, '+', 5, 30);     // +05:30
var d3 = HpdfDate.FromDateTime(DateTime.Now);
var d4 = HpdfDate.FromDateTimeOffset(DateTimeOffset.Now);
```

The constructor does not validate the values. `default(HpdfDate)` has all numbers at 0 and `Ind == '\0'`.

---

## Example

```csharp
using Haru.Types;

var date = HpdfDate.FromDateTimeOffset(new DateTimeOffset(2026, 11, 26, 14, 30, 0, TimeSpan.FromHours(-5)));

Console.WriteLine(date);                     // 2026-11-26T14:30:00-05:00
Console.WriteLine(date.ToDateTimeOffset());  // same instant, offset -05:00
Console.WriteLine(date.ToDateTime());        // 2026-11-26 14:30:00 without time zone (printed in the culture's format)
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Year](#property-year) | `int` | Year, for example 2026 |
| [Month](#property-month) | `int` | Month, 1 to 12 |
| [Day](#property-day) | `int` | Day of the month, 1 to 31 |
| [Hour](#property-hour) | `int` | Hour, 0 to 23 |
| [Minutes](#property-minutes) | `int` | Minutes, 0 to 59 |
| [Seconds](#property-seconds) | `int` | Seconds, 0 to 59 |
| [Ind](#property-ind) | `char` | Time zone indicator: `'+'`, `'-'`, `'Z'` or `' '` (none) |
| [OffHour](#property-offhour) | `int` | Time zone offset, hours |
| [OffMinutes](#property-offminutes) | `int` | Time zone offset, minutes |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfDate(int, int, int, int, int, int, char, int, int)](#constructor-hpdfdate) | Creates a date from its fields |
| [FromDateTime(DateTime)](#method-fromdatetime) | Static: creates a date without time zone |
| [FromDateTimeOffset(DateTimeOffset)](#method-fromdatetimeoffset) | Static: creates a date with time zone offset |
| [ToDateTime()](#method-todatetime) | Converts to `DateTime` (offset is dropped) |
| [ToDateTimeOffset()](#method-todatetimeoffset) | Converts to `DateTimeOffset` |
| [Equals()](#method-equals) | Compares all nine fields |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from all fields |
| [ToString()](#method-tostring) | ISO 8601 text form |

---

## Detailed Descriptions

#### Property: *Year*

**Declaration:**
```csharp
public int Year { get; set; }
```

---

#### Property: *Month*

**Declaration:**
```csharp
public int Month { get; set; }
```

---

#### Property: *Day*

**Declaration:**
```csharp
public int Day { get; set; }
```

---

#### Property: *Hour*

**Declaration:**
```csharp
public int Hour { get; set; }
```

---

#### Property: *Minutes*

**Declaration:**
```csharp
public int Minutes { get; set; }
```

---

#### Property: *Seconds*

**Declaration:**
```csharp
public int Seconds { get; set; }
```

---

#### Property: *Ind*

**Declaration:**
```csharp
public char Ind { get; set; }
```

**Description:**
Relationship to UTC: `'+'` (local time is ahead of UTC), `'-'` (behind UTC), `'Z'` (UTC). The constructor's default
`' '` means no time zone information. Only `'-'` changes the sign of the offset in
[ToDateTimeOffset()](#method-todatetimeoffset); every other value is treated as a positive offset.

---

#### Property: *OffHour*

**Declaration:**
```csharp
public int OffHour { get; set; }
```

**Description:**
Hours of the time zone offset (a positive number; the sign is in `Ind`).

---

#### Property: *OffMinutes*

**Declaration:**
```csharp
public int OffMinutes { get; set; }
```

**Description:**
Minutes of the time zone offset (a positive number).

---

#### Constructor: *HpdfDate()*

**Declaration:**
```csharp
public HpdfDate(int year, int month, int day, int hour, int minutes, int seconds,
                char ind = ' ', int offHour = 0, int offMinutes = 0)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| year | `int` | Year |
| month | `int` | Month, 1 to 12 |
| day | `int` | Day, 1 to 31 |
| hour | `int` | Hour, 0 to 23 |
| minutes | `int` | Minutes, 0 to 59 |
| seconds | `int` | Seconds, 0 to 59 |
| ind | `char` | Time zone indicator (default `' '`: none) |
| offHour | `int` | Offset hours (default 0) |
| offMinutes | `int` | Offset minutes (default 0) |

**Description:**
Stores the values as given; there is no range check.

---

#### Method: *FromDateTime()*

**Declaration:**
```csharp
public static HpdfDate FromDateTime(DateTime dateTime)
```

**Return Value:**
A date with the year, month, day, hour, minute and second of `dateTime`, and `Ind = ' '` (no time zone). The
`DateTime.Kind` is ignored and fractions of a second are dropped.

---

#### Method: *FromDateTimeOffset()*

**Declaration:**
```csharp
public static HpdfDate FromDateTimeOffset(DateTimeOffset dateTimeOffset)
```

**Return Value:**
A date with the local date and time of `dateTimeOffset`, `Ind = '+'` for a zero or positive offset and `'-'` for a
negative one, and the absolute offset in `OffHour` and `OffMinutes`. A zero offset gives `'+'`, not `'Z'`.

---

#### Method: *ToDateTime()*

**Declaration:**
```csharp
public DateTime ToDateTime()
```

**Return Value:**
A `DateTime` (`DateTimeKind.Unspecified`) with the stored date and time; the time zone fields are ignored. Returns
`DateTime.MinValue` when the fields do not form a valid date (for example month 13 or `default(HpdfDate)`); no
exception is thrown.

---

#### Method: *ToDateTimeOffset()*

**Declaration:**
```csharp
public DateTimeOffset ToDateTimeOffset()
```

**Return Value:**
A `DateTimeOffset` with the stored date and time and an offset of `OffHour:OffMinutes`, negative when `Ind` is
`'-'`. Returns `DateTimeOffset.MinValue` when the fields or the offset are invalid; no exception is thrown.

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfDate other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all nine fields are equal. The comparison is field by field, so the same instant in two time zones is
not equal.

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfDate left, HpdfDate right)
public static bool operator !=(HpdfDate left, HpdfDate right)
```

**Description:**
Same comparison as `Equals(HpdfDate)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines all nine fields.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
An ISO 8601 string (not the PDF `D:` date format):

| Ind | Result |
|-----|--------|
| `'Z'` | `2026-11-26T14:30:00Z` |
| `'+'` or `'-'` | `2026-11-26T14:30:00+05:30` |
| anything else | `2026-11-26T14:30:00` |

---

[Back to Index](../../INDEX.md)
