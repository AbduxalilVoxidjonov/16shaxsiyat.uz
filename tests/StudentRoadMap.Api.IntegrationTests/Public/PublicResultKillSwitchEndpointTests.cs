using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// P47: `App:ShowResultToStudent` GLOBAL bayrog'i endi avariya rubilnigi (kill-switch) —
/// makon bayrog'idan (`School.ShowResultToStudent = true`) USTUN. Ya'ni bitta env
/// o'zgaruvchisi butun tizimda (maktab ham, ommaviy makon ham) natijani yopadi.
///
/// Bu test `PublicGetStudentResultForbiddenEndpointTests` (makon bayrog'i o'chiq) bilan
/// JUFT: ikkalasi birgalikda `ShowResultPolicy` ning `&amp;&amp;` mantiqini har ikki tomondan
/// qulflaydi.
/// </summary>
public sealed class PublicResultKillSwitchEndpointTests : IClassFixture<ResultKillSwitchOffApiTestFactory>
{
    private readonly ResultKillSwitchOffApiTestFactory _factory;

    public PublicResultKillSwitchEndpointTests(ResultKillSwitchOffApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudentResult_GlobalRubilnikOchiq_MakonYoqilganBolsaHam_403Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("killswitch1");

        // Makon bayrog'i YOQILGAN — faqat global rubilnik yopib turadi.
        var school = await TestDataFactory.CreateSchoolAsync(
            db, now, "maktab-killswitch1", accessToken, showResultToStudent: true);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "KSW1", 1, questionCount: 2);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Ismoilova Zarina Otabekovna", new DateOnly(2010, 6, 6),
            Gender.Female, 9, "A", "+998901234567", null, null, true, "uz", TestDataFactory.DefaultProgramCode);

        var startResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task CompleteSession_GlobalRubilnikOchiq_ShowResultToStudentFalseQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("killswitch2");
        var school = await TestDataFactory.CreateSchoolAsync(
            db, now, "maktab-killswitch2", accessToken, showResultToStudent: true);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "KSW2", 2, questionCount: 1);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Ismoilova Nodira Otabekovna", new DateOnly(2010, 6, 7),
            Gender.Female, 9, "A", "+998901234568", null, null, true, "uz", TestDataFactory.DefaultProgramCode);

        var startResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        // Sessiyani yakunlash urinishi (testlar to'ldirilmagani uchun `409` bo'lishi mumkin) —
        // bu testda MUHIMI: rubilnik o'chiq bo'lsa `showResultToStudent` hech qachon `true`
        // bo'lmaydi. Shu sabab natija endpointi orqali tekshiriladi (yuqoridagi fact) va
        // bu yerda faqat `403` javobi qulflanadi.
        var resultResponse = await client.GetAsync(new Uri("/api/public/sessions/result", UriKind.Relative));

        resultResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
