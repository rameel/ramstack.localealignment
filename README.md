# Ramstack.LocaleAlignment
[![NuGet](https://img.shields.io/nuget/v/Ramstack.LocaleAlignment.svg)](https://nuget.org/packages/Ramstack.LocaleAlignment)
[![MIT](https://img.shields.io/github/license/rameel/ramstack.localealignment)](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE)

Aligns .NET culture settings with the POSIX locale categories (`LANG`, `LC_NUMERIC`, `LC_MONETARY`, `LC_TIME`)
on Linux and other Unix-like systems.

![screenshot](assets/screenshot.png)

The `.NET` column shows the culture settings .NET picks by itself, and the `LocaleAlignment` column shows them after alignment.

## Problem
On Unix-like systems, .NET resolves a single locale the way POSIX resolves `LC_MESSAGES` (`LC_ALL`, then `LC_MESSAGES`, then `LANG`)
and uses it for both `CurrentCulture` and `CurrentUICulture`. As a result:
* `LC_NUMERIC`, `LC_MONETARY`, and `LC_TIME` are ignored
* `LANG` is ignored when `LC_MESSAGES` is set

A common desktop setup, an English interface with the regional formats of another country, therefore gets
US number, currency, and date formats in .NET applications, while other applications use the configured formats.

This is a known limitation, discussed in [dotnet/runtime#110095](https://github.com/dotnet/runtime/issues/110095).

## How it works
At application startup, the locale is resolved as defined by POSIX, separately for each category:
* `LANG` defines the base culture
* `LC_NUMERIC`, `LC_MONETARY`, and `LC_TIME` override the number, currency, and date and time formats
* `LC_ALL` takes precedence over all of them
* An invalid value is ignored as if the variable were not set

Locale names are converted to .NET culture names, e.g., `sr_RS.UTF-8@latin` to `sr-Latn-RS`.

The resulting culture becomes `CurrentCulture` and `DefaultThreadCurrentCulture`.
As with customized regional formats on Windows, it keeps the name of the base culture,
so `en-US` with German formats is still `en-US`.

`CurrentUICulture` is left as .NET resolved it, so the language of localized resources still follows `LC_MESSAGES`.

Nothing changes on Windows or in globalization-invariant mode.

## Packages

### Ramstack.LocaleAlignment
A runtime library that can be used from any .NET language (`C#`, `F#`, `Visual Basic`, etc.).

```bash
dotnet add package Ramstack.LocaleAlignment
```

Call the alignment method as early as possible during application startup, before any culture-sensitive code runs:
```csharp
LocaleAlignment.Apply();
```

### Ramstack.LocaleAlignment.Generator
A source generator that does the same for C# applications without any code changes.

It embeds the alignment code into the application and runs it from a module initializer before `Main`,
so the application gets no runtime dependency.

```bash
dotnet add package Ramstack.LocaleAlignment.Generator
```

## Supported versions
.NET 6 and later.

## Contributions
Bug reports and contributions are welcome.

## License
This package is released as open source under the **MIT License**.<br />
See the [LICENSE](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE) file for more details.
