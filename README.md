# NETUtil

Common helpers packaged as **extension methods** for .NET: string conversion and
validation, number formatting, file name and path handling, and small object
utilities. JSON helpers ship as a separate companion package.

- Targets `net8.0` and `netstandard2.0`
- XML documentation and symbol package included (`IncludeSymbols`, `snupkg`)
- `CSharpUtil.Extensions` — core helpers, **no third-party dependency**
- `CSharpUtil.Extensions.Json` — JSON helpers, depends on the core package plus
  Newtonsoft.Json and (on `netstandard2.0`) System.Text.Json
- Current version `0.2.0` for both

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
using CSharpUtil.StringUtil;    // string helpers
using CSharpUtil.NumberUtil;    // long / decimal helpers
using CSharpUtil.ObjectUtil;    // object helpers
using CSharpUtil.FileUtil;      // path / file name helpers

int count = "42".ToInt();                       // 42
string masked = "13800138000".MaskMobile();     // 138****8000
bool isJson = payload.IsValidJsonObject();      // strict, System.Text.Json rules (JSON package)
string size = 1048575L.FormatFileSize();        // "1.00 MB"
string ext = @"C:\docs\Report.PDF".GetExtension();       // ".pdf"
string type = "photo.png".GetContentType();              // "image/png"
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
| String cleanup | `TrimToNull`, `DefaultIfBlank`, `SplitTrim`, `Truncate` |
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
| `CSharpUtil.Extensions` 0.2.0 | same version as this package |
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
