using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `GET /api/public/sessions/tests/{testCode}/questions` — `docs/07` 1.5-bo'lim, `prompts/11`.
/// Eng muhim talab: `scale`/`scaleDirection` (va shkala NOMI — `scaleNameUz`) javobda HECH
/// QACHON bo'lmasligi (`CLAUDE.md` 9-qoida).
/// </summary>
public sealed class PublicTestQuestionsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicTestQuestionsEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> StartSessionAsync(
        HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate, string languageCode = "uz")
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", "+998909998877", null, true, languageCode);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }

    [Fact]
    public async Task GetTestQuestions_BirinchiSahifa_ToGriShaklQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("q1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-q1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "Q1", 1, questionCount: 5, pageSize: 3);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Ergasheva Nilufar Rustamovna", new DateOnly(2010, 5, 5));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/Q1/start", UriKind.Relative), content: null);

        var response = await client.GetAsync(new Uri("/api/public/sessions/tests/Q1/questions?page=1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.TestCode.Should().Be("Q1");
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(3);
        body.TotalPages.Should().Be(2); // ceil(5/3)
        body.TotalQuestions.Should().Be(5);
        body.Questions.Should().HaveCount(3);
        body.Questions.Select(q => q.Order).Should().BeInAscendingOrder();
        body.Questions.All(q => q.Type == "Likert5").Should().BeTrue();
        body.Questions.All(q => q.CurrentValue == null).Should().BeTrue();
        body.ScaleLabels.Should().NotBeNull();
        body.ScaleLabels!.Should().HaveCount(5);
        body.ScaleLabels![0].Value.Should().Be(1);
        body.ScaleLabels![0].Label.Should().Be("Umuman qo'shilmayman");
    }

    /// <summary>
    /// `prompts/11` MAXSUS DIQQAT #1: swagger sxemasi va HAQIQIY javob tanasi ikkalasi ham
    /// `scale`/`scaleDirection` maydonlarini o'z ichiga olmasligi tekshiriladi. `scaleLabels`
    /// (docs/07 1.5-bo'lim namunasidagi qonuniy maydon) ataylab istisno qilinadi — tekshiruv
    /// aniq `"scale"`/`"scaleDirection"` JSON kalitlarini qidiradi, oddiy "scale" substring emas.
    /// </summary>
    [Fact]
    public async Task GetTestQuestions_JavobVaSwaggerda_ScaleMaydoniYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("q2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-q2", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "Q2", 1, questionCount: 3);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Sobirova Madina Alisherovna", new DateOnly(2010, 6, 6));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/Q2/start", UriKind.Relative), content: null);

        var response = await client.GetAsync(new Uri("/api/public/sessions/tests/Q2/questions?page=1", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rawBody = await response.Content.ReadAsStringAsync();

        rawBody.Should().NotContain("\"scale\"", "o'quvchi API javobida `scale` maydoni CLAUDE.md 9-qoidasi bo'yicha taqiqlangan");
        rawBody.Should().NotContain("\"scaleDirection\"", "o'quvchi API javobida `scaleDirection` maydoni CLAUDE.md 9-qoidasi bo'yicha taqiqlangan");
        // `scaleNameUz` (admin katalogidagi `CatalogQuestionItemDto` maydoni) `scale`dan ham
        // XAVFLIROQ: u o'lchanayotgan konstruktni OCHIQ aytadi ("Artistik"), ya'ni o'quvchi
        // javobini moslashtirib test natijasini buzishi mumkin. Ommaviy javobda bo'lmasligi
        // shu yerda qulflanadi.
        rawBody.Should().NotContain("\"scaleNameUz\"", "o'quvchi API javobida shkala NOMI ham taqiqlangan — u o'lchanayotgan konstruktni ochib beradi");
        rawBody.Should().NotContain("\"scaleDescriptionUz\"", "shkala TAVSIFI ham konstruktni ochib beradi — ommaviy javobda taqiqlangan");
        // `effectiveValue` (admin audit jadvalidagi TESKARI TUZATILGAN qiymat, 2026-09-03) —
        // `scale`dan ham xavfliroq: u savolning TESKARI ekanini ochib beradi, ya'ni o'quvchi
        // javobini moslashtirib natijani buzishi mumkin. Ommaviy javobda bo'lmasligi shu yerda
        // qulflanadi (`AdminAssessmentAnswerDto` izohiga qarang).
        rawBody.Should().NotContain("\"effectiveValue\"", "o'quvchi API javobida teskari tuzatilgan qiymat taqiqlangan — savolning teskari ekanini ochib beradi");
        rawBody.Should().Contain("\"scaleLabels\"", "docs/07 1.5-bo'lim namunasidagi qonuniy maydon hali ham mavjud bo'lishi kerak");

        // Swagger faqat Development/Staging'da ochiladi (`docs/07` 4-bo'lim) — `WebApplicationFactory`
        // sukut bo'yicha "Development" muhitida ishga tushadi. Tekshiruv FAQAT ommaviy javob
        // sxemasi (`PublicQuestionDto`) bilan CHEKLANGAN — butun hujjat EMAS: P37 (`prompts/37`)
        // dan boshlab admin katalog sxemalari (`CatalogQuestionItemDto` va h.k.) `scale`/
        // `scaleDirection`ni ATAYLAB o'z ichiga oladi (`docs/07` §3.4 — bu ADMIN CRUD uchun,
        // `CLAUDE.md` 9-qoidasi faqat O'QUVCHI API'siga tegishli). Butun hujjatni tekshirish
        // endi noto'g'ri musbat (false positive) berardi.
        var swaggerResponse = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var swaggerDocument = System.Text.Json.JsonDocument.Parse(await swaggerResponse.Content.ReadAsStreamAsync());

        var publicQuestionSchema = swaggerDocument.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("PublicQuestionDto")
            .GetRawText();

        publicQuestionSchema.Should().NotContain("\"scale\"", "ommaviy savol sxemasida `scale` maydoni CLAUDE.md 9-qoidasi bo'yicha taqiqlangan");
        publicQuestionSchema.Should().NotContain("\"scaleDirection\"", "ommaviy savol sxemasida `scaleDirection` maydoni CLAUDE.md 9-qoidasi bo'yicha taqiqlangan");
        publicQuestionSchema.Should().NotContain("\"scaleNameUz\"", "ommaviy savol sxemasida shkala NOMI ham taqiqlangan");
        publicQuestionSchema.Should().NotContain("\"scaleDescriptionUz\"", "ommaviy savol sxemasida shkala TAVSIFI ham taqiqlangan");
        publicQuestionSchema.Should().NotContain("\"effectiveValue\"", "ommaviy savol sxemasida teskari tuzatilgan qiymat taqiqlangan");

        // Butun ommaviy JAVOB sxemasi (`GetTestQuestionsResult`) ham tekshiriladi — maydon
        // `PublicQuestionDto` dan tashqarida, konvert darajasida paydo bo'lib qolmasin.
        var publicResultSchema = swaggerDocument.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("GetTestQuestionsResult")
            .GetRawText();

        publicResultSchema.Should().NotContain("\"scale\"", "ommaviy javob sxemasida `scale` taqiqlangan");
        publicResultSchema.Should().NotContain("\"scaleDirection\"", "ommaviy javob sxemasida `scaleDirection` taqiqlangan");
        publicResultSchema.Should().NotContain("\"scaleNameUz\"", "ommaviy javob sxemasida shkala NOMI taqiqlangan");
        publicResultSchema.Should().NotContain("\"effectiveValue\"", "ommaviy javob sxemasida teskari tuzatilgan qiymat taqiqlangan");
    }

    [Fact]
    public async Task GetTestQuestions_JavobBerilganSavolgaCurrentValueQoshiladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("q3");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-q3", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "Q3", 1, questionCount: 2);
        var firstQuestion = test.Questions.OrderBy(q => q.DisplayOrder).First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Tursunov Sherzod Baxromovich", new DateOnly(2010, 7, 7));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/Q3/start", UriKind.Relative), content: null);

        var saveRequest = new { answers = new[] { new { questionId = firstQuestion.Id, value = 4, durationMs = 1200 } } };
        var saveResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/Q3/answers", saveRequest, TestJson.Options);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync(new Uri("/api/public/sessions/tests/Q3/questions?page=1", UriKind.Relative));
        var body = await response.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);

        var answeredQuestion = body!.Questions.Single(q => q.Id == firstQuestion.Id);
        answeredQuestion.CurrentValue.Should().Be(4);

        var unansweredQuestion = body.Questions.Single(q => q.Id != firstQuestion.Id);
        unansweredQuestion.CurrentValue.Should().BeNull();
    }

    /// <summary>`prompts/11` DoD: aralashtirilgan tartib qayta so'rovda AYNAN bir xil bo'lishi shart.</summary>
    [Fact]
    public async Task GetTestQuestions_AralashtirilganTartib_QaytaSorovdaBirXil()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("q4");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-q4", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "Q4", 1, questionCount: 8, pageSize: 8, shuffleQuestions: true);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Bekova Dilnoza Farrux qizi", new DateOnly(2010, 8, 8));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        var startResponse = await client.PostAsync(new Uri("/api/public/sessions/tests/Q4/start", UriKind.Relative), content: null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = await client.GetAsync(new Uri("/api/public/sessions/tests/Q4/questions?page=1", UriKind.Relative));
        var firstBody = await first.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);

        var second = await client.GetAsync(new Uri("/api/public/sessions/tests/Q4/questions?page=1", UriKind.Relative));
        var secondBody = await second.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);

        var firstOrder = firstBody!.Questions.Select(q => q.Id).ToList();
        var secondOrder = secondBody!.Questions.Select(q => q.Id).ToList();

        secondOrder.Should().Equal(firstOrder, "ShuffleQuestions=true bo'lsa tartib bir marta generatsiya qilinib saqlanadi (prompts/11)");
    }

    /// <summary>
    /// QA topilmasi (2026-09-02): `Question.TextRu`/`TextEn` mavjud, lekin hech qachon seed
    /// qilinmagan. `LanguageCode = "ru"` bilan so'ralganda ham javob matni va `scaleLabels`
    /// mavjud (`uz`) to'plamga qaytishi (fallback) shart — bo'sh/null EMAS.
    /// </summary>
    [Fact]
    public async Task GetTestQuestions_RuTilidaMatnYoq_UzgaFallbackQiladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("q5");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-q5", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "Q5", 1, questionCount: 2);
        var firstQuestion = test.Questions.OrderBy(q => q.DisplayOrder).First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(
            client, school, accessToken, "Karimova Nodira Bahodirovna", new DateOnly(2010, 9, 9), languageCode: "ru");
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/Q5/start", UriKind.Relative), content: null);

        var response = await client.GetAsync(new Uri("/api/public/sessions/tests/Q5/questions?page=1", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);

        // `TextRu` seed qilinmagan — javob `TextUz`ga (domendagi haqiqiy qiymatga) teng bo'lishi kerak.
        body!.Questions.Single(q => q.Id == firstQuestion.Id).Text.Should().Be(firstQuestion.TextUz);

        body.ScaleLabels.Should().NotBeNull("uz to'plamiga fallback qilingani uchun bo'sh bo'lmasligi kerak");
        body.ScaleLabels!.Should().HaveCount(5);
        body.ScaleLabels![0].Label.Should().Be("Umuman qo'shilmayman");
    }

    /// <summary>
    /// QA topilmasi (2026-09-02) — "eng muhimi": kesh kaliti tilni o'z ichiga oladi. Ikki xil
    /// tilda (`uz`/`ru`) so'ralgan bir xil test uchun `PublicCatalogCache` ICHIDA IKKI ALOHIDA
    /// yozuv saqlanishini to'g'ridan-to'g'ri tekshiradi (`ICacheService` orqali) — aks holda
    /// birinchi so'ragan til keshni "egallab olib", boshqa til uchun ham xato matn qaytarardi.
    /// </summary>
    [Fact]
    public async Task GetTestQuestions_TilBoyichaKeshAlohidaSaqlaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("q6");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-q6", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "Q6", 1, questionCount: 2);

        using var client = _factory.CreateClient();
        var uzToken = await StartSessionAsync(client, school, accessToken, "Yoldoshev Bekzod Rustamovich", new DateOnly(2010, 9, 10), languageCode: "uz");
        var ruToken = await StartSessionAsync(client, school, accessToken, "Petrova Anastasiya Igorevna", new DateOnly(2010, 9, 11), languageCode: "ru");

        using (var uzClient = _factory.CreateClient())
        {
            uzClient.DefaultRequestHeaders.Add("X-Session-Token", uzToken);
            await uzClient.PostAsync(new Uri("/api/public/sessions/tests/Q6/start", UriKind.Relative), content: null);
            (await uzClient.GetAsync(new Uri("/api/public/sessions/tests/Q6/questions?page=1", UriKind.Relative)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var ruClient = _factory.CreateClient())
        {
            ruClient.DefaultRequestHeaders.Add("X-Session-Token", ruToken);
            await ruClient.PostAsync(new Uri("/api/public/sessions/tests/Q6/start", UriKind.Relative), content: null);
            (await ruClient.GetAsync(new Uri("/api/public/sessions/tests/Q6/questions?page=1", UriKind.Relative)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var uzKey = PublicCatalogCache.QuestionsCacheKey(test.Id, "uz");
        var ruKey = PublicCatalogCache.QuestionsCacheKey(test.Id, "ru");
        uzKey.Should().NotBe(ruKey, "kesh kaliti tilni o'z ichiga olishi kerak");

        cache.TryGet<IReadOnlyList<CachedQuestionDto>>(uzKey, out var uzCached).Should().BeTrue("uz uchun alohida kesh yozuvi bo'lishi kerak");
        cache.TryGet<IReadOnlyList<CachedQuestionDto>>(ruKey, out var ruCached).Should().BeTrue("ru uchun alohida kesh yozuvi bo'lishi kerak (uz bilan bir xil ro'yxat emas)");

        uzCached.Should().NotBeSameAs(ruCached, "ikki til ikki ALOHIDA kesh obyektiga ega bo'lishi kerak, bittasi ikkinchisini \"egallab olmasligi\" uchun");
    }

    [Fact]
    public async Task GetTestQuestions_TokenBerilmasa_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/sessions/tests/ANY/questions?page=1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
