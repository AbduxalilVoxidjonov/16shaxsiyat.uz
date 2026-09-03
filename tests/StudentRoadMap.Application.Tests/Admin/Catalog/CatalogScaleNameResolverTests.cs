using FluentAssertions;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Admin.Catalog;

/// <summary>
/// `CatalogQuestionItemDto.ScaleNameUz` ni aniqlash tartibi (`docs/07` §3.4):
/// anketaning o'z `TestScale.NameUz` → tizim katalogi (`SystemScaleCatalog`) → `null`.
/// </summary>
public sealed class CatalogScaleNameResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TestScale Scale(Guid testId, string code, string nameUz, string? descriptionUz = null) =>
        TestScale.Create(Guid.NewGuid(), testId, code, nameUz, displayOrder: 0, descriptionUz: descriptionUz);

    [Fact]
    public void Custom_Anketada_NomTestScaleNameUzdanKeladi()
    {
        var testId = Guid.NewGuid();
        var resolver = CatalogScaleNameResolver.Create("SUM", [Scale(testId, "STRESS", "Stressga munosabat")]);

        resolver.Resolve("STRESS").Should().Be("Stressga munosabat");
    }

    [Fact]
    public void TizimMetodikasida_NomKatalogdanKeladi()
    {
        // Tizim metodikasida `TestScale` yozuvlari YO'Q (`docs/07` §3.4) — nom faqat katalogdan.
        var resolver = CatalogScaleNameResolver.Create("RIASEC", []);

        resolver.Resolve("ART").Should().Be("Artistik");
        resolver.Resolve("CONV").Should().Be("Konvensional");
    }

    [Fact]
    public void AnketaningOzShkalasi_KatalogdanUstunTuradi()
    {
        // Bazadagi haqiqat har doim ustun: superadmin `RIASEC` kodli strategiyaga tegmaydi,
        // lekin nusxa olingan anketada o'z shkalasi bo'lsa, aynan o'sha nom ko'rsatiladi.
        var testId = Guid.NewGuid();
        var resolver = CatalogScaleNameResolver.Create("RIASEC", [Scale(testId, "ART", "Ijodkorlik (moslashtirilgan)")]);

        resolver.Resolve("ART").Should().Be("Ijodkorlik (moslashtirilgan)");
    }

    [Theory]
    [InlineData("SUM", "XYZ")]
    [InlineData("RIASEC", "XYZ")]
    [InlineData(null, "ART")]
    public void NomaLumKod_NullQaytaradi_Yiqilmaydi(string? strategyCode, string scaleCode)
    {
        var resolver = CatalogScaleNameResolver.Create(strategyCode, []);

        resolver.Resolve(scaleCode).Should().BeNull();
    }

    [Fact]
    public void BoshKod_NullQaytaradi()
    {
        var resolver = CatalogScaleNameResolver.Create("RIASEC", []);

        resolver.Resolve(null).Should().BeNull();
        resolver.Resolve("   ").Should().BeNull();
    }

    /// <summary>
    /// Tavsif NOM bilan BIR XIL manbadan keladi — aks holda bitta shkala bir joydan nom,
    /// boshqasidan tavsif olib, chalkash juftlik hosil qilardi.
    /// </summary>
    [Fact]
    public void Tavsif_NomBilanBirXilManbadanKeladi()
    {
        var testId = Guid.NewGuid();

        // Tizim metodikasi — `docs/03` §5.1 jadvalidagi "nima o'lchaydi" ustuni.
        CatalogScaleNameResolver.Create("ACTIVITY", []).ResolveDescription("MOT")
            .Should().Be("O'qishga ichki qiziqish, maqsad aniqligi");

        // `docs/03` da tavsifi yo'q shkala — `null`.
        CatalogScaleNameResolver.Create("ACTIVITY", []).ResolveDescription("SOCA").Should().NotBeNull();
        CatalogScaleNameResolver.Create("MBTI16", []).ResolveDescription("EI").Should().BeNull();

        // Anketaning o'z shkalasi ustun — nom ham, tavsif ham o'sha yozuvdan.
        var resolver = CatalogScaleNameResolver.Create("ACTIVITY", [Scale(testId, "MOT", "O'z nomim", "O'z tavsifim")]);
        resolver.Resolve("MOT").Should().Be("O'z nomim");
        resolver.ResolveDescription("MOT").Should().Be("O'z tavsifim");

        // Noma'lum kod — tavsif ham `null`.
        resolver.ResolveDescription("XYZ").Should().BeNull();
    }

    [Fact]
    public void None_HechQachonNomBermaydi()
    {
        CatalogScaleNameResolver.None.Resolve("ART").Should().BeNull();
        CatalogScaleNameResolver.None.ResolveDescription("ART").Should().BeNull();
    }

    [Fact]
    public void ForTest_AgregatningStrategiyaKodiVaShkalalaridanQuriladi()
    {
        var testId = Guid.NewGuid();
        var question = Question.Create(Guid.NewGuid(), testId, "MB-Q01", 1, "Savol", QuestionType.Likert5, "EI", 1, 1.0m, isSystem: true);
        var test = TestDefinition.CreateSystemPublished(
            testId,
            "MBTI16",
            "16 tipli shaxsiyat modeli",
            null,
            displayOrder: 1,
            estimatedMinutes: 9,
            shuffleQuestions: false,
            pageSize: 10,
            scoringStrategyCode: "MBTI16",
            questions: [question],
            now: Now);

        CatalogScaleNameResolver.ForTest(test).Resolve("EI").Should().Be("Ekstraversiya/Introversiya");
    }
}
