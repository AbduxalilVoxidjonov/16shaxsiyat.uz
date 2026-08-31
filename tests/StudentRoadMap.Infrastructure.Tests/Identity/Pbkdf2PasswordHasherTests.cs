using FluentAssertions;
using StudentRoadMap.Infrastructure.Identity;

namespace StudentRoadMap.Infrastructure.Tests.Identity;

/// <summary>
/// `Pbkdf2PasswordHasher` — superadmin paroli xeshlanadi (`docs/08`), tuz noyob, `Verify`
/// tamperlangan/yaroqsiz xeshlarda istisno otmasdan `false` qaytaradi (S4, QA topgan xavfsizlik
/// kamchiligi: downgrade/DoS himoyasi).
/// </summary>
public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify("Sup3rSecret!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify("BoshqaParol!", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_CalledTwiceWithSamePassword_ProducesDifferentHashes()
    {
        // Tasodifiy tuz ishlayapti — bir xil parol har safar boshqa xesh berishi kerak.
        var hash1 = _hasher.Hash("Sup3rSecret!");
        var hash2 = _hasher.Hash("Sup3rSecret!");

        hash1.Should().NotBe(hash2);
        _hasher.Verify("Sup3rSecret!", hash1).Should().BeTrue();
        _hasher.Verify("Sup3rSecret!", hash2).Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("1.2")]
    [InlineData("abc.def.ghi")]
    [InlineData("...")]
    public void Verify_WithMalformedHash_ReturnsFalseWithoutThrowing(string malformedHash)
    {
        var act = () => _hasher.Verify("Sup3rSecret!", malformedHash);

        act.Should().NotThrow();
        act().Should().BeFalse();
    }

    [Fact]
    public void Verify_WithBlankHash_ThrowsArgumentException()
    {
        var act = () => _hasher.Verify("Sup3rSecret!", " ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99_999)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(2_000_000_000)]
    public void Verify_WithTamperedIterationsOutsideAllowedRange_ReturnsFalseWithoutThrowing(int tamperedIterations)
    {
        // S4 (QA): downgrade (iteratsiya juda kam) va DoS (sun'iy katta iteratsiya) urinishi
        // istisno otmasdan `false` bilan rad etilishi kerak.
        var hash = _hasher.Hash("Sup3rSecret!");
        var parts = hash.Split('.', 3);
        var tamperedHash = $"{tamperedIterations}.{parts[1]}.{parts[2]}";

        var act = () => _hasher.Verify("Sup3rSecret!", tamperedHash);

        act.Should().NotThrow();
        act().Should().BeFalse();
    }

    [Fact]
    public void Hash_ThenTamperSingleCharacterInStoredHash_VerifyReturnsFalse()
    {
        var hash = _hasher.Hash("Sup3rSecret!");
        var parts = hash.Split('.', 3);
        var tamperedHashPart = parts[2][..^1] + (parts[2][^1] == 'A' ? 'B' : 'A');
        var tampered = $"{parts[0]}.{parts[1]}.{tamperedHashPart}";

        _hasher.Verify("Sup3rSecret!", tampered).Should().BeFalse();
    }
}
