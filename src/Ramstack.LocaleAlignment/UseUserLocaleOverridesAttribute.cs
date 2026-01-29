namespace Ramstack.LocaleAlignment;

/// <summary>
/// Applies POSIX locale category overrides
/// (<c>LC_NUMERIC</c>, <c>LC_TIME</c>, and <c>LC_MONETARY</c>)
/// to .NET <see src="CultureInfo" /> on Unix systems.
/// </summary>
/// <remarks>
/// <para>
/// On Unix platforms, .NET ignores <c>LC_NUMERIC</c>, <c>LC_TIME</c>,
/// and <c>LC_MONETARY</c> environment variables when initializing
/// <see src="CultureInfo" />. This attribute ensures these locale category
/// overrides are respected by triggering the automatic initialization
/// provided by this library.
/// </para>
/// <para>
/// Has no effect on Windows (where all user locale overrides are already respected)
/// or when globalization invariant mode is enabled.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class UseUserLocaleOverridesAttribute : Attribute;
