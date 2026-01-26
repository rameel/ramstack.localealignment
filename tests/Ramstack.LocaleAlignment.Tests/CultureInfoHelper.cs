namespace Ramstack.LocaleAlignment.Tests;

internal static class CultureInfoHelper
{
    public static string ToBcp47(string posix)
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
            //
            // language-modifier-territory
            //
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
                //
                // language-modifier
                //
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
}
