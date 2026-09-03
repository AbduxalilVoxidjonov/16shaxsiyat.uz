using MediatR;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>Barqaror vaqt — `AppDbContext`/`DbSeeder` uchun (`CLAUDE.md` 2-qoida: vaqt parametr bilan).</summary>
internal sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTime
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

/// <summary>Domen hodisalarini yutuvchi soxta MediatR publisher (`Infrastructure.Tests` bilan bir xil naqsh).</summary>
internal sealed class NoOpPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}

/// <summary>
/// Soxta parol xesher — bu yerda PBKDF2 ning o'zi sinalmaydi (u `Infrastructure.Tests` da),
/// faqat superadmin seed'ining IDEMPOTENTLIGI muhim. Haqiqiy `Pbkdf2PasswordHasher`
/// `internal` va faqat `StudentRoadMap.Infrastructure.Tests` ga ochilgan (`InternalsVisibleTo`),
/// `src/` esa bu vazifada o'zgartirilmaydi.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "fake-hash:";

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string hash) => hash == Prefix + password;
}
