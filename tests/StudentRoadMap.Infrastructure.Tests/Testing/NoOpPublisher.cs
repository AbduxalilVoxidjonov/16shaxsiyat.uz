using MediatR;

namespace StudentRoadMap.Infrastructure.Tests.Testing;

/// <summary>
/// Domen hodisalarini hech narsa qilmasdan yutuvchi soxta MediatR publisher —
/// `AppDbContext.SaveChangesAsync` har safar `SaveChanges`dan keyin eventlarni publish qiladi,
/// lekin `DbSeeder` sinovlari uchun bu eventlar ahamiyatsiz (Catalog agregatlari hodisa
/// ko'tarmaydi, lekin interfeys baribir kerak).
/// </summary>
internal sealed class NoOpPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}
