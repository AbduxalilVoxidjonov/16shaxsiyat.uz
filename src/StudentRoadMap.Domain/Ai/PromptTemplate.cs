using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Ai;

/// <summary>
/// AI so'roviga yuboriladigan prompt shabloni (`Key` + `Version` bo'yicha, masalan
/// `full_analysis` / `v1.0`, `docs/04` 2.9-bo'lim, `docs/09` AI modul).
/// </summary>
public sealed class PromptTemplate : Entity
{
    public string Key { get; private set; } = null!;

    public string Version { get; private set; } = null!;

    public string SystemText { get; private set; } = null!;

    public string UserText { get; private set; } = null!;

    public string JsonSchema { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private PromptTemplate()
    {
    }

    private PromptTemplate(Guid id, string key, string version, string systemText, string userText, string jsonSchema, DateTimeOffset now)
        : base(id)
    {
        Key = key;
        Version = version;
        SystemText = systemText;
        UserText = userText;
        JsonSchema = jsonSchema;
        IsActive = false;
        CreatedAt = now;
    }

    public static PromptTemplate Create(
        Guid id,
        string key,
        string version,
        string systemText,
        string userText,
        string jsonSchema,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Shablon kaliti bo'sh bo'lishi mumkin emas.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Shablon versiyasi bo'sh bo'lishi mumkin emas.", nameof(version));
        }

        if (string.IsNullOrWhiteSpace(systemText))
        {
            throw new ArgumentException("Tizim matni bo'sh bo'lishi mumkin emas.", nameof(systemText));
        }

        if (string.IsNullOrWhiteSpace(userText))
        {
            throw new ArgumentException("Foydalanuvchi matni bo'sh bo'lishi mumkin emas.", nameof(userText));
        }

        if (string.IsNullOrWhiteSpace(jsonSchema))
        {
            throw new ArgumentException("JSON schema bo'sh bo'lishi mumkin emas.", nameof(jsonSchema));
        }

        return new PromptTemplate(id, key, version, systemText, userText, jsonSchema, now);
    }

    /// <summary>Ushbu shablonni joriy (faol) versiya sifatida belgilaydi — bir `Key` uchun bittasi faol bo'lishi Application qatlamida ta'minlanadi.</summary>
    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
