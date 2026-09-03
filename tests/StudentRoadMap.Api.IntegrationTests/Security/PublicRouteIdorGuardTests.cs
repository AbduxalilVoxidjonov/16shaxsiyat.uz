using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Security;

/// <summary>
/// P31 (`CLAUDE.md` 8-qoida): "Ommaviy API'da ID qabul qilinmaydi — faqat `X-Session-Token`".
///
/// Mavjud testlar (`PublicSaveAnswersEndpointTests` — B sessiyasi A ning javoblarini ko'ra
/// olmaydi) IDOR ni XATTI-HARAKAT darajasida qulflaydi. Bu test esa STRUKTURA darajasida:
/// marshrut shablonining O'ZIDA hech qanday ID parametri paydo bo'lmasligini tekshiradi —
/// ya'ni kelajakda kimdir `GET /api/public/assessments/{assessmentId}` qo'shsa, hech qanday
/// xatti-harakat testi yozilmagan bo'lsa ham, shu yerda darhol qizil bo'ladi.
///
/// `questionId` kabi maydonlar TANADA qoladi va bu qoidani buzmaydi: ular egalikni
/// aniqlamaydi (savol allaqachon sessiyaga tegishli test ichidan olinadi), egalik esa
/// faqat `X-Session-Token` orqali aniqlanadi.
/// </summary>
public sealed class PublicRouteIdorGuardTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicRouteIdorGuardTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private List<RouteEndpoint> PublicRouteEndpoints()
    {
        _ = _factory.Server;

        return [.. _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("api/public", StringComparison.OrdinalIgnoreCase) == true)];
    }

    [Fact]
    public async Task OmmaviyMarshrutlar_HechQandayIdParametriniQabulQilmaydi()
    {
        await Task.CompletedTask;
        var endpoints = PublicRouteEndpoints();
        endpoints.Should().NotBeEmpty("ommaviy endpointlar topilishi kerak — aks holda test hech nimani tekshirmaydi");

        foreach (var endpoint in endpoints)
        {
            var idParameters = endpoint.RoutePattern.Parameters
                .Select(parameter => parameter.Name)
                .Where(name => name.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                .ToList();

            idParameters.Should().BeEmpty(
                "`{0}` marshruti ID qabul qilmasligi kerak (`CLAUDE.md` 8-qoida) — egalik faqat `X-Session-Token` orqali aniqlanadi",
                endpoint.RoutePattern.RawText);
        }
    }

    /// <summary>
    /// Sessiyaga tegishli har bir ommaviy endpoint AYNAN `SessionToken` sxemasini talab qiladi
    /// — ya'ni admin JWT bilan ommaviy sessiya ma'lumotiga kirib bo'lmaydi va aksincha.
    /// Autentifikatsiyasiz ikkitasi (`schools/{slug}` va sessiya ochish) bundan mustasno.
    /// </summary>
    [Fact]
    public async Task SessiyaEndpointlari_FaqatSessionTokenSxemasiniTalabQiladi()
    {
        await Task.CompletedTask;
        var sessionEndpoints = PublicRouteEndpoints()
            .Where(endpoint => endpoint.RoutePattern.RawText!.StartsWith("api/public/sessions/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        sessionEndpoints.Should().NotBeEmpty();

        foreach (var endpoint in sessionEndpoints)
        {
            var authorize = endpoint.Metadata.GetMetadata<IAuthorizeData>();

            authorize.Should().NotBeNull("`{0}` autentifikatsiyasiz qolmasligi kerak", endpoint.RoutePattern.RawText);
            authorize!.AuthenticationSchemes.Should().Be(
                SessionTokenAuthenticationHandler.SchemeName,
                "`{0}` faqat o'quvchi sessiya sxemasini qabul qilishi kerak",
                endpoint.RoutePattern.RawText);
        }
    }
}
