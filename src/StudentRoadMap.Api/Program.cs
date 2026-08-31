using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StudentRoadMap.Application;
using StudentRoadMap.Infrastructure;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// --- Servislar ---------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails(options =>
{
    // RFC 9457 formatiga mos `code` maydoni uchun joy (docs/06-arxitektura.md, 6-bo'lim).
    // Konkret `code` qiymatlari keyingi promptlarda ExceptionHandlingMiddleware orqali to'ldiriladi.
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
    };
});

var frontendUrl = builder.Configuration["App:FrontendUrl"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            policy.WithOrigins(frontendUrl)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// --- `--migrate`: migratsiyani bajarib ilovani to'xtatadi (docs/05 4-bo'lim: production'da
// `Database.Migrate()` avtomatik emas — alohida step/konteyner) ---------------------------
if (args.Contains("--migrate"))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

// --- `--seed`: test bankini/tip katalogini/kasb xaritasini/superadminni yuklab ilovani
// to'xtatadi (`docs/05` 4-bo'lim: seed migratsiyaga qo'yilmaydi, idempotent `DbSeeder`)
if (args.Contains("--seed"))
{
    using var seedScope = app.Services.CreateScope();
    var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
    return;
}

// `App:SeedOnStartup=true` bo'lsa har ishga tushishda idempotent seed bajariladi
// (masalan, docker-compose'da birinchi marta ko'tarilganda).
if (app.Configuration.GetValue<bool>("App:SeedOnStartup"))
{
    using var startupSeedScope = app.Services.CreateScope();
    var startupSeeder = startupSeedScope.ServiceProvider.GetRequiredService<DbSeeder>();
    await startupSeeder.SeedAsync();
}

// --- HTTP quvuri ---------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();

// Konteyner/orkestrator uchun jonlik va tayyorlik tekshiruvlari — ikkisi `tag` bilan ajratilgan:
// `/health` — DB'siz jonlik (hech qanday tekshiruv bajarilmaydi), `/health/ready` — faqat
// `ready` tegli tekshiruvlar (hozircha DB ulanishi, `AddDbContextCheck` orqali).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

// Integratsiya testlari (WebApplicationFactory) uchun ochiq qilinadi.
public partial class Program;
