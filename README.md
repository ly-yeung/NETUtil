# NETUtil

Common helpers packaged as **extension methods** for .NET: string conversion and
validation, JSON detection, number formatting, and small object utilities.

- Targets `net8.0` and `netstandard2.0`
- XML documentation and symbol package included (`IncludeSymbols`, `snupkg`)
- Package id `CSharpUtil.Extensions`, current version `0.1.0`

## Installation

The package is not published to nuget.org yet. Build it locally and consume the
produced `.nupkg`:

```bash
dotnet pack CSharpUtil/CSharpUtil.csproj -c Release -o artifacts
dotnet add package CSharpUtil.Extensions --source ./artifacts
```

> Repacking the same version and restoring again will silently use the cached copy
> in `%NUGET_PACKAGES%\csharputil.extensions\<version>`. Delete that folder when you need the new
> build to be picked up.

## Quick start

Every helper is an extension method on `string`, `long`, `decimal` or `object`,
so the `using` directives import the class under which the method lives:

```csharp
using CSharpUtil.StringUtil;    // string helpers
using CSharpUtil.NumberUtil;    // long / decimal helpers
using CSharpUtil.ObjectUtil;    // object helpers

int count = "42".ToInt();                       // 42
string masked = "13800138000".MaskMobile();     // 138****8000
bool isJson = payload.IsValidJsonObject();      // strict, System.Text.Json rules
string size = 1048575L.FormatFileSize();        // "1.00 MB"
```

## API overview

### `CSharpUtil.StringUtil`

| Group | Members |
| --- | --- |
| Conversion | `ToInt`, `ToShort`, `ToLong`, `ToDouble`, `ToFloat`, `ToDecimal`, `ToBool`, `ToDateTime`, `ToGuid`, `ToTimeSpan`, `ToEnum<T>` |
| Predicates | `IsNumber`, `IsInteger`, `IsShort`, `IsLong`, `IsFloat`, `IsDouble`, `IsDecimal`, `IsBool`, `IsDateTime`, `IsGuid`, `IsEnum<T>` |
| Deserialization | `ConvertTo<T>`, `TryConvertTo<T>` |
| String cleanup | `TrimToNull`, `DefaultIfBlank`, `SplitTrim`, `Truncate` |
| Masking | `Mask`, `MaskMobile`, `MaskIdCard`, `MaskEmail` |
| JSON detection | `IsLikeJsonObject`, `IsLikeJsonArray`, `IsValidJsonObject`, `IsValidJsonArray` |

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
| `FormatFileSize(long bytes, int decimals = 2)` | Base-1024 human readable size (`B` / `KB` / `MB` / `GB` / `TB`), carries at boundaries |
| `ToChineseRmb(decimal amount)` | Chinese uppercase RMB amount (`1234.56m` → `壹仟贰佰叁拾肆元伍角陆分`) |
| `RoundTo(decimal value, int digits, MidpointRounding mode = AwayFromZero)` | Decimal rounding with an explicit midpoint mode |

### `CSharpUtil.ObjectUtil`

| Member | Description |
| --- | --- |
| `IsNull` / `IsNotNull` | Null checks |
| `CopyTo<T>()` | Cast to `T`; throws on incompatible types, it does not clone |
| `As<T>()` | Same cast returning `null` instead of throwing; same reference, no copy |
| `IsDefault<T>()` | Default-value check (`0`, `false`, `null`, empty struct value) |
| `Coalesce<T>(T fallback)` | Fallback for `null` |

## Behaviour notes

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

## Layout

```
CSharpUtil/                     library sources (net8.0 + netstandard2.0)
CSharpUtil.Tests/               unit tests (235 cases)
CSharpUtil.IntegrationTests/    pack -> restore -> build/run consumer tests
NETUtil.sln                     solution for all three projects
```

## Build and test

```bash
dotnet build NETUtil.sln -c Release          # both TFMs, warnings as-is
dotnet test  NETUtil.sln -c Release          # unit + integration, ~25 s
dotnet pack  CSharpUtil/CSharpUtil.csproj -c Release -o artifacts
```

The integration project does not reference the library by project. It packs the
current sources, restores the resulting package into a throwaway consumer project
under `%TEMP%`, then compiles (`netstandard2.0`) and runs (`net10.0`) it, so the
dependency grouping, TFM assets and runtime behaviour of the real package are
covered.

## Dependencies

| Package | Applies to |
| --- | --- |
| `Newtonsoft.Json` 13.0.4 | all target frameworks (used by `ConvertTo<T>`) |
| `System.Text.Json` 8.0.5 | `netstandard2.0` only (`net8.0` ships its own) |

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
