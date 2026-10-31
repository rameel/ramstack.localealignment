//
// Shared by the library and the source generator, which embeds this file into
// the generated code as a nested class. Hence the file has no namespace and no
// using directives, relies only on the System and System.Globalization namespaces,
// and must compile with C# 10.
//

internal static class LocaleAlignmentImpl
{
    public static void Apply()
    {
        try
        {
            ApplyCore();
        }
        catch
        {
            // Ignore errors
        }
    }

    private static void ApplyCore()
    {
        if (OperatingSystem.IsWindows())
            return;

        //
        // POSIX resolves each category independently: LC_ALL, then LC_<category>, then LANG.
        // .NET resolves a single culture the way POSIX resolves LC_MESSAGES
        // and uses it for both CurrentCulture and CurrentUICulture.
        //
        // LC_ALL overrides every category, and .NET has already applied it.
        //

        if (IsSet("LC_ALL"))
            return;

        //
        // Without any of these variables, there is nothing to align.
        // They are checked before the globalization-invariant mode, since detecting the mode
        // loads ICU and throws an exception in that mode, which would otherwise happen
        // on every startup in containers that enable the mode by default.
        //

        if (!IsSet("LANG") && !IsSet("LC_NUMERIC") && !IsSet("LC_MONETARY") && !IsSet("LC_TIME"))
            return;

        if (IsGlobalizationInvariant())
            return;

        //
        // As on Windows, the regional format and the display language are kept separate:
        // CurrentCulture is based on LANG with the LC_NUMERIC, LC_MONETARY, and LC_TIME overrides,
        // while CurrentUICulture remains as .NET resolved it from LC_MESSAGES.
        //
        // When LANG is not set, the base is the culture .NET resolved itself, e.g., from LC_MESSAGES,
        // rather than the C locale POSIX uses in this case, so alignment never discards the formats
        // .NET would use otherwise. The invariant culture is not the C locale anyway.
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

        // LC_MONETARY takes precedence over LC_NUMERIC for currency formatting
        var nfi = (NumberFormatInfo)numeric.NumberFormat.Clone();
        nfi.CurrencySymbol = monetary.NumberFormat.CurrencySymbol;
        nfi.CurrencyDecimalDigits = monetary.NumberFormat.CurrencyDecimalDigits;
        nfi.CurrencyDecimalSeparator = monetary.NumberFormat.CurrencyDecimalSeparator;
        nfi.CurrencyGroupSeparator = monetary.NumberFormat.CurrencyGroupSeparator;
        nfi.CurrencyGroupSizes = monetary.NumberFormat.CurrencyGroupSizes;
        nfi.CurrencyNegativePattern = monetary.NumberFormat.CurrencyNegativePattern;
        nfi.CurrencyPositivePattern = monetary.NumberFormat.CurrencyPositivePattern;

        //
        // .NET leaves the behavior undefined for a calendar that is not in the culture's OptionalCalendars,
        // so the date and time formats use the first LC_TIME calendar the LANG culture supports as well,
        // e.g., Gregorian instead of Thai Buddhist for LANG=en_US and LC_TIME=th_TH.
        // Every culture supports the Gregorian calendar, so such a calendar is always found.
        //

        var dtfi = time.DateTimeFormat;
        var calendars = lang.OptionalCalendars;
        if (!IsCalendarSupported(calendars, dtfi.Calendar))
        {
            dtfi = (DateTimeFormatInfo)dtfi.Clone();
            foreach (var calendar in time.OptionalCalendars)
            {
                if (IsCalendarSupported(calendars, calendar))
                {
                    dtfi.Calendar = calendar;
                    break;
                }
            }
        }

        // CultureInfo.ReadOnly leaves NumberFormat and DateTimeFormat writable for a neutral culture,
        // so they are made read-only explicitly.
        var culture = (CultureInfo)lang.Clone();
        culture.NumberFormat = NumberFormatInfo.ReadOnly(nfi);
        culture.DateTimeFormat = DateTimeFormatInfo.ReadOnly(dtfi);

        CultureInfo.CurrentCulture = CultureInfo.ReadOnly(culture);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;

        static bool IsSet(string variable) =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable));

        static CultureInfo? GetCultureInfoFromEnvironment(string variable)
        {
            var name = Environment.GetEnvironmentVariable(variable)?.Trim();
            if (string.IsNullOrEmpty(name))
                return null;

            name = ToBcp47(name);
            name = RemoveUnsupportedScript(name);

            // A value without a language, such as ".UTF-8" or "@euro", leaves an empty name,
            // which .NET resolves to the invariant culture instead of rejecting it,
            // so it is ignored the same way as a value .NET rejects.
            if (name.Length == 0)
                return null;

            // The "C" and "POSIX" locales correspond to the invariant culture
            if (name is "C" or "POSIX")
            {
                // On Apple platforms, .NET resolves them to the system locale instead, since GUI apps
                // get no locale variables at all, so LANG is treated as not set to keep that culture.
                // The categories still get the invariant culture, as .NET does not read them.
                //
                // https://github.com/dotnet/runtime/issues/68321
                // https://github.com/dotnet/runtime/pull/68706
                if (variable == "LANG" && (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()))
                    return null;

                name = "";
            }

            if (string.Equals(name, CultureInfo.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                return CultureInfo.CurrentCulture;

            try
            {
                return CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                // A value that .NET rejects is ignored as if the variable were not set,
                // so it does not affect other categories. As in .NET, a well-formed name
                // is not checked against the ICU data, so "xx-YY" is accepted.
                return null;
            }
        }

        static bool IsCalendarSupported(Calendar[] calendars, Calendar calendar)
        {
            // As in the DateTimeFormatInfo.Calendar setter, a subclass counts as the calendar it derives from,
            // and Gregorian calendars with different GregorianCalendarTypes are distinct calendars.
            foreach (var supported in calendars)
                if (supported.GetType().IsInstanceOfType(calendar))
                    if (supported is not GregorianCalendar gc
                        || gc.CalendarType == ((GregorianCalendar)calendar).CalendarType)
                        return true;

            return false;
        }

        static bool IsGlobalizationInvariant()
        {
            //
            // The mode is detected by its effect rather than from the "System.Globalization.Invariant" switch
            // and the DOTNET_SYSTEM_GLOBALIZATION_INVARIANT variable, since the precedence between them
            // depends on the .NET version: the switch wins up to .NET 8, and the variable since .NET 9.
            //

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

    internal static string ToBcp47(string posix)
    {
        //
        // Converts a POSIX locale name (e.g., "sr_RS.UTF-8@latin") to
        // a BCP 47 language tag (e.g., "sr-Latn-RS"), the format of .NET culture names.
        //
        // A POSIX locale name has the format language[_territory][.codeset][@modifier],
        // where every part except the language is optional,
        // e.g., "en_IE.UTF-8@euro", "en_IE.UTF-8", "en_IE@euro", "en_IE".
        //
        // BCP 47 (IETF Best Current Practice 47, RFC 5646) defines a language tag
        // as hyphen-separated subtags: language[-script][-region][-variant],
        // e.g., "en-US", "sr-Latn-RS", "ca-ES-valencia".
        //
        // This is a simplified conversion: the codeset is dropped, and the modifier
        // is mapped to a script or variant subtag if known, or dropped otherwise.
        // Note that ICU (and therefore .NET) keeps the modifier as a variant
        // ("sr-RS-LATIN"), which loses the script and resolves to Cyrillic data.
        //

        if (posix.AsSpan().IndexOfAny('.', '_', '@') < 0)
            return posix;

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
            // Scripts
            if (modifier.SequenceEqual("arabic")) modifier = "Arab";
            else if (modifier.SequenceEqual("cyrillic")) modifier = "Cyrl";
            else if (modifier.SequenceEqual("devanagari")) modifier = "Deva";
            else if (modifier.SequenceEqual("latin")) modifier = "Latn";

            // Variants
            else if (modifier.SequenceEqual("valencia")) modifier = "valencia";

            // Skip
            else
                modifier = default;
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
            var index = name.IndexOf('_');
            if (index > 0 && modifier.Length == 4 && (uint)modifier[0] - 'A' <= 'Z' - 'A')
            {
                // language-modifier-territory
                name.Slice(0, index).TryCopyTo(buffer);
                buffer[index] = '-';

                modifier.TryCopyTo(buffer.Slice(index + 1));
                buffer[index + 1 + modifier.Length] = '-';

                name.Slice(index + 1).TryCopyTo(buffer.Slice(index + 2 + modifier.Length));
                buffer = buffer.Slice(0, name.Length + modifier.Length + 1);
            }
            else
            {
                // language[-territory]-modifier
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

        // Returning the same variable as the early return above
        // lets the JIT merge both returns into one block, reducing the code size.
        posix = new string(buffer);
        return posix;
    }

    internal static string RemoveUnsupportedScript(string name)
    {
        //
        // ICU has no data for some language and script combinations (e.g., "be-Latn")
        // and resolves them to the default locale, the one resolved for LC_MESSAGES,
        // losing all language-specific data.
        // In this case, the script is removed to keep at least the language and region
        // data: "be-Latn-BY" -> "be-BY".
        //
        // Only the language and script are checked, since a culture that is not
        // predefined itself still gets the script data through its parent:
        // "sr-Latn-HR" falls back to "sr-Latn".
        //
        // Only a script in the second subtag matters ("be-Latn", "be-Latn-BY"),
        // anything else is returned as is:
        // - no language subtag: "be", ""
        // - the second subtag is not 4 characters long, so it is a region of 2 letters
        //   or 3 digits ("be-BY", "es-419"), a variant of 5-8 characters ("ca-valencia"),
        //   or a singleton of an extension ("en-u-nu-thai", "en-x-test")
        // - the second subtag starts with a digit, so it is a 4-character variant ("de-1901")
        //

        var lhs = name.IndexOf('-');
        var rhs = name.IndexOf('-', lhs + 1);
        if (rhs < 0)
            rhs = name.Length;

        if (lhs <= 0
            || rhs - lhs != 5
            || (uint)((name[lhs + 1] | 0x20) - 'a') > 'z' - 'a')
            return name;

        try
        {
            CultureInfo.GetCultureInfo(name[..(lhs + 5)], predefinedOnly: true);
        }
        catch (CultureNotFoundException)
        {
            name = name.Remove(lhs, 5);
        }

        return name;
    }
}
