using FluentAssertions;
using Microsoft.Data.Sqlite;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Ai.Providers;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `AiProviderResolver` — `docs/09-ai-analiz-moduli.md` 2 va 7-bo'lim, `prompts/17` vazifa #2:
/// "GetAvailableAsync — faqat kaliti bor va IsActive bo'lganlar", "ResolveAsync — so'ralgan
/// bo'lsa o'sha, aks holda IsDefault", "GetFallbackChainAsync — fallback_order bo'yicha
/// qolganlar". Haqiqiy `AppDbContext` (SQLite in-memory) ustida — `PromptBuilderTests` bilan
/// bir xil naqsh.
/// </summary>
public sealed class AiProviderResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly FakeEncryptionService Encryption = new();

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static AiProviderResolver CreateResolver(AppDbContext context) =>
        new(context, new EfAsyncQueryExecutor(), Encryption, new FakeHttpClientFactory(FakeHttpMessageHandler.Json(System.Net.HttpStatusCode.OK, "{}")));

    private static AiProviderConfig SeedConfig(
        AppDbContext context,
        AiProvider provider,
        string model,
        bool isActive,
        bool isDefault,
        int fallbackOrder,
        string? apiKey = "fake-api-key")
    {
        var config = AiProviderConfig.Create(Guid.NewGuid(), provider, $"{provider} — test", model, Now, fallbackOrder: fallbackOrder);
        if (apiKey is not null)
        {
            config.SetApiKey(Encryption.Encrypt(apiKey), Now);
        }

        if (isActive)
        {
            config.Activate(Now);
        }

        if (isDefault)
        {
            config.MarkAsDefault(Now);
        }

        context.Add(config);
        return config;
    }

    [Fact]
    public async Task GetAvailableAsync_ExcludesInactiveConfigs_OrderedByFallbackOrder()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.OpenAi, "gpt-4.1-mini", isActive: true, isDefault: false, fallbackOrder: 20);
        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: true, fallbackOrder: 10);
        SeedConfig(context, AiProvider.Anthropic, "claude-sonnet-5", isActive: false, isDefault: false, fallbackOrder: 30); // faol emas
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var available = await resolver.GetAvailableAsync(CancellationToken.None);

        available.Should().HaveCount(2);
        available.Select(a => a.Provider).Should().ContainInOrder(AiProvider.Gemini, AiProvider.OpenAi);
        available.Single(a => a.Provider == AiProvider.Gemini).IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableAsync_ExcludesActiveConfigWithoutApiKey()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: true, fallbackOrder: 10);
        // Kaliti yo'q, lekin faol — hali ham chiqmasligi kerak (`ApiKeyEncrypted != null` filtri).
        SeedConfig(context, AiProvider.Anthropic, "claude-sonnet-5", isActive: true, isDefault: false, fallbackOrder: 5, apiKey: null);
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var available = await resolver.GetAvailableAsync(CancellationToken.None);

        available.Should().ContainSingle().Which.Provider.Should().Be(AiProvider.Gemini);
    }

    [Fact]
    public async Task ResolveAsync_RequestedProvider_ReturnsMatchingProviderInstance()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: true, fallbackOrder: 10);
        SeedConfig(context, AiProvider.OpenAi, "gpt-4.1-mini", isActive: true, isDefault: false, fallbackOrder: 20);
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var provider = await resolver.ResolveAsync(AiProvider.OpenAi, CancellationToken.None);

        provider.Kind.Should().Be(AiProvider.OpenAi);
        provider.Should().BeOfType<OpenAiProvider>();
    }

    [Fact]
    public async Task ResolveAsync_NullRequested_ReturnsDefaultProvider()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: false, fallbackOrder: 10);
        SeedConfig(context, AiProvider.Anthropic, "claude-sonnet-5", isActive: true, isDefault: true, fallbackOrder: 20);
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var provider = await resolver.ResolveAsync(null, CancellationToken.None);

        provider.Kind.Should().Be(AiProvider.Anthropic);
    }

    [Fact]
    public async Task ResolveAsync_RequestedProviderNotActive_ThrowsInvalidOperationException()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: true, fallbackOrder: 10);
        SeedConfig(context, AiProvider.OpenAi, "gpt-4.1-mini", isActive: false, isDefault: false, fallbackOrder: 20);
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var act = async () => await resolver.ResolveAsync(AiProvider.OpenAi, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ResolveAsync_NoDefaultConfigured_ThrowsInvalidOperationException()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: false, fallbackOrder: 10);
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var act = async () => await resolver.ResolveAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetFallbackChainAsync_ExcludesPrimary_OrderedByFallbackOrder()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        SeedConfig(context, AiProvider.Gemini, "gemini-2.0-flash", isActive: true, isDefault: true, fallbackOrder: 10);
        SeedConfig(context, AiProvider.Anthropic, "claude-sonnet-5", isActive: true, isDefault: false, fallbackOrder: 30);
        SeedConfig(context, AiProvider.OpenAi, "gpt-4.1-mini", isActive: true, isDefault: false, fallbackOrder: 20);
        await context.SaveChangesAsync();

        var resolver = CreateResolver(context);
        var chain = await resolver.GetFallbackChainAsync(AiProvider.Gemini, CancellationToken.None);

        chain.Select(p => p.Kind).Should().ContainInOrder(AiProvider.OpenAi, AiProvider.Anthropic);
        chain.Should().NotContain(p => p.Kind == AiProvider.Gemini);
    }
}
