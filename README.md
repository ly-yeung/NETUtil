# NETUtil

Common helpers packaged as **extension methods** for .NET: string conversion and
validation, number formatting, file name and path handling, and small object
utilities. JSON helpers ship as a separate companion package.

- Targets `net8.0` and `netstandard2.0`
- XML documentation and symbol package included (`IncludeSymbols`, `snupkg`)
- `CSharpUtil.Extensions` — core helpers, **no third-party dependency**
- `CSharpUtil.Extensions.Json` — JSON helpers, depends on the core package plus
  Newtonsoft.Json and (on `netstandard2.0`) System.Text.Json
- Current version `0.3.0` for both

## Installation

```bash
dotnet add package CSharpUtil.Extensions          # core: strings, numbers, objects, files
dotnet add package CSharpUtil.Extensions.Json     # only if you need the JSON helpers
```

The two packages share the `CSharpUtil.*` namespaces and member names, so
`using CSharpUtil.StringUtil;` covers both of them — install the JSON package and
`IsValidJsonObject`, `ConvertTo<T>` and friends appear without any code change.

To consume an unreleased build instead, pack locally and point the restore at the
produced `.nupkg` files:

```bash
dotnet pack CSharpUtil/CSharpUtil.csproj -c Release -o artifacts
dotnet pack CSharpUtil.Json/CSharpUtil.Json.csproj -c Release -o artifacts
dotnet add package CSharpUtil.Extensions --source ./artifacts
dotnet add package CSharpUtil.Extensions.Json --source ./artifacts
```

> Repacking the same version and restoring again will silently use the cached copy
> in `%NUGET_PACKAGES%\csharputil.extensions\<version>`. Delete that folder (and
> `csharputil.extensions.json\<version>`) when you need the new build to be picked up.

## Quick start

Every helper is an extension method on `string`, `long`, `decimal` or `object`,
so the `using` directives import the class under which the method lives:

```csharp
using CSharpUtil.StringUtil;      // string helpers
using CSharpUtil.NumberUtil;      // long / decimal helpers
using CSharpUtil.ObjectUtil;      // object helpers
using CSharpUtil.FileUtil;        // path / file name helpers
using CSharpUtil.CollectionUtil;  // null-safe sequence helpers
using CSharpUtil.DateTimeUtil;    // timestamps and day boundaries
using CSharpUtil.GuidUtil;        // compact GUIDs
using CSharpUtil.EnumUtil;        // [Description] lookup

int count = "42".ToInt();                       // 42
string masked = "13800138000".MaskMobile();     // 138****8000
bool isJson = payload.IsValidJsonObject();      // strict, System.Text.Json rules (JSON package)
string size = 1048575L.FormatFileSize();        // "1.00 MB"
string ext = @"C:\docs\Report.PDF".GetExtension();       // ".pdf"
string type = "photo.png".GetContentType();              // "image/png"
string normalized = "  a  b ".NormalizeSpace();          // "a b"
string percent = 0.1234m.ToPercent();                    // "12.3%"
long stamp = DateTime.UtcNow.ToUnixSeconds();            // Unix seconds
string shortId = Guid.NewGuid().ToShortString();         // 22 characters
```

Members marked *JSON package* live in `CSharpUtil.Extensions.Json`; everything else
comes with the core package.

## API overview

### `CSharpUtil.StringUtil`

