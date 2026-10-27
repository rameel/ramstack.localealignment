# Ramstack.LocaleAlignment
[![NuGet](https://img.shields.io/nuget/v/Ramstack.LocaleAlignment.svg)](https://nuget.org/packages/Ramstack.LocaleAlignment)
[![MIT](https://img.shields.io/github/license/rameel/ramstack.localealignment)](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE)

A small library for aligning .NET culture settings with POSIX locale user overrides on Unix-like systems.

![screenshot](https://raw.githubusercontent.com/rameel/ramstack.localealignment/c3abd3eb89b4b88965dede9f4a6b1eccb1b74b21/assets/screenshot.png)

## Problem
On Unix-like systems, .NET determines the process culture using only a subset of locale-related environment variables (`LC_ALL`, `LC_MESSAGES`, `LANG`).

Category-specific overrides such as `LC_NUMERIC`, `LC_TIME`, and `LC_MONETARY` **are not applied** during `CultureInfo` initialization,
and `LANG` is ignored when `LC_MESSAGES` is set.

This behavior is documented and discussed in the .NET runtime repository:
* [https://github.com/dotnet/runtime/issues/110095](https://github.com/dotnet/runtime/issues/110095)


## Project structure
This repository contains two packages:

### Ramstack.LocaleAlignment
A small runtime library that explicitly applies POSIX locale category overrides to .NET `CultureInfo`.

It is intended to be called manually during application startup and can be used from any .NET language (`C#`, `F#`, `Nemerle`, etc.).

### Ramstack.LocaleAlignment.Generator
A dependency-free Roslyn source generator that enables automatic locale alignment at application startup.

The generator injects the equivalent of a `LocaleAlignment.Apply()` call directly into the compiled output,
without introducing any runtime dependency or requiring any configuration.


## Supported versions
|      | Version        |
|------|----------------|
| .NET | 6, 7, 8, 9, 10 |

## Contributions
Bug reports and contributions are welcome.

## License
This package is released as open source under the **MIT License**.<br />
See the [LICENSE](https://github.com/rameel/ramstack.localealignment/blob/main/LICENSE) file for more details.
