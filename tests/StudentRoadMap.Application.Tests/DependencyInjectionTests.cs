using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.StartSession;

namespace StudentRoadMap.Application.Tests;

/// <summary>
/// `AddApplication()` DI ro'yxatga olishini tekshiradi. Bu regressiyaga qarshi test —
/// `StartSessionCommandValidator`ning `internal` bo'lganida `AddValidatorsFromAssembly`
/// uni topmasligini (FluentValidation.DependencyInjectionExtensions 12.1.1 xatti-harakati)
/// haqiqiy integratsiya testi orqali aniqlab, `public`ga o'zgartirilgan edi (izohga qarang).
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_PipelineBehaviorlarniOchiqGenerikSifatidaRoyxatdanOtkazadi()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        var behaviorImplementations = services
            .Where(d => d.ServiceType.IsGenericTypeDefinition && d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType?.Name)
            .ToList();

        behaviorImplementations.Should().BeEquivalentTo(
            ["ValidationBehavior`2", "LoggingBehavior`2", "TransactionBehavior`2"]);
    }

    [Fact]
    public void AddApplication_FluentValidationValidatorlarniTopadi()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IDateTime>(new FakeDateTime(DateTimeOffset.UtcNow));

        using var provider = services.BuildServiceProvider();

        var validators = provider.GetServices<IValidator<StartSessionCommand>>().ToList();

        validators.Should().ContainSingle(v => v.GetType() == typeof(StartSessionCommandValidator));
    }

    private sealed class FakeDateTime : IDateTime
    {
        public FakeDateTime(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
