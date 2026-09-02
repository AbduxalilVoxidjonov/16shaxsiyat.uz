using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>Testlar uchun superadmin (`AdminUser`) yaratuvchi yordamchi — haqiqiy `IPasswordHasher` orqali (DI'dan).</summary>
internal static class AdminTestDataFactory
{
    public const string DefaultPassword = "Sup3rSecretPwd!";

    public static async Task<AdminUser> CreateAdminUserAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        DateTimeOffset now,
        string username = "superadmin",
        string password = DefaultPassword,
        string? email = null,
        AdminRole role = AdminRole.SuperAdmin)
    {
        var admin = AdminUser.Create(Guid.NewGuid(), username, email ?? $"{username}@salohiyat.uz", hasher.Hash(password), now, role);
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        return admin;
    }

    /// <summary>`Set-Cookie` javob sarlavhasidan berilgan nomli cookie'ning `name=value` qismini ajratib oladi.</summary>
    public static string? ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        foreach (var cookie in cookies)
        {
            var namePart = cookie.Split(';', 2)[0];
            var separatorIndex = namePart.IndexOf('=');
            if (separatorIndex > 0 && namePart[..separatorIndex] == cookieName)
            {
                return namePart;
            }
        }

        return null;
    }
}
