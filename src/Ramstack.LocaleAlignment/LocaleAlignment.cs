using System.ComponentModel;

namespace Ramstack.LocaleAlignment;

/// <inheritdoc cref="LocaleAligner"/>
[Obsolete("Use LocaleAligner instead.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LocaleAlignment
{
    /// <inheritdoc cref="LocaleAligner.Apply"/>
    public static void Apply() =>
        LocaleAligner.Apply();
}
