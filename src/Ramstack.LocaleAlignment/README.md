# Ramstack.LocaleAlignment
[![NuGet](https://img.shields.io/nuget/v/Ramstack.LocaleAlignment.svg)](https://nuget.org/packages/Ramstack.LocaleAlignment)
[![MIT](https://img.shields.io/github/license/rameel/ramstack.localealignment)](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE)

A small runtime helper for applying POSIX locale user overrides to .NET `CultureInfo` on Unix-like systems.

![screenshot](https://raw.githubusercontent.com/rameel/ramstack.localealignment/c3abd3eb89b4b88965dede9f4a6b1eccb1b74b21/assets/screenshot.png)

## Problem
On Unix-like systems, .NET determines the process culture using only a subset of locale-related environment variables (`LC_ALL`, `LC_MESSAGES`, `LANG`).

Category-specific overrides such as `LC_NUMERIC`, `LC_TIME`, and `LC_MONETARY` **are not applied** during `CultureInfo` initialization.

This behavior is documented and discussed in the .NET runtime repository:
* [https://github.com/dotnet/runtime/issues/110095](https://github.com/dotnet/runtime/issues/110095)

## What this package does
`Ramstack.LocaleAlignment` provides a small runtime API that explicitly aligns
.NET culture settings with POSIX locale category overrides.

When invoked at application startup, it:
* Detects whether the application is running on a Unix-like platform
* Applies the following environment variables to `CultureInfo`:
    * `LC_NUMERIC`
    * `LC_MONETARY`
    * `LC_TIME`
* Updates the current and default thread cultures
* Respects .NET globalization invariant mode

Has no effect on Windows platforms.


## Usage
Add the package to your project:

```bash
dotnet add package Ramstack.LocaleAlignment
```

Call the alignment method as early as possible during application startup:
```csharp
LocaleAlignment.Apply();
```

> [!NOTE]
> This package requires an explicit call.
> If you prefer automatic initialization via source generation,
> see [Ramstack.LocaleAlignment.Generator](https://www.nuget.org/packages/Ramstack.LocaleAlignment.Generator).

No additional configuration or code changes are required.


## Supported versions
|      | Version        |
|------|----------------|
| .NET | 6, 7, 8, 9, 10 |

## Contributions
Bug reports and contributions are welcome.

## License
This package is released as open source under the **MIT License**.<br />
See the [LICENSE](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE) file for more details.
