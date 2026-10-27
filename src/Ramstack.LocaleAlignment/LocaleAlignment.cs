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
        try
        {
            if (Interlocked.Exchange(ref s_initialized, 1) == 0)
                ApplyCore();
        }
        catch
        {
            // Ignore errors
        }
    }

    private static void ApplyCore()
    {
        if (OperatingSystem.IsWindows() || IsGlobalizationInvariant())
            return;

        //
        // POSIX resolves each category independently: LC_ALL, then LC_<category>, then LANG.
        // .NET resolves a single culture the way POSIX resolves LC_MESSAGES
        // and uses it for both CurrentCulture and CurrentUICulture.
        //
        // LC_ALL overrides every category, and .NET has already applied it.
        //

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LC_ALL")))
            return;

        //
        // As on Windows, the regional format and the display language are kept separate:
        // CurrentCulture is based on LANG with the LC_NUMERIC, LC_MONETARY, and LC_TIME overrides,
        // while CurrentUICulture remains as .NET resolved it from LC_MESSAGES.
        //

        var current = CultureInfo.CurrentCulture;
        var lang = GetCultureInfoFromEnvironment("LANG") ?? current;
        var numeric = GetCultureInfoFromEnvironment("LC_NUMERIC") ?? lang;
        var monetary = GetCultureInfoFromEnvironment("LC_MONETARY") ?? lang;
        var time = GetCultureInfoFromEnvironment("LC_TIME") ?? lang;

        if (ReferenceEquals(lang, current)
            && ReferenceEquals(numeric, current)
            && ReferenceEquals(monetary, current)
            && ReferenceEquals(time, current))
            return;

        //
        // LC_MONETARY takes precedence over LC_NUMERIC for currency formatting.
        //

        var nfi = (NumberFormatInfo)numeric.NumberFormat.Clone();
        nfi.CurrencySymbol = monetary.NumberFormat.CurrencySymbol;
        nfi.CurrencyDecimalDigits = monetary.NumberFormat.CurrencyDecimalDigits;
        nfi.CurrencyDecimalSeparator = monetary.NumberFormat.CurrencyDecimalSeparator;
        nfi.CurrencyGroupSeparator = monetary.NumberFormat.CurrencyGroupSeparator;
        nfi.CurrencyGroupSizes = monetary.NumberFormat.CurrencyGroupSizes;
        nfi.CurrencyNegativePattern = monetary.NumberFormat.CurrencyNegativePattern;
        nfi.CurrencyPositivePattern = monetary.NumberFormat.CurrencyPositivePattern;

        var culture = (CultureInfo)lang.Clone();
        culture.NumberFormat = nfi;
        culture.DateTimeFormat = time.DateTimeFormat;

        CultureInfo.CurrentCulture = CultureInfo.ReadOnly(culture);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;

        static CultureInfo? GetCultureInfoFromEnvironment(string variable)
        {
            var name = Environment.GetEnvironmentVariable(variable)?.Trim();
            if (string.IsNullOrEmpty(name))
                return null;

            name = ToBcp47(name);

            // The "C" and "POSIX" locales correspond to the invariant culture
            if (name is "C" or "POSIX")
                name = "";

            if (string.Equals(name, CultureInfo.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                return CultureInfo.CurrentCulture;

            return CultureInfo.GetCultureInfo(name);
        }

        static string ToBcp47(string posix)
        {
            //
            // Converts a POSIX-style locale string (e.g., "ff_CM.UTF-8@latin") to
            // a BCP-47 compliant culture name (e.g., "ff-Latn-CM").
            //

            if (posix.AsSpan().IndexOfAny('.', '_', '@') < 0)
                return posix;

            // Format:
            // language[_territory][.codeset][@modifier]
            //
            // en_IE.UTF-8@euro - full
            // en_IE.UTF-8      - without modifier
            // en_IE@euro       - without codeset
            // en_IE            - minimal

            var di = posix.IndexOf('.');
            if (di < 0)
                di = posix.Length;

            var ai = posix.IndexOf('@');
            if (ai < 0)
                ai = posix.Length;

            var name = posix.AsSpan(0, Math.Min(di, ai));
            var modifier = posix.AsSpan(Math.Min(ai + 1, posix.Length));

            if (!modifier.IsEmpty)
            {
                modifier = modifier switch
                {
                    // Scripts
                    "arabic" => "Arab",
                    "cyrillic" => "Cyrl",
                    "devanagari" => "Deva",
                    "latin" => "Latn",
                    // "adlam" => "Adlm",
                    // "bengali" => "Beng",
                    // "gurmukhi" => "Guru",
                    // "olchiki" => "Olck",
                    // "orya" or "odia" => "Orya",
                    // "telugu" => "Telu",
                    // "tifinagh" => "Tfng",
                    // "vai" => "Vaii",

                    // Variants
                    "valencia" => "valencia",

                    // Skip
                    _ => ""
                };
            }

            var buffer = posix.Length > 32
                ? new char[posix.Length]
                : stackalloc char[32];

            if (modifier.IsEmpty)
            {
                name.TryCopyTo(buffer);
                buffer = buffer.Slice(0, name.Length);
            }
            else
            {
                // language-modifier-territory
                var index = name.IndexOf('_');
                if (index > 0 && modifier.Length == 4 && (uint)modifier[0] - 'A' <= 'Z' - 'A')
                {
                    name.Slice(0, index).TryCopyTo(buffer);
                    buffer[index] = '-';

                    modifier.TryCopyTo(buffer.Slice(index + 1));
                    buffer[index + 1 + modifier.Length] = '-';

                    name.Slice(index + 1).TryCopyTo(buffer.Slice(index + 2 + modifier.Length));
                    buffer = buffer.Slice(0, name.Length + modifier.Length + 1);
                }
                else
                {
                    // language-modifier
                    name.TryCopyTo(buffer);
                    buffer[name.Length] = '-';
                    modifier.TryCopyTo(buffer.Slice(name.Length + 1));
                    buffer = buffer.Slice(0, name.Length + 1 + modifier.Length);
                }
            }

            #if NET8_0_OR_GREATER
            buffer.Replace('_', '-');
            #else
            for (var i = 0; i < buffer.Length; i++)
                if (buffer[i] == '_')
                    buffer[i] = '-';
            #endif

            posix = new string(buffer);
            return posix;
        }

        static bool IsGlobalizationInvariant()
        {
            // https://github.com/dotnet/runtime/blob/8fafcb3251a3d2ccde36612a2eab47e9ae8ebe2b/src/libraries/System.Private.CoreLib/src/System/Globalization/GlobalizationMode.cs

            if (AppContext.TryGetSwitch("System.Globalization.Invariant", out var isInvariant) && isInvariant)
                return true;

            var value = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
            if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                // Starting in .NET 6, creating or resolving cultures other than the invariant one
                // throws an exception except when "PredefinedCulturesOnly" is set to false
                // (in this case, NativeName contains "Invariant" for all returned cultures).
                //
                // https://learn.microsoft.com/en-us/dotnet/core/compatibility/globalization/6.0/culture-creation-invariant-mode
                // https://learn.microsoft.com/en-us/dotnet/core/runtime-config/globalization#predefined-cultures

                return CultureInfo.GetCultureInfo("en-US").NativeName.Contains("Invariant", StringComparison.Ordinal);
            }
            catch (CultureNotFoundException)
            {
                return true;
            }
        }
    }
}
