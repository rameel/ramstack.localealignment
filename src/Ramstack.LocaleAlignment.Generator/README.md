# Ramstack.LocaleAlignment.Generator
[![NuGet](https://img.shields.io/nuget/v/Ramstack.LocaleAlignment.Generator.svg)](https://nuget.org/packages/Ramstack.LocaleAlignment.Generator)
[![MIT](https://img.shields.io/github/license/rameel/ramstack.localealignment)](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE)

A source generator that aligns .NET culture settings with the POSIX locale categories (`LANG`, `LC_NUMERIC`, `LC_MONETARY`, `LC_TIME`)
on Linux and other Unix-like systems, without any code changes.

![screenshot](https://raw.githubusercontent.com/rameel/ramstack.localealignment/d9c9544db7d97e1182733dce22afebfa982382f4/assets/screenshot.png)

The `.NET` column shows the culture settings .NET picks by itself, and the `LocaleAlignment` column shows them after alignment.

## Problem
On Unix-like systems, .NET resolves a single locale the way POSIX resolves `LC_MESSAGES` (`LC_ALL`, then `LC_MESSAGES`, then `LANG`)
and uses it for both `CurrentCulture` and `CurrentUICulture`. As a result:
* `LC_NUMERIC`, `LC_MONETARY`, and `LC_TIME` are ignored
* `LANG` is ignored when `LC_MESSAGES` is set

A common desktop setup, an English interface with the regional formats of another country, therefore gets
US number, currency, and date formats in .NET applications, while other applications use the configured formats.

This is a known limitation, discussed in [dotnet/runtime#110095](https://github.com/dotnet/runtime/issues/110095).

## What this package does
The generator embeds the alignment code into your application and runs it from a module initializer before `Main`,
so the application gets no runtime dependency.

At startup, the locale is resolved as defined by POSIX, separately for each category:
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

## Usage
Add the package to your project:

```bash
dotnet add package Ramstack.LocaleAlignment.Generator
```

`dotnet add package` marks the reference as private, so the generator does not flow to projects that reference yours.
If you add the reference manually, mark it the same way:

```xml
<PackageReference Include="Ramstack.LocaleAlignment.Generator" Version="..." PrivateAssets="all" />
```

No other changes are required.

The initializer is generated only for applications (`OutputType` set to `Exe` or `WinExe`).
Class libraries are left unchanged, so they never override the culture chosen by the application.

The initializer is compiled only for .NET 6 and later,
so applications that also target older frameworks, such as .NET Framework, still build.

> [!NOTE]
> The generator supports C# only.
> For other .NET languages, use [Ramstack.LocaleAlignment](https://www.nuget.org/packages/Ramstack.LocaleAlignment).

## Supported versions
|      | Version        |
|------|----------------|
| .NET | 6, 7, 8, 9, 10 |

## Contributions
Bug reports and contributions are welcome.

## License
This package is released as open source under the **MIT License**.<br />
See the [LICENSE](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE) file for more details.
