using FluentAssertions;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.GetTypeCatalog;
using StudentRoadMap.Application.Tests.Public.Testing;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `GetTypeCatalogQueryHandler` — ommaviy `/metodika` sahifasining 16 tip bo'limi
/// (`docs/07` 1.10-bo'lim). DB/HTTP'siz sinov: soxta `IAppDbContext` + LINQ-to-Objects
/// bajaruvchi + xotiradagi kesh (`GetSchoolInfoQueryHandlerTests` bilan bir xil ruhda).
/// </summary>
public sealed class GetTypeCatalogQueryHandlerTests
{
    /// <summary>`SeedData/type-catalog.json` dagi 16 kod — seeder testidagi (`DbSeederTests`) kutilgan son bilan bir xil.</summary>
    private static readonly string[] AllCodes =
    [
        "ISTJ", "ISFJ", "INFJ", "INTJ",
        "ISTP", "ISFP", "INFP", "INTP",
        "ESTP", "ESFP", "ENFP", "ENTP",
        "ESTJ", "ESFJ", "ENFJ", "ENTJ",
    ];

    [Fact]
    public async Task Handle_KatalogToliq_16TaYozuvniQaytaradi()
    {
        var (handler, context, _) = CreateHandler(BuildCatalog(AllCodes));

        var result = await handler.Handle(new GetTypeCatalogQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Types.Should().HaveCount(16, "ommaviy sahifada 16 tipning HAR BIRI bo'lishi kerak (loyiha egasining talabi)");
        result.Value.Types.Select(t => t.Code).Should().BeEquivalentTo(AllCodes);
        context.TypeCatalogReadCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_KodBoyichaBarqarorTartibdaQaytaradi()
    {
        // Kirish tartibi ATAYLAB aralash — javob tartibi kirishga emas, `Code` ga bog'liq
        // bo'lishi kerak (mijozdagi "oldingi/keyingi tip" navigatsiyasi shunga tayanadi).
        var (handler, _, _) = CreateHandler(BuildCatalog(["INTJ", "ENFP", "ISTJ", "ESFJ"]));

        var result = await handler.Handle(new GetTypeCatalogQuery(), CancellationToken.None);

        result.Value.Types.Select(t => t.Code).Should().ContainInOrder("ENFP", "ESFJ", "INTJ", "ISTJ");
    }

    [Fact]
    public async Task Handle_HarBirYozuvningKontentMaydonlariToliqQaytadi()
    {
        var (handler, _, _) = CreateHandler(BuildCatalog(["INTJ"]));

        var result = await handler.Handle(new GetTypeCatalogQuery(), CancellationToken.None);

        var type = result.Value.Types.Single();
        type.Name.Should().Be("INTJ nomi");
        type.ShortDescription.Should().Be("INTJ qisqa tavsifi");
        type.LongDescription.Should().Be("INTJ to'liq tavsifi");
        type.Strengths.Should().Equal("INTJ kuchli tomoni");
        type.GrowthAreas.Should().Equal("INTJ o'sish yo'nalishi");
        type.CareerHints.Should().Equal("INTJ kasb maslahati");
    }

    [Fact]
    public async Task Handle_IkkinchiChaqiruv_BazagaQaytaBormaydi()
    {
        var (handler, context, cache) = CreateHandler(BuildCatalog(AllCodes));

        var first = await handler.Handle(new GetTypeCatalogQuery(), CancellationToken.None);
        var second = await handler.Handle(new GetTypeCatalogQuery(), CancellationToken.None);

        first.Value.Types.Should().HaveCount(16);
        second.Value.Types.Should().HaveCount(16);
        context.TypeCatalogReadCount.Should().Be(1, "katalog keshlanadi — ikkinchi chaqiruv `type_catalog` ga so'rov yubormasligi kerak");
        cache.WrittenKeys.Should().ContainSingle().Which.Should().Be(PublicCatalogCache.TypeCatalogCacheKey());
    }

    [Fact]
    public async Task Handle_KatalogBosh_XatoEmasBoshRoyxatQaytaradi()
    {
        // Seed qilinmagan baza — bu ommaviy marketing bo'limi, sahifa YIQILMASLIGI kerak.
        var (handler, _, _) = CreateHandler([]);

        var result = await handler.Handle(new GetTypeCatalogQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Types.Should().BeEmpty();
    }

    private static (GetTypeCatalogQueryHandler Handler, FakeTypeCatalogAppDbContext Context, InMemoryCacheService Cache) CreateHandler(
        IReadOnlyList<TypeCatalogEntry> entries)
    {
        var context = new FakeTypeCatalogAppDbContext(entries);
        var cache = new InMemoryCacheService();
        var catalogCache = new PublicCatalogCache(context, new TypeCatalogInlineAsyncQueryExecutor(), cache);

        return (new GetTypeCatalogQueryHandler(catalogCache), context, cache);
    }

    /// <summary>
    /// Sinov uchun tuzilgan yozuvlar — HAQIQIY kontent (`type-catalog.json`) bu yerda
    /// takrorlanmaydi: handler matnni o'zgartirmaydi, faqat uzatadi, shu sabab tekshiruv
    /// uchun kod bo'yicha hosil qilingan o'rinbosar matn yetarli.
    /// </summary>
    private static List<TypeCatalogEntry> BuildCatalog(IEnumerable<string> codes) =>
        [.. codes.Select(code => TypeCatalogEntry.Create(
            code,
            $"{code} nomi",
            $"{code} qisqa tavsifi",
            $"{code} to'liq tavsifi",
            [$"{code} kuchli tomoni"],
            [$"{code} o'sish yo'nalishi"],
            [$"{code} kasb maslahati"]))];
}
