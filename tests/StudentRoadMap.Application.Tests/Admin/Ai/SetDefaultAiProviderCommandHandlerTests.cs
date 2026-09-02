using FluentAssertions;
using StudentRoadMap.Application.Admin.Ai.SetDefaultProvider;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Tests.Admin.Ai;

/// <summary>`SetDefaultAiProviderCommandHandler` — `docs/07` §3.5 "POST .../set-default"; `AiProviderConfig.IsDefault` invarianti (`docs/04` §2.9: faqat bittasi).</summary>
public sealed class SetDefaultAiProviderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private static SetDefaultAiProviderCommandHandler CreateHandler(FakeAiAppDbContext context) =>
        new(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeEncryptionServiceForAiTests(), new FakeIpHasher());

    private static AiProviderConfig ActiveKeyedConfig(AiProvider provider)
    {
        var config = AiProviderConfig.Create(Guid.NewGuid(), provider, provider.ToString(), "model", Now);
        config.SetApiKey("enc:key", Now);
        config.Activate(Now);
        return config;
    }

    [Fact]
    public async Task Handle_SwitchingDefault_UnmarksPreviousAndMarksNew()
    {
        var context = new FakeAiAppDbContext();
        var oldDefault = ActiveKeyedConfig(AiProvider.Gemini);
        oldDefault.MarkAsDefault(Now);
        var newDefault = ActiveKeyedConfig(AiProvider.Anthropic);
        context.ProviderConfigList.Add(oldDefault);
        context.ProviderConfigList.Add(newDefault);

        var handler = CreateHandler(context);
        var result = await handler.Handle(new SetDefaultAiProviderCommand(AiProvider.Anthropic, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        oldDefault.IsDefault.Should().BeFalse();
        newDefault.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TargetNotConfigured_ReturnsNotFound()
    {
        var context = new FakeAiAppDbContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SetDefaultAiProviderCommand(AiProvider.OpenAi, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ProblemCodes.NotFound);
    }

    [Fact]
    public async Task Handle_TargetInactive_ReturnsValidationError()
    {
        var context = new FakeAiAppDbContext();
        var inactive = AiProviderConfig.Create(Guid.NewGuid(), AiProvider.OpenAi, "OpenAI", "model", Now);
        inactive.SetApiKey("enc:key", Now); // kaliti bor, lekin faol emas
        context.ProviderConfigList.Add(inactive);

        var handler = CreateHandler(context);
        var result = await handler.Handle(new SetDefaultAiProviderCommand(AiProvider.OpenAi, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_TargetWithoutApiKey_ReturnsValidationError()
    {
        var context = new FakeAiAppDbContext();
        var noKey = AiProviderConfig.Create(Guid.NewGuid(), AiProvider.OpenAi, "OpenAI", "model", Now);
        noKey.Activate(Now); // faol, lekin kaliti yo'q
        context.ProviderConfigList.Add(noKey);

        var handler = CreateHandler(context);
        var result = await handler.Handle(new SetDefaultAiProviderCommand(AiProvider.OpenAi, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }
}
