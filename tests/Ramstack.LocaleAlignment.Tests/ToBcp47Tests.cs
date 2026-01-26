namespace Ramstack.LocaleAlignment.Tests;

[TestFixture]
public class ToBcp47Tests
{
    [TestCase("uz_UZ.UTF-8@latin", ExpectedResult = "uz-Latn-UZ")]
    [TestCase("uz_UZ.UTF-8@cyrillic", ExpectedResult = "uz-Cyrl-UZ")]
    [TestCase("uz_UZ.UTF-8@arabic", ExpectedResult = "uz-Arab-UZ")]

    [TestCase("uz.UTF-8@latin", ExpectedResult = "uz-Latn")]
    [TestCase("uz.UTF-8@cyrillic", ExpectedResult = "uz-Cyrl")]
    [TestCase("uz.UTF-8@arabic", ExpectedResult = "uz-Arab")]

    [TestCase("uz_UZ@latin", ExpectedResult = "uz-Latn-UZ")]
    [TestCase("uz_UZ.UTF-8", ExpectedResult = "uz-UZ")]

    [TestCase("uz@latin", ExpectedResult = "uz-Latn")]
    [TestCase("uz.UTF-8", ExpectedResult = "uz")]

    [TestCase("uz", ExpectedResult = "uz")]
    [TestCase("uz_UZ", ExpectedResult = "uz-UZ")]
    [TestCase("", ExpectedResult = "")]

    [TestCase("en_IE.UTF-8@euro", ExpectedResult = "en-IE")]
    [TestCase("en_IE@euro", ExpectedResult = "en-IE")]
    [TestCase("en_IE.UTF-8", ExpectedResult = "en-IE")]
    [TestCase("en.UTF-8@euro", ExpectedResult = "en")]
    [TestCase("en@euro", ExpectedResult = "en")]
    [TestCase("en.UTF-8", ExpectedResult = "en")]

    [TestCase("ca_ES.UTF-8@valencia", ExpectedResult = "ca-ES-valencia")]
    [TestCase("ca_ES@valencia", ExpectedResult = "ca-ES-valencia")]
    [TestCase("ca.UTF-8@valencia", ExpectedResult = "ca-valencia")]
    [TestCase("ca@valencia", ExpectedResult = "ca-valencia")]

    [TestCase("sr_RS.ISO-8859-5@cyrillic", ExpectedResult = "sr-Cyrl-RS")]
    [TestCase("sr_RS.ISO-8859-5", ExpectedResult = "sr-RS")]
    [TestCase("sr.ISO-8859-5@cyrillic", ExpectedResult = "sr-Cyrl")]
    [TestCase("sr.ISO-8859-5", ExpectedResult = "sr")]

    [TestCase("sr.UTF-8@latin9", ExpectedResult = "sr")]
    [TestCase("sr_RS.UTF-8@latin9", ExpectedResult = "sr-RS")]
    public string ToBcp47(string posix) =>
        CultureInfoHelper.ToBcp47(posix);
}
