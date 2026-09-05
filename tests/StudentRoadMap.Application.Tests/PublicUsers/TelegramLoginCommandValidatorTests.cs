using FluentAssertions;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;

namespace StudentRoadMap.Application.Tests.PublicUsers;

/// <summary>
/// `TelegramLoginCommandValidator` — faqat SHAKL tekshiruvi (haqiqiylik handler'da,
/// `LoginCommandValidator` bilan bir xil asos).
/// </summary>
public sealed class TelegramLoginCommandValidatorTests
{
    private const string ValidHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly TelegramLoginCommandValidator Validator = new();

    private static TelegramLoginCommand Command(long id = 12345, long authDate = 1767225600, string hash = ValidHash) =>
        new(id, authDate, hash);

    [Fact]
    public void ToGriShakl_Toġri()
    {
        Validator.Validate(Command()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TelegramIdMusbatEmas_Xato(long id)
    {
        var result = Validator.Validate(Command(id: id));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TelegramLoginCommand.Id));
    }

    [Fact]
    public void AuthDateMusbatEmas_Xato()
    {
        var result = Validator.Validate(Command(authDate: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TelegramLoginCommand.AuthDate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("qisqa")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void ImzoNotogriFormatda_Xato(string hash)
    {
        var result = Validator.Validate(Command(hash: hash));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TelegramLoginCommand.Hash));
    }

    [Fact]
    public void ImzoKattaHarfliHex_Toġri()
    {
        Validator.Validate(Command(hash: ValidHash.ToUpperInvariant())).IsValid.Should().BeTrue();
    }
}
