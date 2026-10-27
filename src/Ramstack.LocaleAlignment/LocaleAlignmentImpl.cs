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
            name = RemoveUnsupportedScript(name);

            // The "C" and "POSIX" locales correspond to the invariant culture
            if (name is "C" or "POSIX")
                name = "";

            if (string.Equals(name, CultureInfo.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                return CultureInfo.CurrentCulture;

            try
            {
                return CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                // An invalid value is ignored as if the variable were not set,
                // so it does not affect other categories
                return null;
            }
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

    internal static string ToBcp47(string posix)
    {
        //
        // Converts a POSIX locale name (e.g., "sr_RS.UTF-8@latin") to
        // a BCP 47 language tag (e.g., "sr-Latn-RS"), the format of .NET culture names.
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
            // Scripts
            if (modifier.SequenceEqual("arabic")) modifier = "Arab";
            else if (modifier.SequenceEqual("cyrillic")) modifier = "Cyrl";
            else if (modifier.SequenceEqual("devanagari")) modifier = "Deva";
            else if (modifier.SequenceEqual("latin")) modifier = "Latn";
            // else if (modifier.SequenceEqual("adlam")) modifier = "Adlm";
            // else if (modifier.SequenceEqual("bengali")) modifier = "Beng";
            // else if (modifier.SequenceEqual("gurmukhi")) modifier = "Guru";
            // else if (modifier.SequenceEqual("olchiki")) modifier = "Olck";
            // else if (modifier.SequenceEqual("orya") || modifier.SequenceEqual("odia")) modifier = "Orya";
            // else if (modifier.SequenceEqual("telugu")) modifier = "Telu";
            // else if (modifier.SequenceEqual("tifinagh")) modifier = "Tfng";
            // else if (modifier.SequenceEqual("vai")) modifier = "Vaii";

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

        // Returning the same variable as the early return above
        // lets the JIT merge both returns into one block, reducing the code size
        posix = new string(buffer);
        return posix;
    }

    internal static string RemoveUnsupportedScript(string name)
    {
        //
        // ICU has no data for some language and script combinations (e.g., "be-Latn")
        // and resolves them to the root locale, losing all language-specific data.
        // In this case, the script is removed to keep at least the language and region
        // data: "be-Latn-BY" -> "be-BY".
        //
        // Only the language and script are checked, since a culture that is not
        // predefined itself still gets the script data through its parent:
        // "sr-Latn-HR" falls back to "sr-Latn".
        //

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
