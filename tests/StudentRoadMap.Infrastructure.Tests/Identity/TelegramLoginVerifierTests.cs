using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Infrastructure.Identity;

namespace StudentRoadMap.Infrastructure.Tests.Identity;

/// <summary>
/// `TelegramLoginVerifier` — Telegram rasmiy imzo algoritmi (`docs/08` 2a-bo'lim).
///
/// Kutilgan imzo bu yerda MUSTAQIL (ishlab chiqarish kodidan nusxa OLMASDAN, hujjatdagi
/// ta'rifga qarab) hisoblanadi — aks holda algoritmdagi xato ikkala tomonda bir xil bo'lib,
/// test yashil qolib ketardi.
/// </summary>
public sealed class TelegramLoginVerifierTests
{
    private const string BotToken = "1234567890:TEST-ONLY-BOT-TOKEN";

    private static TelegramLoginVerifier CreateVerifier(string? botToken = BotToken) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Telegram:BotToken"] = botToken })
            .Build());

    private static Dictionary<string, string> Fields(long id = 12345, string firstName = "Ali", string? username = "alivali") =>
        new(StringComparer.Ordinal)
        {
            ["id"] = id.ToString(CultureInfo.InvariantCulture),
            ["auth_date"] = "1767225600",
            ["first_name"] = firstName,
            ["username"] = username ?? "",
        };

    /// <summary>Telegram hujjatidagi ta'rifning to'g'ridan-to'g'ri tarjimasi.</summary>
    private static string ExpectedHash(IReadOnlyDictionary<string, string> fields, string botToken)
    {
        var dataCheckString = string.Join(
            '\n',
            fields.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));

        return Convert.ToHexString(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString))).ToLowerInvariant();
    }

    [Fact]
    public void IsConfigured_TokenBerilgan_True()
    {
        CreateVerifier().IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_TokenBerilmagan_False()
    {
        CreateVerifier(botToken: null).IsConfigured.Should().BeFalse();
        CreateVerifier(botToken: "   ").IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Verify_ToGriImzo_True()
    {
        var fields = Fields();

        CreateVerifier().Verify(fields, ExpectedHash(fields, BotToken)).Should().BeTrue();
    }

    [Fact]
    public void Verify_KattaHarfliHexImzo_True()
    {
        var fields = Fields();
        var hash = ExpectedHash(fields, BotToken).ToUpperInvariant();

        CreateVerifier().Verify(fields, hash).Should().BeTrue("hex katta/kichik harfda kelishi mumkin");
    }

    [Fact]
    public void Verify_MaydonOzgartirilgan_False()
    {
        var fields = Fields(firstName: "Ali");
        var hash = ExpectedHash(fields, BotToken);

        fields["first_name"] = "Vali";

        CreateVerifier().Verify(fields, hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_QoshimchaMaydonQoshilgan_False()
    {
        var fields = Fields();
        var hash = ExpectedHash(fields, BotToken);

        fields["photo_url"] = "https://t.me/i/a.jpg";

        CreateVerifier().Verify(fields, hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_BoshqaBotTokeniBilanImzolangan_False()
    {
        var fields = Fields();
        var hash = ExpectedHash(fields, "9999:BOSHQA-BOT-TOKEN");

        CreateVerifier().Verify(fields, hash).Should().BeFalse();
    }

    /// <summary>`hash` maydoni `data_check_string`ga KIRMASLIGI kerak (Telegram algoritmi).</summary>
    [Fact]
    public void Verify_HashMaydoniMalumotToPlamidaBolsaHam_EtiborsizQoldiriladi()
    {
        var fields = Fields();
        var hash = ExpectedHash(fields, BotToken);

        var withHash = new Dictionary<string, string>(fields, StringComparer.Ordinal) { ["hash"] = hash };

        CreateVerifier().Verify(withHash, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_SozlanmaganTokenBilan_False()
    {
        var fields = Fields();

        CreateVerifier(botToken: null).Verify(fields, ExpectedHash(fields, BotToken)).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zzzz")]
    [InlineData("0123")]
    public void Verify_YaroqsizImzoSatri_IstisnoOtmasdanFalse(string hash)
    {
        var act = () => CreateVerifier().Verify(Fields(), hash);

        act.Should().NotThrow();
        CreateVerifier().Verify(Fields(), hash).Should().BeFalse();
    }
}
