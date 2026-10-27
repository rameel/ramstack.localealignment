# Ramstack.LocaleAlignment.Generator
[![NuGet](https://img.shields.io/nuget/v/Ramstack.LocaleAlignment.Generator.svg)](https://nuget.org/packages/Ramstack.LocaleAlignment.Generator)
[![MIT](https://img.shields.io/github/license/rameel/ramstack.localealignment)](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE)

Utility that applies POSIX locale user overrides to .NET `CultureInfo` on Unix-like systems.

![screenshot](https://raw.githubusercontent.com/rameel/ramstack.localealignment/c3abd3eb89b4b88965dede9f4a6b1eccb1b74b21/assets/screenshot.png)

## Problem
On Unix-like systems, .NET determines the process culture using only a subset of locale-related environment variables (`LC_ALL`, `LC_MESSAGES`, `LANG`).

Category-specific overrides such as `LC_NUMERIC`, `LC_TIME`, and `LC_MONETARY` **are not applied** during `CultureInfo` initialization,
and `LANG` is ignored when `LC_MESSAGES` is set.

This behavior is documented and discussed in the .NET runtime repository:
* [https://github.com/dotnet/runtime/issues/110095](https://github.com/dotnet/runtime/issues/110095)

## What this package does
`Ramstack.LocaleAlignment.Generator` provides a **source generator** that injects a module initializer into your application.

At startup, it:
* Detects whether the application is running on a non-Windows platform
* Resolves locale categories as defined by POSIX:
    * `LANG` defines the base culture
    * `LC_NUMERIC`, `LC_MONETARY`, and `LC_TIME` override the corresponding formats
    * `LC_ALL` takes precedence over all of them
    * An invalid value is ignored as if the variable were not set
* Updates the current and default thread cultures
* Leaves the UI culture as .NET resolved it from `LC_MESSAGES`
* Respects .NET globalization invariant mode

Has no effect on Windows platforms.

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

No additional configuration or code changes are required.

The generated initializer runs automatically at module load time.

The initializer is generated only for applications (`OutputType` set to `Exe` or `WinExe`).
Class libraries are left unchanged, so they never override the culture chosen by the application.

## Supported versions

|      | Version        |
|------|----------------|
| .NET | 6, 7, 8, 9, 10 |

## Contributions
Bug reports and contributions are welcome.

## License
This package is released as open source under the **MIT License**.<br />
See the [LICENSE](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE) file for more details.
