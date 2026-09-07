using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// `POST /api/me/sessions` — BR-1/BR-5 **`(o'quvchi, dastur)` juftligi** bo'yicha (`docs/07`
/// §5.4, egasining qarori 2026-09-07). Ommaviy makonga ikki `Public` dastur (A, B)
/// biriktirilgan; har test ALOHIDA Telegram akkaunti (→ alohida `Student`) bilan ishlaydi.
/// Bu fixture'da `TestDataFactory.CreatePublishedTestAsync` chaqirilmaydi — u standart
/// dasturni ham yaratib, dasturlar sonini o'zgartirib yuborardi.
/// </summary>
public sealed class StartPublicSessionPerProgramEndpointTests : IClassFixture<TelegramApiTestFactory>
{
    private const string ProgramCodeA = "PUBPP-A";
    private const string ProgramCodeB = "PUBPP-B";
    private const string TestCodeA = "PUBPPA";
    private const string TestCodeB = "PUBPPB";

    private readonly TelegramApiTestFactory _factory;

    public StartPublicSessionPerProgramEndpointTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureSeededAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);

        // Sinf ichidagi testlar ketma-ket ishlaydi (xUnit) — bitta marta yetarli.
        if (await db.AssessmentPrograms.AnyAsync(p => p.Code == ProgramCodeA))
        {
            return;
        }

        var testA = await TestDataFactory.CreateStandaloneTestAsync(db, now, TestCodeA, 1, questionCount: 1);
        var testB = await TestDataFactory.CreateStandaloneTestAsync(db, now, TestCodeB, 2, questionCount: 1);
        await TestDataFactory.CreateProgramAsync(db, now, ProgramCodeA, [(testA.Id, 1)]);
        await TestDataFactory.CreateProgramAsync(db, now, ProgramCodeB, [(testB.Id, 1)]);
    }

    private static object Body(string? programCode) => new
    {
        fullName = "Perprogram Ommaviy Foydalanuvchi",
        birthDate = new DateOnly(1995, 4, 12),
        gender = nameof(Gender.Male),
        phone = "+998901234567",
        consentAccepted = true,
        parentalConsent = false,
        grade = (int?)null,
        languageCode = "uz",
        programCode,
    };

    private async Task<HttpClient> AuthenticatedClientAsync(long telegramId)
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();
        var (accessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, telegramId);
        client.UseBearer(accessToken);
        return client;
    }

    private static async Task<StartSessionResult> StartAsync(HttpClient client, string? programCode, HttpStatusCode expected)
    {
        var response = await client.PostAsJsonAsync("/api/me/sessions", Body(programCode), TestJson.Options);
        response.StatusCode.Should().Be(expected);
        return (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
    }

    private async Task CompleteAsync(Guid assessmentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await AssessmentCompletionHelper.CompleteAsync(db, assessmentId, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task StartSession_ADasturYakunlangan_AQayta_409DuplicateAssessment()
    {
        using var client = await AuthenticatedClientAsync(720300001);

        var first = await StartAsync(client, ProgramCodeA, HttpStatusCode.Created);
        await CompleteAsync(first.AssessmentId);

        var response = await client.PostAsJsonAsync("/api/me/sessions", Body(ProgramCodeA), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("DUPLICATE_ASSESSMENT");
    }

    [Fact]
    public async Task StartSession_ADasturYakunlangan_BDastur_201YangiSessiya()
    {
        using var client = await AuthenticatedClientAsync(720300002);

        var first = await StartAsync(client, ProgramCodeA, HttpStatusCode.Created);
        await CompleteAsync(first.AssessmentId);

        var second = await StartAsync(client, ProgramCodeB, HttpStatusCode.Created);

        second.Resumed.Should().BeFalse("boshqa dastur — takror emas");
        second.AssessmentId.Should().NotBe(first.AssessmentId);
        second.Tests.Should().ContainSingle(t => t.Code == TestCodeB);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstRow = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == first.AssessmentId);
        var secondRow = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == second.AssessmentId);
        secondRow.StudentId.Should().Be(firstRow.StudentId, "bitta akkaunt — bitta `Student` profili");
    }

    [Fact]
    public async Task StartSession_ADaYarimQolgan_AQayta_200Resumed()
    {
        using var client = await AuthenticatedClientAsync(720300003);

        var first = await StartAsync(client, ProgramCodeA, HttpStatusCode.Created);

        var second = await StartAsync(client, ProgramCodeA, HttpStatusCode.OK);

        second.Resumed.Should().BeTrue();
        second.AssessmentId.Should().Be(first.AssessmentId);
    }

    [Fact]
    public async Task StartSession_ADaYarimQolgan_BDastur_201YangiVaKabinetIkkalasiniKoradi()
    {
        using var client = await AuthenticatedClientAsync(720300004);

        var first = await StartAsync(client, ProgramCodeA, HttpStatusCode.Created);

        var second = await StartAsync(client, ProgramCodeB, HttpStatusCode.Created);

        second.Resumed.Should().BeFalse("A dagi yarim qolgan sessiya B ni 'davom ettirish' deb qaytarilmaydi");
        second.AssessmentId.Should().NotBe(first.AssessmentId);
        second.Tests.Should().ContainSingle(t => t.Code == TestCodeB);

        // Kabinet (`GET /api/me/assessments`, `docs/07` §5.2) har sessiyani ALOHIDA qaytaradi —
        // ikkala tugallanmagan sessiya ham ro'yxatda, mijoz qaysi birini davom ettirishni tanlaydi.
        var list = await client.GetAsync(new Uri("/api/me/assessments", UriKind.Relative));
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().ToList();

        var unfinishedIds = items
            .Where(i => i.GetProperty("status").GetString() is nameof(AssessmentStatus.Draft) or nameof(AssessmentStatus.InProgress))
            .Select(i => i.GetProperty("id").GetGuid())
            .ToList();

        unfinishedIds.Should().BeEquivalentTo([first.AssessmentId, second.AssessmentId]);
    }

    /// <summary>Tartib: dastur tanlash sessiya holatidan OLDIN — yarim qolgan sessiya bo'lsa ham `programCode` shart.</summary>
    [Fact]
    public async Task StartSession_YarimQolganBorLekinProgramCodeYoq_400ProgramRequired()
    {
        using var client = await AuthenticatedClientAsync(720300005);

        await StartAsync(client, ProgramCodeA, HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync("/api/me/sessions", Body(programCode: null), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("PROGRAM_REQUIRED");
    }
}
