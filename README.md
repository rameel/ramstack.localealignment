# Ramstack.LocaleAlignment
[![NuGet](https://img.shields.io/nuget/v/Ramstack.LocaleAlignment.svg)](https://nuget.org/packages/Ramstack.LocaleAlignment)
[![MIT](https://img.shields.io/github/license/rameel/ramstack.localealignment)](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE)

A Roslyn source generator that applies POSIX locale user overrides to .NET `CultureInfo` on Unix-like systems.

![](https://github.com/rameel/ramstack.localealignment/blob/main/assets/screenshot.png)

## Problem
On Unix platforms, .NET determines the process culture using a limited subset of locale-related environment variables,
in the following order:
* `LC_ALL`
* `LC_MESSAGES`
* `LANG`

Overrides such as `LC_NUMERIC`, `LC_TIME`, and `LC_MONETARY` are **ignored** for `CultureInfo` initialization,
even though they are explicitly defined by POSIX and commonly used by users to fine-tune locale behavior.

As a result, applications may observe:

* Incorrect numeric formatting when `LC_NUMERIC` is overridden
* Incorrect date and time formatting when `LC_TIME` is overridden
* Incorrect currency formatting when `LC_MONETARY` is overridden

This behavior is documented and discussed in the .NET runtime repository:

* [https://github.com/dotnet/runtime/issues/110095](https://github.com/dotnet/runtime/issues/110095)

## What this package does
`Ramstack.LocaleAlignment` provides a **source generator** that injects a module initializer into your application.

At startup, it:
* Detects whether the application is running on a non-Windows platform
* Respects .NET globalization invariant mode
* Reads the following environment variables independently:
    * `LC_NUMERIC`
    * `LC_MONETARY`
    * `LC_TIME`
* Applies their effects to `CultureInfo` in a POSIX-consistent manner

The generator creates a derived `CultureInfo` instance that:
* Uses date/time formatting from `LC_TIME`
* Uses numeric formatting from `LC_NUMERIC`
* Uses currency formatting from `LC_MONETARY`

The resulting culture is applied to:

* `CultureInfo.CurrentCulture`
* `CultureInfo.CurrentUICulture`
* `CultureInfo.DefaultThreadCurrentCulture`
* `CultureInfo.DefaultThreadCurrentUICulture`

All initialization is performed defensively and never throws.

## Usage
Add the package to your project:

```bash
dotnet add package Ramstack.LocaleAlignment
```

No additional configuration or code changes are required.

The generated initializer runs automatically at module load time.

## Platform behavior
* **Windows**: no effect (not required - Windows automatically applies all user locale overrides)
* **Unix / Linux**: locale alignment is applied
* **Invariant globalization mode**: no effect

## Supported versions

|      | Version        |
|------|----------------|
| .NET | 6, 7, 8, 9, 10 |

## Contributions
Bug reports and contributions are welcome.

## License
This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE) file for more details.
