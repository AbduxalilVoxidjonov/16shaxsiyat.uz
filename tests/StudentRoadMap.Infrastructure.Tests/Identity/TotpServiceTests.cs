using FluentAssertions;
using StudentRoadMap.Infrastructure.Identity;

namespace StudentRoadMap.Infrastructure.Tests.Identity;

/// <summary>
/// `TotpService` — RFC 6238 (30 s qadam, ±1 oyna) va qayta ishlatishga qarshi himoya
/// (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 5-band).
/// </summary>
public sealed class TotpServiceTests
{
    private readonly TotpService _service = new();

    private static long StepOf(DateTimeOffset now) => now.ToUnixTimeSeconds() / 30;

    [Fact]
    public void GenerateSecret_ProducesNonEmptyBase32String()
    {
        var secret = _service.GenerateSecret();

        secret.Should().NotBeNullOrWhiteSpace();
        secret.Should().MatchRegex("^[A-Z2-7]+$");
    }

    [Fact]
    public void GenerateSecret_CalledTwice_ProducesDifferentSecrets()
    {
        _service.GenerateSecret().Should().NotBe(_service.GenerateSecret());
    }

    [Fact]
    public void BuildOtpauthUri_ContainsSecretIssuerAndAccount()
    {
        var uri = _service.BuildOtpauthUri("JBSWY3DPEHPK3PXP", "superadmin", "Shaxsiyat");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("secret=JBSWY3DPEHPK3PXP");
        uri.Should().Contain("issuer=Shaxsiyat");
    }

    [Fact]
    public void TryValidate_WithCodeForCurrentStep_ReturnsTrueAndMatchedStep()
    {
        var secret = _service.GenerateSecret();
        var now = DateTimeOffset.UtcNow;
        var code = ComputeCodeForTest(secret, StepOf(now));

        var valid = _service.TryValidate(secret, code, now, lastUsedStep: null, out var matchedStep);

        valid.Should().BeTrue();
        matchedStep.Should().Be(StepOf(now));
    }

    [Fact]
    public void TryValidate_WithCodeOneStepInPastOrFuture_ReturnsTrue()
    {
        var secret = _service.GenerateSecret();
        var now = DateTimeOffset.UtcNow;
        var pastCode = ComputeCodeForTest(secret, StepOf(now) - 1);

        _service.TryValidate(secret, pastCode, now, lastUsedStep: null, out _).Should().BeTrue();
    }

    [Fact]
    public void TryValidate_WithCodeTwoStepsAway_ReturnsFalse()
    {
        var secret = _service.GenerateSecret();
        var now = DateTimeOffset.UtcNow;
        var farCode = ComputeCodeForTest(secret, StepOf(now) - 2);

        _service.TryValidate(secret, farCode, now, lastUsedStep: null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_WithWrongCode_ReturnsFalse()
    {
        var secret = _service.GenerateSecret();

        _service.TryValidate(secret, "000000", DateTimeOffset.UtcNow, lastUsedStep: null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_WithAlreadyUsedStep_ReturnsFalse_ReplayProtection()
    {
        var secret = _service.GenerateSecret();
        var now = DateTimeOffset.UtcNow;
        var step = StepOf(now);
        var code = ComputeCodeForTest(secret, step);

        // Qadam allaqachon qabul qilingan (lastUsedStep == step) — qayta ishlatish rad etiladi.
        var valid = _service.TryValidate(secret, code, now, lastUsedStep: step, out _);

        valid.Should().BeFalse();
    }

    [Fact]
    public void TryValidate_WithMalformedCode_ReturnsFalseWithoutThrowing()
    {
        var secret = _service.GenerateSecret();

        var act = () => _service.TryValidate(secret, "abc", DateTimeOffset.UtcNow, lastUsedStep: null, out _);

        act.Should().NotThrow();
        act().Should().BeFalse();
    }

    /// <summary>
    /// RFC 4226 HOTP'ni mustaqil (`TotpService`ning o'ziga bog'liq bo'lmagan) qayta hisoblaydi
    /// — faqat Base32 dekodlash uchun `Base32.Decode` (internal, `InternalsVisibleTo` orqali
    /// ko'rinadi) qayta ishlatiladi.
    /// </summary>
    private static string ComputeCodeForTest(string secret, long step)
    {
        var secretBytes = Base32.Decode(secret);
        var counter = new byte[8];
        var value = step;
        for (var i = counter.Length - 1; i >= 0; i--)
        {
            counter[i] = (byte)(value & 0xFF);
            value >>= 8;
        }

        var hash = System.Security.Cryptography.HMACSHA1.HashData(secretBytes, counter);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);
        var otp = binary % 1_000_000;

        return otp.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }
}
