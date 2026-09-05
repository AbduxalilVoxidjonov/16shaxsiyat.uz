using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Identity;

namespace StudentRoadMap.Infrastructure.Tests.Identity;

/// <summary>
/// P47: ommaviy (Telegram) foydalanuvchi access tokeni. Eng muhim invariantlar —
/// (1) `aud` superadminникidan FARQ qiladi, (2) rol `PublicUser`,
/// (3) tokenda hech qanday shaxsiy ma'lumot (`name`) yo'q.
/// </summary>
public sealed class JwtTokenServicePublicUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static IConfiguration BuildConfiguration(string? publicAudience = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "studentroadmap",
                ["Jwt:Audience"] = "studentroadmap-admin",
                ["Jwt:PublicAudience"] = publicAudience,
            })
            .Build();

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void CreatePublicUserAccessToken_SubVaRolniYozadi()
    {
        var service = new JwtTokenService(BuildConfiguration());
        var publicUserId = Guid.NewGuid();

        var token = Read(service.CreatePublicUserAccessToken(publicUserId, Now).Token);

        token.Claims.Should().ContainSingle(c => c.Type == "sub" && c.Value == publicUserId.ToString());
        token.Claims.Should().ContainSingle(c => c.Type == "role" && c.Value == PublicUserClaims.Role);
        token.Claims.Should().Contain(c => c.Type == "jti");
    }

    [Fact]
    public void CreatePublicUserAccessToken_ShaxsiyMalumotClaimQoshmaydi()
    {
        var service = new JwtTokenService(BuildConfiguration());

        var token = Read(service.CreatePublicUserAccessToken(Guid.NewGuid(), Now).Token);

        token.Claims.Should().NotContain(
            c => c.Type == "name",
            "tokenga ism/username tushmasligi kerak (`CLAUDE.md` 5-qoida ruhida)");
    }

    [Fact]
    public void CreatePublicUserAccessToken_SuperadminAudienceSidanFarqQiladi()
    {
        var service = new JwtTokenService(BuildConfiguration());
        var admin = AdminUser.Create(Guid.NewGuid(), "superadmin", "admin@16shaxsiyat.uz", "hashed", Now);

        var publicToken = Read(service.CreatePublicUserAccessToken(Guid.NewGuid(), Now).Token);
        var adminToken = Read(service.CreateAccessToken(admin, Now).Token);

        publicToken.Audiences.Should().ContainSingle().Which.Should().Be(PublicUserClaims.DefaultAudience);
        adminToken.Audiences.Should().ContainSingle().Which.Should().Be("studentroadmap-admin");
        publicToken.Audiences.Should().NotIntersectWith(adminToken.Audiences, "ikki auditoriya `aud` bilan ajratiladi");
    }

    [Fact]
    public void CreatePublicUserAccessToken_SozlanganPublicAudienceniIshlatadi()
    {
        var service = new JwtTokenService(BuildConfiguration(publicAudience: "shaxsiyat-kabinet"));

        var token = Read(service.CreatePublicUserAccessToken(Guid.NewGuid(), Now).Token);

        token.Audiences.Should().ContainSingle().Which.Should().Be("shaxsiyat-kabinet");
    }

    [Fact]
    public void CreatePublicUserAccessToken_BoshIdentifikatorBilan_IstisnoOtadi()
    {
        var service = new JwtTokenService(BuildConfiguration());

        var act = () => service.CreatePublicUserAccessToken(Guid.Empty, Now);

        act.Should().Throw<ArgumentException>();
    }
}
