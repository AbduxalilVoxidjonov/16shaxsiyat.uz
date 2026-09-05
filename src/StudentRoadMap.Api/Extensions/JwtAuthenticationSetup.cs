using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Api.Extensions;

/// <summary>
/// Superadmin JWT (Bearer) autentifikatsiyasini ommaviy `SessionToken` sxemasi QATOR (default
/// sxema o'zgarmaydi) qo'shadi — `docs/08-auth-va-xavfsizlik.md` 2-bo'lim. Claim nomlari
/// `JwtTokenService`dagi bilan bir xil (`sub`, `name`, `role`) — `MapInboundClaims = false`,
/// aks holda .NET standart claim'larni XML-schema URI'lariga avtomatik xaritalab, `[Authorize
/// (Roles = "SuperAdmin")]` "role" claim'ini topa olmay qoladi.
/// </summary>
public static class JwtAuthenticationSetup
{
    /// <summary>`[Authorize(Policy = SuperAdminPolicy)]` — barcha admin controller'lar uchun asos policy.</summary>
    public const string SuperAdminPolicy = "SuperAdmin";

    /// <summary>
    /// `[Authorize(Policy = PublicUserPolicy)]` — ommaviy (Telegram) foydalanuvchi kabineti
    /// (`/api/me/*`). ALOHIDA sxema (<see cref="PublicBearerScheme"/>) + rol talab qiladi.
    /// </summary>
    public const string PublicUserPolicy = "PublicUser";

    /// <summary>
    /// Ommaviy JWT uchun alohida autentifikatsiya sxemasi. `Bearer` (superadmin) sxemasi
    /// BUTUNLAY o'zgarishsiz qoladi — bu qo'shimcha sxema faqat `PublicUserPolicy` ichida
    /// tanlanadi. Ikkisining `TokenValidationParameters` lari faqat `ValidAudience` bilan
    /// farq qiladi (`Jwt:PublicAudience`), ya'ni bir sxemaga mo'ljallangan token boshqasida
    /// IMZO tekshiruvidan keyin `aud` mos kelmagani uchun rad etiladi.
    /// </summary>
    public const string PublicBearerScheme = "PublicBearer";

    public static AuthenticationBuilder AddAdminJwtBearer(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.MapInboundClaims = false;

            // Konfiguratsiya LAZY o'qiladi (`options.Events`/`TokenValidationParameters` shu
            // yopilishi ichida) — birinchi so'rovda, host to'liq qurilgach (test host'ining
            // qo'shimcha konfiguratsiyasi allaqachon qo'shilgan bo'ladi, `AddInfrastructure`
            // izohidagi bilan bir xil sabab). Kalit yo'q/qisqa bo'lsa — `IJwtTokenService`
            // `Program.cs`da START-UPda MAJBURIY resolve qilinib fail-fast beradi, shu sabab
            // bu yerga ilova production'da yaroqsiz kalit bilan yetib kelmaydi; shunga qaramay,
            // himoya sifatida bo'sh kalit bilan ham istisno otmasdan (auth shunchaki har doim
            // rad etadi) davom etadi.
            var key = configuration["Jwt:Key"];
            var keyBytes = string.IsNullOrWhiteSpace(key) ? new byte[32] : Encoding.UTF8.GetBytes(key);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "studentroadmap",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "studentroadmap-admin",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                RoleClaimType = "role",
                NameClaimType = "name",
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    return WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized, ProblemCodes.Unauthorized, "Kirish tokeni yaroqsiz yoki yo'q.");
                },
                OnForbidden = context =>
                    WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden, ProblemCodes.Forbidden, "Ushbu amal uchun ruxsat yetarli emas."),
            };
        });

        return builder;
    }

    /// <summary>
    /// Ommaviy (Telegram) foydalanuvchi JWT sxemasi — P47. `AddAdminJwtBearer` bilan bir xil
    /// tuzilish; farqi faqat sxema nomi va `ValidAudience`da. Rad javoblari ham bir xil
    /// `ProblemDetails` shaklida (`UNAUTHORIZED`/`FORBIDDEN`).
    /// </summary>
    public static AuthenticationBuilder AddPublicUserJwtBearer(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        builder.AddJwtBearer(PublicBearerScheme, options =>
        {
            options.MapInboundClaims = false;

            // Konfiguratsiya LAZY o'qiladi — `AddAdminJwtBearer` bilan bir xil sabab.
            var key = configuration["Jwt:Key"];
            var keyBytes = string.IsNullOrWhiteSpace(key) ? new byte[32] : Encoding.UTF8.GetBytes(key);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "studentroadmap",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:PublicAudience"] ?? PublicUserClaims.DefaultAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                RoleClaimType = "role",
                NameClaimType = "name",
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    return WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized, ProblemCodes.Unauthorized, "Kirish tokeni yaroqsiz yoki yo'q.");
                },
                OnForbidden = context =>
                    WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden, ProblemCodes.Forbidden, "Ushbu amal uchun ruxsat yetarli emas."),
            };
        });

        return builder;
    }

    public static IServiceCollection AddAdminAuthorizationPolicy(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // ⚠️ O'ZGARMAYDI — 242 integratsiya testi shu xatti-harakatga tayanadi.
            options.AddPolicy(SuperAdminPolicy, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(nameof(AdminRole.SuperAdmin)));

            // Ommaviy kabinet — IKKI qatlamli ajratish: (1) faqat `PublicBearer` sxemasi
            // (superadmin tokeni `aud` mos kelmagani uchun bu yerda umuman validatsiyadan
            // o'tmaydi), (2) rol `PublicUser` (`AdminRole` qiymatlaridan farqli).
            options.AddPolicy(PublicUserPolicy, policy => policy
                .AddAuthenticationSchemes(PublicBearerScheme)
                .RequireRole(PublicUserClaims.Role));
        });

        return services;
    }

    private static Task WriteProblemAsync(HttpContext httpContext, int status, string code, string title)
    {
        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://studentroadmap/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
