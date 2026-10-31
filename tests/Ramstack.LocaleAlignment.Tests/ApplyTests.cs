using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

using Ramstack.LocaleAlignment.TestApp;

namespace Ramstack.LocaleAlignment.Tests;

//
// The expected values follow how .NET resolves the locale on Linux.
// On Apple platforms, .NET resolves the C and POSIX locales, as well as an unset locale,
// to the system locale rather than to the invariant culture, and uses the ICU data of the system,
// so the tests are skipped there. On Windows, Apply has no effect.
//

[TestFixture]
[Platform(Exclude = "Win,MacOsX", Reason = "The expected values follow how .NET resolves the locale on Linux")]
public class ApplyTests
{
    //
    // Apply changes process-wide settings and runs only once per process,
    // so each scenario runs Ramstack.LocaleAlignment.TestApp in a separate process.
    //
    // The expected values are culture names: the current culture, the UI culture
    // (null when it is left entirely to .NET), and the cultures expected to provide
    // the numeric, monetary, and time settings. An empty name is the invariant culture.
    //

    // No locale settings
    [TestCase("", "", "", "", "", "")]
    [TestCase("LANG=en_US.UTF-8", "en-US", "en-US", "en-US", "en-US", "en-US")]

    // Category overrides
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=ru_RU.UTF-8 LC_MONETARY=ru_RU.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "ru-RU", "ru-RU", "ru-RU")]
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=ru_RU.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "ru-RU", "en-US", "ru-RU")]
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=de_DE.UTF-8 LC_MONETARY=ru_RU.UTF-8", "en-US", "en-US", "de-DE", "ru-RU", "en-US")]
    [TestCase("LANG=en_US.UTF-8 LC_MONETARY=ru_RU.UTF-8", "en-US", "en-US", "en-US", "ru-RU", "en-US")]
    [TestCase("LANG=en_US.UTF-8 LC_TIME=sr_RS.UTF-8@latin", "en-US", "en-US", "en-US", "en-US", "sr-Latn-RS")]

    // A calendar the base culture supports is kept, including a non-Gregorian one
    [TestCase("LANG=th_TH.UTF-8 LC_NUMERIC=ru_RU.UTF-8", "th-TH", "th-TH", "ru-RU", "th-TH", "th-TH")]
    [TestCase("LANG=th_TH.UTF-8 LC_TIME=en_US.UTF-8", "th-TH", "th-TH", "th-TH", "th-TH", "en-US")]

    // A script without ICU data is removed instead of resolving to the default locale
    [TestCase("LANG=en_US.UTF-8 LC_TIME=be_BY.UTF-8@latin", "en-US", "en-US", "en-US", "en-US", "be-BY")]

    // A neutral culture, for which CultureInfo.ReadOnly leaves the number and date formats writable
    [TestCase("LANG=de.UTF-8 LC_NUMERIC=ru_RU.UTF-8", "de", "de", "ru-RU", "de", "de")]

    // LANG is the base culture, while LC_MESSAGES only affects the UI culture
    [TestCase("LANG=cs_CZ.UTF-8 LC_MESSAGES=en_US.UTF-8", "cs-CZ", "en-US", "cs-CZ", "cs-CZ", "cs-CZ")]
    [TestCase("LANG=ru_RU.UTF-8 LC_MESSAGES=C", "ru-RU", "", "ru-RU", "ru-RU", "ru-RU")]
    [TestCase("LC_MESSAGES=en_US.UTF-8 LC_NUMERIC=ru_RU.UTF-8", "en-US", "en-US", "ru-RU", "en-US", "en-US")]

    // LC_ALL overrides every category, and an empty value is ignored
    [TestCase("LC_ALL=en_US.UTF-8 LC_NUMERIC=ru_RU.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "en-US")]
    [TestCase("LC_ALL=C LANG=ru_RU.UTF-8 LC_NUMERIC=ru_RU.UTF-8", "", "", "", "", "")]
    [TestCase("LC_ALL= LANG=ru_RU.UTF-8", "ru-RU", null, "ru-RU", "ru-RU", "ru-RU")]

    // C and POSIX locales
    [TestCase("LANG=C.UTF-8 LC_NUMERIC=ru_RU.UTF-8", "", "", "ru-RU", "", "")]
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=C", "ru-RU", "ru-RU", "", "ru-RU", "ru-RU")]
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=C.UTF-8", "ru-RU", "ru-RU", "", "ru-RU", "ru-RU")]
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=POSIX DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=true", "ru-RU", "ru-RU", "", "ru-RU", "ru-RU")]

    // A value that .NET rejects is ignored as if the variable were not set, without affecting other categories
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=@latin LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "ru-RU")]
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=sr_HR@latin LC_TIME=ru_RU.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=true", "en-US", "en-US", "en-US", "en-US", "ru-RU")]
    [TestCase("LANG=_RU LC_MESSAGES=en_US.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "ru-RU")]

    // A value without a language is ignored as well, rather than resolving to the invariant culture
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=.UTF-8 LC_MONETARY=@euro LC_TIME=@euro", "ru-RU", "ru-RU", "ru-RU", "ru-RU", "ru-RU")]
    [TestCase("LANG=.UTF-8 LC_MESSAGES=en_US.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "ru-RU")]
    [TestCase("LANG=@euro LC_MESSAGES=en_US.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "ru-RU")]

    // Globalization-invariant mode
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=en_US.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1", "", "", "", "", "")]

    // Without PredefinedCulturesOnly, the mode returns cultures with the invariant data instead of rejecting them
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=en_US.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=false", "", "", "", "", "")]
    public void Apply(string environment, string culture, string? ui, string numeric, string monetary, string time)
    {
        var report = Run(environment);

        Assert.Multiple(() =>
        {
            Assert.That(report["culture"], Is.EqualTo(culture), "culture");
            if (ui is not null)
                Assert.That(report["ui"], Is.EqualTo(ui), "UI culture");

            Assert.That(report["numeric"], Is.EqualTo(CultureReport.DescribeNumeric(CultureInfo.GetCultureInfo(numeric))), "numeric");
            Assert.That(report["monetary"], Is.EqualTo(CultureReport.DescribeMonetary(CultureInfo.GetCultureInfo(monetary))), "monetary");
            Assert.That(report["time"], Is.EqualTo(CultureReport.DescribeTime(CultureInfo.GetCultureInfo(time))), "time");

            // The culture is shared by all threads, so neither it nor its formats can be modified
            Assert.That(report["readonly"], Is.EqualTo("True"), "read-only");

            // .NET leaves the behavior undefined for a calendar the culture does not support
            Assert.That(report["calendar"], Is.EqualTo("True"), "calendar");

            AssertThreadCulture(report);
        });
    }

    //
    // An LC_TIME calendar the base culture does not support is replaced with one it does,
    // so en-US gets the Thai formats with the Gregorian calendar instead of the Thai Buddhist one.
    //

    [TestCase("en_US.UTF-8", "en-US")]

    // A neutral culture, for which CultureInfo.ReadOnly leaves the date and time formats writable
    [TestCase("en.UTF-8", "en")]
    public void Apply_UnsupportedCalendar(string lang, string culture)
    {
        var report = Run($"LANG={lang} LC_TIME=th_TH.UTF-8");

        var time = (CultureInfo)CultureInfo.GetCultureInfo("th-TH").Clone();
        time.DateTimeFormat.Calendar = new GregorianCalendar();

        Assert.Multiple(() =>
        {
            Assert.That(report["culture"], Is.EqualTo(culture), "culture");
            Assert.That(report["time"], Is.EqualTo(CultureReport.DescribeTime(time)), "time");
            Assert.That(report["calendar"], Is.EqualTo("True"), "calendar");

            // The date and time formats are cloned to replace the calendar, so they are made read-only again
            Assert.That(report["readonly"], Is.EqualTo("True"), "read-only");

            AssertThreadCulture(report);
        });
    }

    //
    // When the "System.Globalization.Invariant" switch and the DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
    // variable disagree, the switch wins up to .NET 8, and the variable since .NET 9.
    //

    [TestCase(true, "0")]
    [TestCase(false, "1")]
    public void Apply_InvariantSwitchAndVariableDisagree(bool invariantSwitch, string variable)
    {
        var invariant = Environment.Version.Major >= 9 ? variable == "1" : invariantSwitch;
        var report = Run($"LANG=en_US.UTF-8 LC_NUMERIC=ru_RU.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT={variable}", invariantSwitch);

        var culture = invariant ? "" : "en-US";
        var numeric = invariant ? "" : "ru-RU";

        Assert.Multiple(() =>
        {
            Assert.That(report["culture"], Is.EqualTo(culture), "culture");
            Assert.That(report["numeric"], Is.EqualTo(CultureReport.DescribeNumeric(CultureInfo.GetCultureInfo(numeric))), "numeric");
        });
    }

    //
    // The scenarios above are only meaningful if the reports of the cultures
    // they distinguish actually differ.
    //

    [TestCase("", "en-US")]
    [TestCase("", "ru-RU")]
    [TestCase("en-US", "ru-RU")]
    [TestCase("en-US", "de-DE")]
    [TestCase("en-US", "cs-CZ")]
    [TestCase("en-US", "sr-Latn-RS")]
    [TestCase("en-US", "be-BY")]
    [TestCase("be-BY", "be-Latn-BY")]
    [TestCase("de-DE", "ru-RU")]
    public void Report_DistinguishesCultures(string first, string second)
    {
        var a = CultureInfo.GetCultureInfo(first);
        var b = CultureInfo.GetCultureInfo(second);

        Assert.Multiple(() =>
        {
            Assert.That(CultureReport.DescribeNumeric(a), Is.Not.EqualTo(CultureReport.DescribeNumeric(b)), "numeric");
            Assert.That(CultureReport.DescribeMonetary(a), Is.Not.EqualTo(CultureReport.DescribeMonetary(b)), "monetary");
            Assert.That(CultureReport.DescribeTime(a), Is.Not.EqualTo(CultureReport.DescribeTime(b)), "time");
        });
    }

    //
    // Threads that do not inherit the current culture get the same one through DefaultThreadCurrentCulture,
    // so every "thread." value of the report matches the value for the current culture.
    // The keys are listed explicitly, so a report without them fails instead of passing with nothing checked.
    //

    private static void AssertThreadCulture(Dictionary<string, string> report)
    {
        foreach (var key in new[] { "culture", "numeric", "monetary", "time", "readonly", "calendar" })
            Assert.That(report["thread." + key], Is.EqualTo(report[key]), "thread " + key);
    }

    private static Dictionary<string, string> Run(string environment, bool? invariantSwitch = null)
    {
        var start = new ProcessStartInfo(DotNetHost)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false
        };

        var app = Path.Combine(AppContext.BaseDirectory, "Ramstack.LocaleAlignment.TestApp.dll");

        if (invariantSwitch is not null)
        {
            // The switch is read from runtimeconfig.json, so the app runs with a copy of its own
            // that sets the switch, the same as <InvariantGlobalization> in the project file does.
            var config = JsonNode.Parse(File.ReadAllText(Path.ChangeExtension(app, ".runtimeconfig.json")))!;
            var options = config["runtimeOptions"]!.AsObject();
            if (options["configProperties"] is not JsonObject properties)
                options["configProperties"] = properties = new JsonObject();

            properties["System.Globalization.Invariant"] = invariantSwitch.Value;

            var path = Path.ChangeExtension(app, $".Invariant{invariantSwitch.Value}.runtimeconfig.json");
            File.WriteAllText(path, config.ToJsonString());

            start.ArgumentList.Add("exec");
            start.ArgumentList.Add("--runtimeconfig");
            start.ArgumentList.Add(path);
        }

        start.ArgumentList.Add(app);

        // Start from a clean locale environment
        foreach (var name in start.Environment.Keys.ToArray())
            if (name is "LANG" or "LANGUAGE"
                || name.StartsWith("LC_", StringComparison.Ordinal)
                || name.StartsWith("DOTNET_SYSTEM_GLOBALIZATION_", StringComparison.Ordinal))
                start.Environment.Remove(name);

        foreach (var variable in environment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = variable.IndexOf('=');
            start.Environment[variable[..index]] = variable[(index + 1)..];
        }

        using var process = Process.Start(start)!;

        // Both streams are read at the same time, so the app cannot block on writing
        // to a full pipe of one stream while the other one is being read to the end.
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.Zero, error.Result);

        return output.Result
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => pair[1]);
    }

    private static string DotNetHost
    {
        get
        {
            var path = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? Environment.ProcessPath;
            return path is not null && Path.GetFileNameWithoutExtension(path) == "dotnet" ? path : "dotnet";
        }
    }
}
