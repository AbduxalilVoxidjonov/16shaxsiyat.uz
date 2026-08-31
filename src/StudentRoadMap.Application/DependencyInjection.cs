using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace StudentRoadMap.Application;

/// <summary>
/// `Application` qatlami servislarini DI konteyneriga ro'yxatdan o'tkazadi. Hozircha faqat
/// MediatR (`IPublisher`/`ISender`) — `AppDbContext.SaveChangesAsync` domen hodisalarini shu
/// orqali publish qiladi (P03). CQRS pipeline behavior'lari (`ValidationBehavior` va h.k.)
/// va FluentValidation ro'yxatdan o'tkazish keyingi promptlarda (use-case'lar bilan birga) qo'shiladi.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
