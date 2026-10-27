namespace Ramstack.LocaleAlignment.Tests;

[TestFixture]
[Platform(Exclude = "Win")]
public class RemoveUnsupportedScriptTests
{
    // No ICU data for the script
    [TestCase("be-Latn-BY", ExpectedResult = "be-BY")]
    [TestCase("be-Latn", ExpectedResult = "be")]
    [TestCase("tt-Latn-RU", ExpectedResult = "tt-RU")]

    // ICU data for the script
    [TestCase("sr-Latn-RS", ExpectedResult = "sr-Latn-RS")]
    [TestCase("sr-Latn-HR", ExpectedResult = "sr-Latn-HR")]
    [TestCase("uz-Cyrl-UZ", ExpectedResult = "uz-Cyrl-UZ")]
    [TestCase("uz-Arab-UZ", ExpectedResult = "uz-Arab-UZ")]
    [TestCase("ks-Deva-IN", ExpectedResult = "ks-Deva-IN")]
    [TestCase("uz-Latn", ExpectedResult = "uz-Latn")]

    // No script
    [TestCase("be-BY", ExpectedResult = "be-BY")]
    [TestCase("es-419", ExpectedResult = "es-419")]
    [TestCase("ca-ES-valencia", ExpectedResult = "ca-ES-valencia")]
    [TestCase("ca-valencia", ExpectedResult = "ca-valencia")]
    [TestCase("de-1901", ExpectedResult = "de-1901")]
    [TestCase("en-u-nu-thai", ExpectedResult = "en-u-nu-thai")]
    [TestCase("en-US-u-ca-buddhist", ExpectedResult = "en-US-u-ca-buddhist")]
    [TestCase("en-x-test", ExpectedResult = "en-x-test")]
    [TestCase("be", ExpectedResult = "be")]
    [TestCase("C", ExpectedResult = "C")]
    [TestCase("", ExpectedResult = "")]
    public string RemoveUnsupportedScript(string name) =>
        LocaleAlignmentImpl.RemoveUnsupportedScript(name);
}
