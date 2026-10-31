using System.Globalization;
using System.Text;

using Ramstack.LocaleAlignment;
using Ramstack.LocaleAlignment.TestApp;

//
// Applies the locale settings from the environment and prints the resulting
// culture settings as "key=value" lines for Ramstack.LocaleAlignment.Tests.
//

LocaleAlignment.Apply();

//
// Without the execution context, the thread does not inherit the current culture
// and starts with DefaultThreadCurrentCulture instead.
//

CultureInfo? threadCulture = null;
var thread = new Thread(() => threadCulture = CultureInfo.CurrentCulture);
using (ExecutionContext.SuppressFlow())
    thread.Start();
thread.Join();

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine($"ui={CultureInfo.CurrentUICulture.Name}");
Print("", CultureInfo.CurrentCulture);
Print("thread.", threadCulture!);

static void Print(string prefix, CultureInfo culture)
{
    Console.WriteLine($"{prefix}culture={culture.Name}");
    Console.WriteLine($"{prefix}numeric={CultureReport.DescribeNumeric(culture)}");
    Console.WriteLine($"{prefix}monetary={CultureReport.DescribeMonetary(culture)}");
    Console.WriteLine($"{prefix}time={CultureReport.DescribeTime(culture)}");
    Console.WriteLine($"{prefix}readonly={culture.IsReadOnly && culture.NumberFormat.IsReadOnly && culture.DateTimeFormat.IsReadOnly}");
    Console.WriteLine($"{prefix}calendar={IsCalendarSupported(culture)}");
}

//
// Whether the date and time formats use one of the calendars the culture supports.
// The DateTimeFormatInfo.Calendar setter rejects a calendar its own culture does not support,
// so the check is left to .NET on a new instance of the culture, since the date and time formats
// of the culture itself may come from another one.
//

static bool IsCalendarSupported(CultureInfo culture)
{
    try
    {
        new CultureInfo(culture.Name).DateTimeFormat.Calendar = culture.DateTimeFormat.Calendar;
        return true;
    }
    catch (ArgumentOutOfRangeException)
    {
        return false;
    }
}