| Group | Members |
| --- | --- |
| Conversion | `ToInt`, `ToShort`, `ToLong`, `ToDouble`, `ToFloat`, `ToDecimal`, `ToBool`, `ToDateTime`, `ToGuid`, `ToTimeSpan`, `ToEnum<T>` |
| Predicates | `IsNumber`, `IsInteger`, `IsShort`, `IsLong`, `IsFloat`, `IsDouble`, `IsDecimal`, `IsBool`, `IsDateTime`, `IsGuid`, `IsEnum<T>` |
| Deserialization *(JSON package)* | `ConvertTo<T>`, `TryConvertTo<T>` |
| Blank checks | `IsNullOrWhiteSpace`, `IsNullOrEmpty`, `EmptyIfNull` |
| Guards | `EnsureMaxLength` (return or throw), `EnsureNotEmpty` (return or throw) |
| String cleanup | `TrimToNull`, `DefaultIfBlank`, `NormalizeSpace`, `SplitTrim`, `Truncate`, `JoinNonEmpty` |
| Cutting | `CutBefore`, `CutAfter` (string and char overloads) |
| Encoding | `ToUtf8Bytes`, `FromUtf8Bytes`, `ToBase64`, `ToBase64Url`, `FromBase64`, `FromBase64Url` |
| Masking | `Mask`, `MaskMobile`, `MaskIdCard`, `MaskEmail` |
| JSON detection *(JSON package)* | `IsLikeJsonObject`, `IsLikeJsonArray`, `IsValidJsonObject`, `IsValidJsonArray` |

`To*` members come in pairs: a non-nullable overload with an optional default
(`"abc".ToInt(0)` → `0`) and a nullable overload that returns `null` on failure.
The nullable overload has **no default value on purpose** — otherwise
`"1".ToInt()` would be ambiguous between the two and would silently bind to the
nullable one. To get `null` semantics, pass it explicitly:

```csharp
int number = "1".ToInt();          // int, 1
int? maybe = "abc".ToInt((int?)null); // null
```

### `CSharpUtil.NumberUtil`

| Member | Description |
| --- | --- |
| `FormatFileSize(long bytes, int decimals = 2, SizeUnit maxUnit = PB)` | Base-1024 human readable size (`B` / `KB` / `MB` / `GB` / `TB` / `PB`), carries at boundaries, capped by `maxUnit` |
| `ToChineseRmb(decimal amount)` | Chinese uppercase RMB amount (`1234.56m` → `壹仟贰佰叁拾肆元伍角陆分`) |
| `RoundTo(decimal value, int digits, MidpointRounding mode = AwayFromZero)` | Decimal rounding with an explicit midpoint mode |
| `Clamp(value, min, max)` | int / long / decimal / double range clamp; fills the `decimal` gap and the `netstandard2.0` gap that `Math.Clamp` leaves |
| `SafeDivide(value, divisor, fallback = 0m)` | Division that returns the fallback instead of throwing on a zero divisor |
| `ToPercent(decimals = 1)` | Invariant percentage text (`0.1234m` → `"12.3%"`) |

`FormatFileSize` promotes the unit when rounding reaches the next boundary
(`1048575` → `1.00 MB` instead of `1024.00 KB`). Pass a `SizeUnit` to cap it, e.g. for a
table column that must not switch units:

```csharp
1048576L.FormatFileSize();                       // "1.00 MB"
1048576L.FormatFileSize(2, SizeUnit.KB);         // "1024.00 KB"
1073741824L.FormatFileSize(2, SizeUnit.MB);      // "1024.00 MB"
1536L.FormatFileSize(2, SizeUnit.B);             // "1536 B"
```

At the cap the unit is not promoted, so a rounded value may overflow to the next
magnitude (`1048575` bytes with a `KB` cap prints `1024.00 KB`); this is deliberate —
the cap is a display contract, not a rounding rule.

Values below 1 KB, and any call capped at `SizeUnit.B`, print the raw byte count
without decimals (`999 B`), because a fraction of a byte is meaningless. Rounding is
half away from zero at every precision, and the symbol `KB` here means 1024 bytes —
the Windows convention, not the strict SI/IEC meaning of 1000.

### `CSharpUtil.ObjectUtil`

