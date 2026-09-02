using System.Text.Json;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// `TestResult.RawScoresJson`/`NormalizedScoresJson`/`LevelsJson`/`FlagsJson` (`jsonb`)
/// ustuni bilan `Domain.Scoring.ScoringResult` orasidagi (de)serializatsiya — `Application`
/// qatlamida (EF Core'siz, `docs/06` 3-bo'lim), `System.Text.Json` BCL qismi bo'lgani uchun
/// bu yerga bog'lash muammo emas. `CompleteTestCommandHandler` yozish uchun, `CompleteSessionCommandHandler`
/// (BIG5/ACTIVITY natijasini `MaturityIndex` uchun qayta o'qish) va `GetStudentResultQueryHandler`
/// o'qish uchun ishlatadi.
/// </summary>
internal static class TestResultJson
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = null };

    public static string Serialize(IReadOnlyDictionary<string, double> scores) => JsonSerializer.Serialize(scores, Options);

    public static string Serialize(IReadOnlyDictionary<string, string> levels) => JsonSerializer.Serialize(levels, Options);

    public static string SerializeFlags(IReadOnlyList<string> flags) => JsonSerializer.Serialize(flags, Options);

    public static IReadOnlyDictionary<string, double> DeserializeScores(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, double>>(json, Options) ?? [];

    public static IReadOnlyDictionary<string, string> DeserializeLevels(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options) ?? [];

    public static IReadOnlyList<string> DeserializeFlags(string json) =>
        JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
}
