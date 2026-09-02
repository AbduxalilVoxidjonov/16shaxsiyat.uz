using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Infrastructure.Common;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Security;

namespace StudentRoadMap.Infrastructure;

/// <summary>
/// `Infrastructure` qatlami servislarini DI konteyneriga ro'yxatdan o'tkazadi
/// (`docs/06-arxitektura.md` 2-bo'limi).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Ulanish satri LAZY o'qiladi (options lambdasi ichida, `AddInfrastructure` chaqirilganda
        // emas) — `IConfiguration` (`ConfigurationManager`) jonli obyekt, keyinroq qo'shiladigan
        // manbalar (masalan, `WebApplicationFactory.ConfigureAppConfiguration` — sinov host'i)
        // shu tufayli ko'rinadi. Eager o'qish bu integratsiya sinovlarini buzadi.
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException(
                    "'ConnectionStrings:Postgres' sozlamasi topilmadi. Env o'zgaruvchi yoki user-secrets orqali bering.");

            options
                .UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(30))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAsyncQueryExecutor, EfAsyncQueryExecutor>();
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IIpHasher, IpHasher>();
        services.AddSingleton<IAppSettings, AppSettingsProvider>();
        // `prompts/12`: AI navbati hozircha yo'q — `NoOpJobQueue` kontraktni bajaradi, P18 da almashadi.
        services.AddSingleton<IBackgroundJobQueue, NoOpJobQueue>();
        services.AddScoped<DbSeeder>();

        // `prompts/11`: ommaviy katalog keshi (10 daqiqa) va savollarni aralashtirish abstraksiyasi.
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IQuestionShuffler, RandomQuestionShuffler>();

        // `/health/ready` DB ulanishini tekshiradi — `ready` tag bilan ajratilib,
        // Api/Program.cs da alohida endpoint sifatida ochiladi (`/health` esa DB'siz jonlik).
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "postgres", tags: ["ready"]);

        return services;
    }
}
