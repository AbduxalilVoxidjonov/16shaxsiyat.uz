using FluentAssertions;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Admin.Catalog;

/// <summary>
/// <b>Drift qo'riqchisi.</b> <see cref="SystemScaleCatalog"/> tizim metodikalarining shkala
/// nomlarini beradi, lekin haqiqiy shkala kodlari SEED JSON'ida yashaydi
/// (`Infrastructure/Persistence/SeedData/test-definitions/*.json` — har savolda `"scale": "…"`).
/// Ikki joy bir-biridan uzilib qolsa, admin katalogida nom JIMGINA yo'qoladi (`null`) yoki
/// ortiqcha nom qolib ketadi.
///
/// <para>
/// Shu sabab kutilgan kodlar bu yerda QO'LDA YOZILMAYDI — ular seed fayllaridan o'qiladi.
/// Qo'lda yozilsa qo'riqchining o'zi uchinchi manba bo'lib, o'zi drift bo'lardi.
/// Seed'ga yangi shkala qo'shilsa yoki kod o'zgarsa — shu test darhol yiqiladi va katalogni
/// yangilash kerakligini aytadi.
/// </para>
///
/// <para>
/// Kalit — `ScoringStrategyCode`. Tizim metodikasi uchun u seed'da `dto.Code` dan olinadi
/// (`SeedDataLoader.ToDomainSystemTestDefinition`: `scoringStrategyCode: dto.Code`), shu sabab
/// test ham aynan shu manbadan foydalanadi, alohida ro'yxatdan emas.
/// </para>
/// </summary>
public sealed class SystemScaleCatalogDriftTests
{
    private static string SeedDirectory =>
        Path.Combine(AppContext.BaseDirectory, "SeedData", "test-definitions");

    private static IReadOnlyList<TestDefinitionSeedDto> SeededSystemTests() =>
        Directory.EnumerateFiles(SeedDirectory, "*.json")
            .OrderBy(file => file, StringComparer.Ordinal)
            .Select(file => SeedDataLoader.ParseTestDefinition(File.ReadAllText(file), Path.GetFileName(file)))
            .ToList();

    public static TheoryData<string> SeededStrategyCodes()
    {
        var data = new TheoryData<string>();
        foreach (var dto in SeededSystemTests())
        {
            data.Add(dto.Code);
        }

        return data;
    }

    /// <summary>
    /// Har bir tizim metodikasi uchun katalog seed'dagi shkala kodlarini AYNAN qoplaydi —
    /// kam ham emas, ortiq ham emas.
    /// </summary>
    [Theory]
    [MemberData(nameof(SeededStrategyCodes))]
    public void ForStrategy_SeedKodlariniAynanQoplaydi(string strategyCode)
    {
        var seeded = SeededSystemTests().Single(dto => string.Equals(dto.Code, strategyCode, StringComparison.Ordinal));

        var seedScaleCodes = seeded.Questions
            .Select(q => q.Scale)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        var catalogScaleCodes = SystemScaleCatalog.ForStrategy(strategyCode)
            .Select(scale => scale.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        catalogScaleCodes.Should().Equal(
            seedScaleCodes,
            "`{0}` katalogi seed JSON'idagi shkala kodlari bilan aynan mos bo'lishi kerak (docs/03)",
            strategyCode);
    }

    /// <summary>Katalogda seed'da umuman yo'q metodika (yoki aksincha) qolib ketmasin.</summary>
    [Fact]
    public void StrategyCodes_SeeddagiMetodikalarBilanAynanMos()
    {
        var seedCodes = SeededSystemTests()
            .Select(dto => dto.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        SystemScaleCatalog.StrategyCodes
            .OrderBy(code => code, StringComparer.Ordinal)
            .Should().Equal(seedCodes, "katalog aynan seed'dagi tizim metodikalarini qoplaydi");
    }

    /// <summary>Har bir shkalada nom bo'lishi shart — bo'sh nom kodni ko'rsatishdan yomonroq.</summary>
    [Theory]
    [MemberData(nameof(SeededStrategyCodes))]
    public void ForStrategy_HarBirShkalaNomiToLdirilgan(string strategyCode)
    {
        foreach (var scale in SystemScaleCatalog.ForStrategy(strategyCode))
        {
            scale.NameUz.Should().NotBeNullOrWhiteSpace($"`{strategyCode}.{scale.Code}` shkalasining nomi docs/03 da bor");
            scale.NameUz.Should().NotBe(scale.Code, "nom kodning takrori bo'lmasligi kerak");
        }
    }

    /// <summary>
    /// Tanilmagan strategiya (`SUM`, superadmin anketasi) — bo'sh ro'yxat va `null` nom,
    /// istisno EMAS.
    /// </summary>
    [Theory]
    [InlineData("SUM")]
    [InlineData("NOMALUM")]
    [InlineData(null)]
    [InlineData("")]
    public void TanilmaganStrategiya_BoshRoYxatVaNullNom(string? strategyCode)
    {
        SystemScaleCatalog.ForStrategy(strategyCode).Should().BeEmpty();
        SystemScaleCatalog.FindNameUz(strategyCode, "ART").Should().BeNull();
        SystemScaleCatalog.Find(strategyCode, "ART").Should().BeNull();
    }

    /// <summary>Tanilgan strategiya + noma'lum kod → `null` (yiqilmaydi).</summary>
    [Fact]
    public void TanilganStrategiya_NomaLumKod_NullQaytaradi()
    {
        SystemScaleCatalog.FindNameUz("RIASEC", "XYZ").Should().BeNull();
        SystemScaleCatalog.FindNameUz("RIASEC", null).Should().BeNull();
        SystemScaleCatalog.FindNameUz("RIASEC", "art").Should().BeNull("kod registrga sezgir (`Ordinal`)");
    }

    /// <summary>
    /// `docs/03` §4.1 dagi eng ko'p chalkashtiradigan kod — egasi aynan shundan shikoyat qilgan.
    /// Nom o'zgarsa test yiqiladi (nom foydalanuvchiga ko'rinadigan shartnoma).
    /// </summary>
    [Theory]
    [InlineData("RIASEC", "ART", "Artistik")]
    [InlineData("RIASEC", "CONV", "Konvensional")]
    [InlineData("ACTIVITY", "SOCA", "Ijtimoiy faollik")]
    [InlineData("MBTI16", "EI", "Ekstraversiya/Introversiya")]
    [InlineData("BIG5", "O", "Ochiqlik")]
    public void FindNameUz_Docs03dagiNomniQaytaradi(string strategyCode, string scaleCode, string expectedName)
    {
        SystemScaleCatalog.FindNameUz(strategyCode, scaleCode).Should().Be(expectedName);
    }
}
