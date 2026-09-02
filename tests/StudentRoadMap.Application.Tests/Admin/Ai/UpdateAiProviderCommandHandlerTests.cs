using FluentAssertions;
using StudentRoadMap.Application.Admin.Ai.UpdateProvider;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Tests.Admin.Ai;

/// <summary>`UpdateAiProviderCommandHandler` — `docs/07` §3.5 "PUT .../providers/{provider}" (upsert, kalit shifrlash, faollashtirish qo'riqchisi).</summary>
public sealed class UpdateAiProviderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private static UpdateAiProviderCommandHandler CreateHandler(FakeAiAppDbContext context) =>
        new(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeEncryptionServiceForAiTests(), new FakeIpHasher());

    [Fact]
    public async Task Handle_NoExistingConfig_CreatesNewInactiveConfigAndAuditLog()
    {
        var context = new FakeAiAppDbContext();
        var handler = CreateHandler(context);
        var command = new UpdateAiProviderCommand(AiProvider.Gemini, "gemini-2.0-flash", 4096, 0.4m, IsActive: false, FallbackOrder: 10, ApiKey: null, BaseUrl: null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.ProviderConfigList.Should().ContainSingle();
        context.ProviderConfigList[0].Model.Should().Be("gemini-2.0-flash");
        context.ProviderConfigList[0].IsActive.Should().BeFalse();
        context.AuditLogList.Should().ContainSingle(a => a.Action == AuditActions.AiConfigUpdated);
        // Kalit hech qachon berilmagan — `AiConfig.KeyChanged` YOZILMASLIGI kerak.
        context.AuditLogList.Should().NotContain(a => a.Action == AuditActions.AiConfigKeyChanged);
    }

    [Fact]
    public async Task Handle_ApiKeyProvided_EncryptsKeyAndLogsKeyChanged()
    {
        var context = new FakeAiAppDbContext();
        var handler = CreateHandler(context);
        var command = new UpdateAiProviderCommand(AiProvider.Gemini, "gemini-2.0-flash", 4096, 0.4m, IsActive: true, FallbackOrder: 10, ApiKey: "raw-secret-key", BaseUrl: null, Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);

        var config = context.ProviderConfigList.Single();
        config.ApiKeyEncrypted.Should().Be("enc:raw-secret-key", "haqiqiy AES o'rniga soxta shifrlash — faqat 'shifrlanadi' fakti tekshiriladi");
        config.ApiKeyEncrypted.Should().NotBe("raw-secret-key");
        context.AuditLogList.Should().Contain(a => a.Action == AuditActions.AiConfigKeyChanged);
        // Audit yozuvida kalit QIYMATI umuman bo'lmasligi kerak.
        context.AuditLogList.Should().OnlyContain(a => a.AfterJson == null || !a.AfterJson.Contains("raw-secret-key"));
    }

    [Fact]
    public async Task Handle_ActivateWithoutApiKey_ReturnsValidationError()
    {
        var context = new FakeAiAppDbContext();
        var handler = CreateHandler(context);
        var command = new UpdateAiProviderCommand(AiProvider.Gemini, "gemini-2.0-flash", 4096, 0.4m, IsActive: true, FallbackOrder: 10, ApiKey: null, BaseUrl: null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_ExistingConfigWithKeyAlready_ActivateWithoutNewKey_Succeeds()
    {
        var context = new FakeAiAppDbContext();
        var existing = AiProviderConfig.Create(Guid.NewGuid(), AiProvider.Gemini, "Gemini", "old-model", Now);
        existing.SetApiKey("enc:already-set", Now);
        context.ProviderConfigList.Add(existing);

        var handler = CreateHandler(context);
        var command = new UpdateAiProviderCommand(AiProvider.Gemini, "gemini-2.0-flash", 8192, 0.5m, IsActive: true, FallbackOrder: 5, ApiKey: null, BaseUrl: null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsActive.Should().BeTrue();
        existing.Model.Should().Be("gemini-2.0-flash");
        existing.ApiKeyEncrypted.Should().Be("enc:already-set", "yangi kalit berilmagan — eskisi o'zgarmasligi kerak");
        context.AuditLogList.Should().NotContain(a => a.Action == AuditActions.AiConfigKeyChanged);
    }

    [Fact]
    public async Task Handle_MaskedApiKeyInResponse_NeverExposesRawKey()
    {
        var context = new FakeAiAppDbContext();
        var handler = CreateHandler(context);
        var command = new UpdateAiProviderCommand(AiProvider.Gemini, "gemini-2.0-flash", 4096, 0.4m, IsActive: true, FallbackOrder: 10, ApiKey: "AIzaSyD-verysecretkey1234567890", BaseUrl: null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.MaskedApiKey.Should().NotBeNullOrEmpty();
        result.Value.MaskedApiKey.Should().NotBe("AIzaSyD-verysecretkey1234567890");
        result.Value.MaskedApiKey.Should().Contain("••••••");
    }
}
