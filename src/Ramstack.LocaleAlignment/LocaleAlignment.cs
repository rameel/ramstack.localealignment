using System.Threading;

namespace Ramstack.LocaleAlignment;

/// <summary>
/// Provides helpers for applying POSIX locale settings to .NET culture on Unix-like systems.
/// </summary>
/// <remarks>
/// On Unix-like systems, .NET determines the process culture using only a subset
/// of locale-related environment variables (LC_ALL, LC_MESSAGES, LANG).
/// Category-specific overrides such as LC_NUMERIC, LC_TIME, and LC_MONETARY
/// are not applied during <see cref="CultureInfo"/> initialization,
/// and LANG is ignored when LC_MESSAGES is set.
/// </remarks>
public static class LocaleAlignment
{
    private static int s_initialized;

    /// <summary>
    /// Applies POSIX locale settings (LANG, LC_NUMERIC, LC_MONETARY, LC_TIME) to the current culture.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   Call this method as early as possible during application startup,
    ///   before any culture-sensitive code runs.
    /// </para>
    /// <para>
    ///   LC_ALL takes precedence over all other variables, as defined by POSIX.
    ///   The UI culture is not changed and remains derived from LC_MESSAGES.
    /// </para>
    /// <para>
    ///   It has no effect on Windows or in globalization-invariant mode.
    /// </para>
    /// </remarks>
    public static void Apply()
    {
        if (Interlocked.Exchange(ref s_initialized, 1) == 0)
            LocaleAlignmentImpl.Apply();
    }
}