| Member | Description |
| --- | --- |
| `IsNull` / `IsNotNull` | Null checks |
| `CopyTo<T>()` | Cast to `T`; throws on incompatible types, it does not clone |
| `As<T>()` | Same cast returning `null` instead of throwing; same reference, no copy |
| `IsDefault<T>()` | Default-value check (`0`, `false`, `null`, empty struct value) |
| `Coalesce<T>(T fallback)` | Fallback for `null` |

### `CSharpUtil.CollectionUtil`

| Member | Description |
| --- | --- |
| `OrEmpty<T>()` | The sequence itself, or `Enumerable.Empty<T>()` when null; returns the original instance, no wrapper, no copy |
| `IsNullOrEmpty<T>()` | Null-or-no-element check for `IReadOnlyCollection<T>`; only countable collections are accepted so nothing is enumerated twice |

`OrEmpty` exists to delete a null check from the caller, not to hide one:

```csharp
foreach (string item in names.OrEmpty().Where(name => name.Length > 2))
{
    // no separate if (names != null)
}
```

### `CSharpUtil.DateTimeUtil`

| Member | Description |
| --- | --- |
| `ToUnixSeconds()` / `ToUnixMilliseconds()` | Convert to Unix time; `Unspecified` kinds are treated as local, which is what `ToUniversalTime` already assumes |
| `FromUnixSeconds()` / `FromUnixMilliseconds()` | Build a UTC `DateTime`; out-of-range values throw `ArgumentOutOfRangeException` instead of wrapping around |
| `IsUnixTimestamp()` / `ToTimestamp()` | Text form: exactly 10 digits (seconds) or 13 digits (milliseconds), anything else returns `null` |
| `StartOfDay()` / `EndOfDay()` | Midnight, and the last tick of the day (`23:59:59.9999999`) so `BETWEEN` filters include the whole day |
| `IsBetween(min, max)` | Inclusive on both ends |
| `ToVersion(precision)` / `ToDateTime(Version, precision)` | Encode an instant as `Version(year, month, day, unitsOfDay)` and read it back; `VersionPrecision` picks hour, minute, second (default) or millisecond |
| `ToVersionStamp()` | `Ticks` — the value to compare for "is this newer than mine" |

`ToDateTime(Version)` must be given the same `VersionPrecision` that built the value: the fourth
part means "hours since midnight" at hour precision but "milliseconds since midnight" at
millisecond precision, so mixing them silently shifts the result.

`ToTimestamp` deliberately refuses short digit strings: accepting any number would turn `"2024"`
(a year) or an 8-digit legacy id into a plausible-looking date near 1970.

### `CSharpUtil.GuidUtil`

| Member | Description |
| --- | --- |
| `ToShortString()` | 22 URL-safe Base64 characters instead of the 36-character canonical form |
| `ParseShortString()` | Reads it back; wrong length or illegal characters return `null` |

The bytes come from `Guid.ToByteArray`, so the compact form is this library's own encoding —
other tools read it as an opaque token.

### `CSharpUtil.EnumUtil`

| Member | Description |
| --- | --- |
| `GetDescription<T>()` | The `[Description]` text, or the member name when the attribute is missing; cached per enum type, so reflection runs only on the first call per value |

### `CSharpUtil.FileUtil`

String and path level file helpers. Nothing here opens a file, so the whole namespace is safe
to call on untrusted input before a byte is read.

| Type | Members |
| --- | --- |
| `FilePathUtil` | `GetFileName`, `GetFileNameWithoutExtension`, `GetExtension` (with dot), `GetExtensionName` (no dot), `GetCompoundExtension`, `IsExtensionOf`, `IsUnderRoot`, `TryResolveUnderRoot`, `CombineUnderRoot`, `IsPathLengthWithin` |
| `FileNameUtil` | `IsSafeFileName` (plus an overload explaining the failed rule), `IsPortableFileName`, `GetSafeFileName`, `MaxFileNameLength` |
| `FileKindUtil` | `GetFileKind`, `GetFileCategory`, `IsDangerousUploadExtension`, enums `FileKind` / `FileCategory` |
| `FileContentTypeUtil` | `GetContentType` (falls back to `application/octet-stream`), `TryGetContentType`, `DefaultContentType` |
| `FileTypeInfoUtil` | `GetFileInfo` / `TryGetFileInfo` returning `FileTypeInfo` (`Kind`, `Category`, `ContentType`, `IsKnown`, `IsDangerousUploadExtension`) |

