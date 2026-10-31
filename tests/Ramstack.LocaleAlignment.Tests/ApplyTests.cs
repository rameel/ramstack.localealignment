using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

using Ramstack.LocaleAlignment.TestApp;

namespace Ramstack.LocaleAlignment.Tests;

[TestFixture]
[Platform(Exclude = "Win")]
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

    // A script without ICU data is removed instead of resolving to the root locale
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
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=C", "ru-RU", "ru-RU", "", "ru-RU", "ru-RU")]
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=C.UTF-8", "ru-RU", "ru-RU", "", "ru-RU", "ru-RU")]
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=POSIX DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=true", "ru-RU", "ru-RU", "", "ru-RU", "ru-RU")]

    // An invalid value is ignored as if the variable were not set, without affecting other categories
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=@latin LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "ru-RU")]
    [TestCase("LANG=en_US.UTF-8 LC_NUMERIC=sr_HR@latin LC_TIME=ru_RU.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=true", "en-US", "en-US", "en-US", "en-US", "ru-RU")]
    [TestCase("LANG=_RU LC_MESSAGES=en_US.UTF-8 LC_TIME=ru_RU.UTF-8", "en-US", "en-US", "en-US", "en-US", "ru-RU")]

    // Globalization-invariant mode
    [TestCase("LANG=ru_RU.UTF-8 LC_NUMERIC=en_US.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1", "", "", "", "", "")]
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

            // Threads that do not inherit the current culture get the same one through DefaultThreadCurrentCulture
            foreach (var key in new[] { "culture", "numeric", "monetary", "time", "readonly" })
                Assert.That(report["thread." + key], Is.EqualTo(report[key]), "thread " + key);
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
            //
            // The switch is read from runtimeconfig.json, so the app runs with a copy of its own
            // that sets the switch, the same as <InvariantGlobalization> in the project file does.
            //

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
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.Zero, error);

        return output
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
