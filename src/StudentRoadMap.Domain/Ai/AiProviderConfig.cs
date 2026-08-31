using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Ai;

/// <summary>
/// Superadmin tomonidan kiritilgan AI provayder sozlamasi. `ApiKeyEncrypted` — Infrastructure
/// qatlamida shifrlangan holda saqlanadi, Domen uni xom matn sifatida ushlaydi (`docs/04` 2.9-bo'lim).
/// </summary>
public sealed class AiProviderConfig : Entity
{
    public AiProvider Provider { get; private set; }

    public string DisplayName { get; private set; } = null!;

    public string? ApiKeyEncrypted { get; private set; }

    public string Model { get; private set; } = null!;

    public string? BaseUrl { get; private set; }

    public int MaxOutputTokens { get; private set; }

    public decimal Temperature { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public int FallbackOrder { get; private set; }

    public DateTimeOffset? LastCheckedAt { get; private set; }

    public string? LastCheckStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AiProviderConfig()
    {
    }

    private AiProviderConfig(
        Guid id,
        AiProvider provider,
        string displayName,
        string model,
        int maxOutputTokens,
        decimal temperature,
        int fallbackOrder,
        DateTimeOffset now)
        : base(id)
    {
        Provider = provider;
        DisplayName = displayName;
        Model = model;
        MaxOutputTokens = maxOutputTokens;
        Temperature = temperature;
        FallbackOrder = fallbackOrder;
        IsDefault = false;
        IsActive = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static AiProviderConfig Create(
        Guid id,
        AiProvider provider,
        string displayName,
        string model,
        DateTimeOffset now,
        int maxOutputTokens = 4096,
        decimal temperature = 0.4m,
        int fallbackOrder = 100)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Ko'rsatish nomi bo'sh bo'lishi mumkin emas.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model nomi bo'sh bo'lishi mumkin emas.", nameof(model));
        }

        if (maxOutputTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxOutputTokens), "Maksimal token soni musbat bo'lishi kerak.");
        }

        return new AiProviderConfig(id, provider, displayName, model, maxOutputTokens, temperature, fallbackOrder, now);
    }

    public void UpdateSettings(
        string displayName,
        string model,
        string? baseUrl,
        int maxOutputTokens,
        decimal temperature,
        int fallbackOrder,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Ko'rsatish nomi bo'sh bo'lishi mumkin emas.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model nomi bo'sh bo'lishi mumkin emas.", nameof(model));
        }

        if (maxOutputTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxOutputTokens), "Maksimal token soni musbat bo'lishi kerak.");
        }

        DisplayName = displayName;
        Model = model;
        BaseUrl = baseUrl;
        MaxOutputTokens = maxOutputTokens;
        Temperature = temperature;
        FallbackOrder = fallbackOrder;
        UpdatedAt = now;
    }

    /// <summary>Shifrlangan API kalitini yangilaydi (shifrlash Infrastructure qatlamida bajariladi).</summary>
    public void SetApiKey(string apiKeyEncrypted, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(apiKeyEncrypted))
        {
            throw new ArgumentException("API kaliti bo'sh bo'lishi mumkin emas.", nameof(apiKeyEncrypted));
        }

        ApiKeyEncrypted = apiKeyEncrypted;
        UpdatedAt = now;
    }

    /// <summary>Faqat bitta faol yozuv `IsDefault = true` bo'lishi kerak — avvalgisini `UnmarkAsDefault()` bilan bekor qiling.</summary>
    public void MarkAsDefault(DateTimeOffset now)
    {
        IsDefault = true;
        UpdatedAt = now;
    }

    public void UnmarkAsDefault(DateTimeOffset now)
    {
        IsDefault = false;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>Ulanishni tekshirish natijasini yozadi (`TestConnection` use-case).</summary>
    public void RecordConnectionCheck(string status, DateTimeOffset now)
    {
        LastCheckStatus = status;
        LastCheckedAt = now;
        UpdatedAt = now;
    }
}
