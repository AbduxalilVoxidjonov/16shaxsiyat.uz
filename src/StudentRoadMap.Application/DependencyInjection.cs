using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Application.Common.Behaviors;
using StudentRoadMap.Application.Public.Common;

namespace StudentRoadMap.Application;

/// <summary>
/// `Application` qatlami servislarini DI konteyneriga ro'yxatdan o'tkazadi: MediatR
/// (`IPublisher`/`ISender`), CQRS pipeline behavior'lari (`docs/06-arxitektura.md` 4-bo'lim
/// — `ValidationBehavior` → `LoggingBehavior` → `TransactionBehavior` tartibida, ya'ni
/// validatsiya eng birinchi ishga tushadi, tranzaksiya esa handler'ni eng yaqindan o'raydi)
/// va FluentValidation validatorlari.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // `prompts/11`: ommaviy katalog keshi — `StartTest`/`GetTestQuestions` ikkalasi ham ishlatadi.
        services.AddScoped<PublicCatalogCache>();

        return services;
    }
}
