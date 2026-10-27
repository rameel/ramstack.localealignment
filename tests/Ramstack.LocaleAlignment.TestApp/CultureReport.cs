using System.Globalization;

namespace Ramstack.LocaleAlignment.TestApp;

/// <summary>
/// Describes the culture settings controlled by each POSIX locale category,
/// so that the settings of a process can be compared with those of a reference culture.
/// </summary>
public static class CultureReport
{
    public static string DescribeNumeric(CultureInfo culture)
    {
        var nfi = culture.NumberFormat;
        return Join(
            nfi.NumberDecimalSeparator,
            nfi.NumberGroupSeparator,
            string.Join(",", nfi.NumberGroupSizes),
            nfi.NegativeSign,
            nfi.PercentSymbol,
            nfi.PositiveInfinitySymbol);
    }

    public static string DescribeMonetary(CultureInfo culture)
    {
        var nfi = culture.NumberFormat;
        return Join(
            nfi.CurrencySymbol,
            nfi.CurrencyDecimalSeparator,
            nfi.CurrencyGroupSeparator,
            string.Join(",", nfi.CurrencyGroupSizes),
            nfi.CurrencyDecimalDigits.ToString(CultureInfo.InvariantCulture),
            nfi.CurrencyPositivePattern.ToString(CultureInfo.InvariantCulture),
            nfi.CurrencyNegativePattern.ToString(CultureInfo.InvariantCulture));
    }

    public static string DescribeTime(CultureInfo culture)
    {
        var dtfi = culture.DateTimeFormat;
        return Join(
            dtfi.ShortDatePattern,
            dtfi.LongDatePattern,
            dtfi.ShortTimePattern,
            dtfi.DayNames[1],
            dtfi.MonthNames[0]);
    }

    private static string Join(params string[] values) =>
        string.Join("|", values);
}
