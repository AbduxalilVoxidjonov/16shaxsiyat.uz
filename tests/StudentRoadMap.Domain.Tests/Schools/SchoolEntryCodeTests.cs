using FluentAssertions;
using StudentRoadMap.Domain.Events;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Domain.Tests.Schools;

/// <summary>
/// Maktab kodi (`School.EntryCode`, `SchoolEntryCode`) — `docs/08` 3a-bo'lim: format, alifbo,
/// normalizatsiya, ommaviy makonda `null`, qayta yaratish.
/// </summary>
public sealed class SchoolEntryCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

    private static School CreateSchool(string entryCode = "ABCD2345") => School.Create(
        Guid.NewGuid(),
        "12-son maktab",
        "Farg'ona",
        "Qo'qon",
        SchoolSlug.Create("12-son-maktab-qoqon").Value,
        "initial-access-token",
        entryCode,
        Now);

    [Fact]
    public void Alphabet_ChalkashBelgilarsiz_31Belgi()
    {
        SchoolEntryCode.Alphabet.Should().HaveLength(31);
        SchoolEntryCode.Alphabet.Should().NotContainAny("0", "O", "1", "I", "L");
        SchoolEntryCode.Alphabet.Distinct().Should().HaveCount(31, "alifboda takror belgi bo'lmasligi kerak");
        SchoolEntryCode.Alphabet.Should().MatchRegex("^[A-Z2-9]+$");
        SchoolEntryCode.Length.Should().Be(8);
    }

    [Theory]
    [InlineData("ABCD2345", true)]
    [InlineData("ZZZZ9999", true)]
    [InlineData("abcd2345", false)] // kichik harf — saqlash shakli emas (Normalize orqali kelishi kerak)
    [InlineData("ABCD-2345", false)] // defis saqlanmaydi
    [InlineData("ABCD234", false)]
    [InlineData("ABCD23456", false)]
    [InlineData("ABCD0123", false)] // 0 va 1 alifboda yo'q
    [InlineData("ABCDOILX", false)] // O, I, L alifboda yo'q
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_SaqlashShakliniTekshiradi(string? code, bool expected)
    {
        SchoolEntryCode.IsValid(code).Should().Be(expected);
    }

    [Theory]
    [InlineData("7K3M-9XQ2", "7K3M9XQ2")]
    [InlineData("7k3m9xq2", "7K3M9XQ2")]
    [InlineData("  7k3m 9xq2 ", "7K3M9XQ2")]
    [InlineData("7K3M–9XQ2", "7K3M9XQ2")] // en-dash (telefon klaviaturasi)
    [InlineData("7K3M9XQ2", "7K3M9XQ2")]
    public void Normalize_KattaHarfDefisBoshliq_YagonaShakl(string input, string expected)
    {
        SchoolEntryCode.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("7K3M9XQ")] // qisqa
    [InlineData("7K3M9XQ22")] // uzun
    [InlineData("7K3M-9XQ2-A")] // uzun (defis bilan)
    [InlineData("7K3M9XQ0")] // alifbodan tashqari
    [InlineData("7K3M9XQ!")]
    public void Normalize_YaroqsizKirish_Null(string? input)
    {
        SchoolEntryCode.Normalize(input).Should().BeNull();
    }

    [Fact]
    public void Format_DefisBilanKorsatadi_SaqlashShakliniOzgartirmaydi()
    {
        SchoolEntryCode.Format("7K3M9XQ2").Should().Be("7K3M-9XQ2");
        // Yaroqsiz qiymat o'zgarishsiz — ko'rsatishda hech qachon istisno otilmaydi.
        SchoolEntryCode.Format("bad").Should().Be("bad");
    }

    [Fact]
    public void Create_Maktab_EntryCodeToldirilgan()
    {
        var school = CreateSchool("7K3M9XQ2");

        school.Kind.Should().Be(SchoolKind.School);
        school.EntryCode.Should().Be("7K3M9XQ2");
        // `AccessCode` (sinf kodi) bu bilan ALOQASIZ — standart holatda `null` qoladi.
        school.AccessCode.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcd2345")]
    [InlineData("ABCD-2345")]
    [InlineData("ABCD012")]
    public void Create_YaroqsizEntryCode_ArgumentException(string entryCode)
    {
        var act = () => CreateSchool(entryCode);

        act.Should().Throw<ArgumentException>().WithParameterName("entryCode");
    }

    [Fact]
    public void CreatePublicSpace_EntryCodeNull()
    {
        var space = School.CreatePublicSpace(Guid.NewGuid(), "Ommaviy makon", SchoolSlug.FromExisting("ommaviy"), "token", Now);

        space.Kind.Should().Be(SchoolKind.PublicSpace);
        space.EntryCode.Should().BeNull("ommaviy makonga kod bilan kirish yo'q — faqat Telegram");
    }

    [Fact]
    public void RegenerateEntryCode_YangiKod_EskisiAlmashadi_EventKotariladi()
    {
        var school = CreateSchool("ABCD2345");
        var later = Now.AddHours(1);

        school.RegenerateEntryCode("WXYZ6789", later);

        school.EntryCode.Should().Be("WXYZ6789");
        school.UpdatedAt.Should().Be(later);
        school.DomainEvents.Should().ContainSingle(e => e is SchoolEntryCodeRegeneratedEvent)
            .Which.Should().BeOfType<SchoolEntryCodeRegeneratedEvent>()
            .Which.SchoolId.Should().Be(school.Id);
    }

    [Fact]
    public void RegenerateEntryCode_YaroqsizKod_ArgumentException_EskiKodQoladi()
    {
        var school = CreateSchool("ABCD2345");

        var act = () => school.RegenerateEntryCode("abcd-2345", Now);

        act.Should().Throw<ArgumentException>();
        school.EntryCode.Should().Be("ABCD2345");
    }

    [Fact]
    public void RegenerateEntryCode_OmmaviyMakonda_DomainException()
    {
        var space = School.CreatePublicSpace(Guid.NewGuid(), "Ommaviy makon", SchoolSlug.FromExisting("ommaviy"), "token", Now);

        var act = () => space.RegenerateEntryCode("ABCD2345", Now);

        act.Should().Throw<Domain.Common.DomainException>();
        space.EntryCode.Should().BeNull();
    }

    [Fact]
    public void RegenerateAccessToken_EntryCodeniOzgartirmaydi()
    {
        var school = CreateSchool("ABCD2345");

        school.RegenerateAccessToken("new-token", Now.AddMinutes(5));

        school.EntryCode.Should().Be("ABCD2345", "havola va kod — MUSTAQIL sirlar, biri yangilansa ikkinchisi qoladi");
    }
}
