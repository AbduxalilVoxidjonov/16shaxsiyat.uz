using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StudentRoadMap.Infrastructure.Persistence.Converters;

/// <summary>
/// Domen kolleksiyalarini (`IReadOnlyList&lt;Guid&gt;`, `IReadOnlyList&lt;string&gt;`) `jsonb`
/// ustunlariga serializatsiya qiluvchi umumiy value converter/comparer to'plami (`docs/05`
/// 1-bo'lim: erkin struktura — `jsonb`).
/// </summary>
internal static class JsonValueConverters
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<IReadOnlyList<Guid>, string> GuidListConverter = new(
        value => JsonSerializer.Serialize(value, SerializerOptions),
        json => JsonSerializer.Deserialize<List<Guid>>(json, SerializerOptions) ?? new List<Guid>());

    public static readonly ValueComparer<IReadOnlyList<Guid>> GuidListComparer = new(
        (a, b) => (a ?? new List<Guid>()).SequenceEqual(b ?? new List<Guid>()),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        v => v.ToList());

    public static readonly ValueConverter<IReadOnlyList<string>, string> StringListConverter = new(
        value => JsonSerializer.Serialize(value, SerializerOptions),
        json => JsonSerializer.Deserialize<List<string>>(json, SerializerOptions) ?? new List<string>());

    public static readonly ValueComparer<IReadOnlyList<string>> StringListComparer = new(
        (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        v => v.ToList());
}