Both `\` and `/` split a path on every OS, so `"a\b.txt".GetFileName()` returns `"b.txt"` even
when the process runs on Linux — `Path.GetFileName` would return the whole string there.
Dot files keep their name: `".gitignore".GetFileNameWithoutExtension()` is `".gitignore"`, and
`GetExtension` returns an empty string for both `".gitignore"` and `"notes.txt."`, so a caller
never has to guess which case it received.

An upload pipeline usually wants kind, category, media type and the dangerous flag for the same
name. Call `GetFileInfo` once in that case: it resolves the extension a single time, while
`GetFileKind`, `GetFileCategory`, `GetContentType` and `IsDangerousUploadExtension` each scan the
name again — 216 B versus 88 B per checked name in the allocation probe.

## Behaviour notes

### File type is inferred from the name, not from the bytes

`GetFileKind` and `GetContentType` read the extension only. `"invoice.pdf"` can still be a renamed
executable, so a `FileKind.Pdf` result is a routing hint, not a security check. Content sniffing
needs a stream and is deliberately out of this package. `IsDangerousUploadExtension` is a policy
helper with the same limitation: it flags `.exe`, `.ps1`, `.html`, `.svg` and similar active
content by name.

### Parsing is culture-invariant

All `To*` / `Is*` members call `TryParse` with `CultureInfo.InvariantCulture`
plus an explicit `NumberStyles`, and the `Is*` predicates use exactly the same
rules as their `To*` counterparts. This removes a whole class of silent errors:
`double.TryParse("1.5")` under a `de-DE` thread culture returns `15`, because `.`
is the thousands separator there.

| Constant | Value |
| --- | --- |
| `ParsingCulture` | `CultureInfo.InvariantCulture` |
| `IntegerStyle` | `NumberStyles.Integer` |
| `FloatStyle` | `NumberStyles.Float \| NumberStyles.AllowThousands` |
| `DecimalStyle` | `NumberStyles.Number` |

Trade-off: culture-specific literals are rejected by design. `"1.234,5"` and
`"2026年11月7日"` do not parse — pass a pre-normalised string if you need them.

Non-finite values are excluded everywhere: `"1e400"` overflows to `Infinity`,
so `IsDouble` is `false` and `ToDouble` falls back to the default. The invariant
is `Is*` returns `true` ⇒ the matching `To*` returns the parsed value.

### Timestamps, and Version versus VersionStamp

`ToVersion()` packs an instant into `Version(year, month, day, unitsOfDay)`. `Version.CompareTo`
orders those the same way the instants are ordered, which makes the value usable as a cache key,
build number or audit label. The fourth part counts whole units since midnight, and
`VersionPrecision` chooses the unit:

| Precision | Fourth part | Distinct values per day | Two instants 30 s apart |
| --- | --- | --- | --- |
| `Hour` | hours since midnight | 24 | equal |
| `Minute` | minutes since midnight | 1,440 | equal |
| `Second` *(default)* | seconds since midnight | 86,400 | ordered |
| `Millisecond` | milliseconds since midnight | 86,400,000 | ordered |

Coarser precision buys a smaller range, not more order: anything below the chosen unit collapses
into the same `Version`. Even at millisecond precision this is a sortable label, not a concurrency
token — two writes inside the same millisecond compare as equal. Reading a value back needs the
same precision that produced it, and `ToDateTime` returns `null` when the fourth part is outside
the range the precision allows (24 hours, 1,440 minutes, 86,400 seconds, 86,400,000 milliseconds).

For "is this row newer than mine", use `ToVersionStamp()`: it returns `DateTime.Ticks`, which keeps
100-nanosecond resolution and compares with a single operator. It is still wall clock, so keep the
comparison on one server; two machines whose clocks drift can hand out stamps in the wrong order.

### JSON detection has two layers

| Layer | Members | Cost | Meaning |
| --- | --- | --- | --- |
| Shape | `IsLikeJsonObject`, `IsLikeJsonArray` | ~2 ns, 0 B allocation | First non-whitespace character is `{` / `[`; no parsing |
| Validity | `IsValidJsonObject`, `IsValidJsonArray` | hundreds of ns and up for large payloads | Full token walk with `Utf8JsonReader` |

Shape detection answers "does this look like a JSON object" cheaply and may
return `true` for malformed input such as `{bad json`. Validity detection is
strict and matches `System.Text.Json` exactly: `MaxDepth = 64`,
`CommentHandling.Disallow`, `AllowTrailingCommas = false`, root token must be the
expected type. Single quotes, unquoted keys, trailing commas and `NaN` are all
rejected, so a value accepted here is accepted by `JsonSerializer`.

`IsValidJson*` also has a `ReadOnlySpan<byte>` overload that skips the string
transcoding and reads UTF-8 bytes directly.

All JSON members live in `CSharpUtil.Extensions.Json` (class `JsonStringUtil`, same
namespace) because they need a serializer; the core package therefore has no
third-party dependency at all. The JSON package depends on the core package at the
same version, so installing it is enough to get both.

## Layout

```
CSharpUtil/                     core library sources (net8.0 + netstandard2.0), no dependencies
CSharpUtil.Json/                JSON helpers -> CSharpUtil.Extensions.Json package
CSharpUtil.Tests/               unit tests (xUnit)
CSharpUtil.IntegrationTests/    pack -> restore -> build/run consumer tests
NETUtil.sln                     solution for all four projects
```

## Build and test

```bash
dotnet build NETUtil.sln -c Release          # both TFMs, warnings as-is
dotnet test  NETUtil.sln -c Release          # unit + integration, ~40 s
dotnet pack  CSharpUtil/CSharpUtil.csproj -c Release -o artifacts
dotnet pack  CSharpUtil.Json/CSharpUtil.Json.csproj -c Release -o artifacts
```

The integration project does not reference the libraries by project. It packs the
current sources, restores the resulting packages into throw-away consumer projects
under `%TEMP%`, then compiles (`netstandard2.0`) and runs (`net10.0`) them. It covers
the dependency grouping of both packages, the TFM assets and runtime behaviour — and,
because the core-only consumer compiles without the JSON package and its restore
assets contain no serializer, it also proves the split holds.

## Dependencies

`CSharpUtil.Extensions`

| Package | Applies to |
| --- | --- |
| none | — the package resolves no third-party dependency |
| `Microsoft.SourceLink.GitHub` 8.0.0 | build only (`PrivateAssets="all"`), not a consumer dependency |

`CSharpUtil.Extensions.Json`

| Package | Applies to |
| --- | --- |
| `CSharpUtil.Extensions` 0.3.0 | same version as this package |
| `Newtonsoft.Json` 13.0.4 | all target frameworks (used by `ConvertTo<T>`) |
| `System.Text.Json` 8.0.5 | `netstandard2.0` only (`net8.0` ships its own) |
| `Microsoft.SourceLink.GitHub` 8.0.0 | build only (`PrivateAssets="all"`); maps pdb source paths to the GitHub commit so debuggers can load the original sources |

## License

MIT — see [LICENSE](LICENSE).

```
MIT License

Copyright (c) 2026 Yeung

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, subject to the condition that the above copyright
notice and this permission notice are included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
```

The NuGet package declares the same license via
`PackageLicenseExpression=MIT` in `CSharpUtil.csproj`.
