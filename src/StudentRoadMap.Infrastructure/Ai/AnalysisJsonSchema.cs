using System.Reflection;
using Json.Schema;

namespace StudentRoadMap.Infrastructure.Ai;

/// <summary>
/// `full_analysis` javobining JSON sxemasi (`docs/09-ai-analiz-moduli.md` 5-bo'lim) — embedded
/// resurs sifatida (`Ai/Resources/analysis-schema.v1.json`), kodda qattiq yozilgan JSON matni
/// emas. `AiResponseValidator`ning 2-bosqichi (`JsonSchema.Net`) va `DbSeeder`ning
/// `prompt_templates.json_schema` ustuni shu klassdan foydalanadi.
/// </summary>
public static class AnalysisJsonSchema
{
    private const string ResourceName = "StudentRoadMap.Infrastructure.Ai.Resources.analysis-schema.v1.json";

    /// <summary>Sxemaning xom JSON matni (`prompt_templates.json_schema`ga seed qilish uchun).</summary>
    public static string RawJson { get; } = LoadRawJson();

    /// <summary>Tayyor parse qilingan `JsonSchema.Net` sxemasi — har chaqiruvda qayta parse qilinmasligi uchun bir marta yaratiladi.</summary>
    public static JsonSchema Default { get; } = JsonSchema.FromText(RawJson);

    private static string LoadRawJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resurs topilmadi: '{ResourceName}'. csproj'da 'EmbeddedResource' sifatida ro'yxatdan o'tganini tekshiring.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
