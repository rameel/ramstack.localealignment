using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Ramstack.LocaleAlignment.Generator;

namespace Ramstack.LocaleAlignment.Tests;

[TestFixture]
public class GeneratorTests
{
    //
    // The generator runs on an in-memory application compiled against the current runtime
    // with the preprocessor symbols of its version, so the code embedded from LocaleAlignmentImpl.cs,
    // including the branches for newer .NET versions, is compiled as in an application targeting it.
    //

    private const string Program = "class Program { static void Main() { } }";

    [Test]
    public void Library_NothingGenerated()
    {
        var (generated, _) = Run(Program, LanguageVersion.Latest, OutputKind.DynamicallyLinkedLibrary);
        Assert.That(generated, Is.Null);
    }

    // Before C# 10, nothing is generated so that the application still builds
    [TestCase(LanguageVersion.CSharp8)]
    [TestCase(LanguageVersion.CSharp9)]
    public void BeforeCSharp10_NothingGenerated(LanguageVersion version)
    {
        var (generated, diagnostics) = Run(Program, version);

        Assert.Multiple(() =>
        {
            Assert.That(generated, Is.Null);
            Assert.That(diagnostics, Is.Empty);
        });
    }

    // File-local types require C# 11
    [TestCase(LanguageVersion.CSharp10, "internal static class LocaleAlignment")]
    [TestCase(LanguageVersion.CSharp11, "file static class LocaleAlignment")]
    [TestCase(LanguageVersion.Latest, "file static class LocaleAlignment")]
    public void Application_InitializerGenerated(LanguageVersion version, string declaration)
    {
        var (generated, diagnostics) = Run(Program, version);

        Assert.Multiple(() =>
        {
            Assert.That(generated?.ToString(), Does.Contain(declaration));
            Assert.That(diagnostics, Is.Empty);
        });
    }

    // For frameworks before .NET 6, such as .NET Framework, the generated code is excluded entirely
    [Test]
    public void BeforeNet6_NothingCompiled()
    {
        var (generated, diagnostics) = Run(Program, LanguageVersion.Latest, symbols: []);

        Assert.Multiple(() =>
        {
            Assert.That(generated?.GetRoot().DescendantNodes(), Is.Empty);
            Assert.That(diagnostics, Is.Empty);
        });
    }

    //
    // The names used by the generated code bind to the System types regardless of
    // the application's global usings and the types in its global namespace.
    //

    [TestCase(LanguageVersion.CSharp10)]
    [TestCase(LanguageVersion.Latest)]
    public void NameConflicts_Compiles(LanguageVersion version)
    {
        const string Source = """
            global using MyApp.Util;

            class Program { static void Main() { } }

            static class LocaleAlignment { }
            static class LocaleAlignmentImpl { }

            namespace MyApp.Util
            {
                static class Environment { }
                static class Math { }
                static class OperatingSystem { }
                class CultureInfo { }
                class CultureNotFoundException { }
                class DateTimeFormatInfo { }
                class NumberFormatInfo { }
                enum StringComparison { }

                static class StringExtensions
                {
                    public static int AsSpan(this string value) => 0;
                }
            }
            """;

        var (generated, diagnostics) = Run(Source, version);

        Assert.Multiple(() =>
        {
            Assert.That(generated, Is.Not.Null);
            Assert.That(diagnostics, Is.Empty);
        });
    }

    private static (SyntaxTree? Generated, Diagnostic[] Diagnostics) Run(
        string source,
        LanguageVersion version,
        OutputKind kind = OutputKind.ConsoleApplication,
        string[]? symbols = null)
    {
        var options = new CSharpParseOptions(version, preprocessorSymbols: symbols ?? s_symbols);
        var compilation = CSharpCompilation.Create(
            "App",
            [CSharpSyntaxTree.ParseText(source, options)],
            s_references,
            new CSharpCompilationOptions(kind));

        var driver = CSharpGeneratorDriver
            .Create([new LocaleAlignmentGenerator().AsSourceGenerator()], parseOptions: options)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var errors);

        var diagnostics = errors
            .Concat(output.GetDiagnostics())
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();

        return (driver.GetRunResult().GeneratedTrees.SingleOrDefault(), diagnostics);
    }

    // The symbols the SDK defines for the current .NET version: NET5_0_OR_GREATER, NET6_0_OR_GREATER, etc.
    private static readonly string[] s_symbols = Enumerable
        .Range(5, Environment.Version.Major - 4)
        .Select(major => $"NET{major}_0_OR_GREATER")
        .ToArray();

    private static readonly MetadataReference[] s_references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path => Path.GetDirectoryName(path) == Path.GetDirectoryName(typeof(object).Assembly.Location))
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();
}
