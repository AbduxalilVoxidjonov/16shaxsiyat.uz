using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Seeding;

/// <summary>
/// `Infrastructure/Persistence/SeedData/surveys/*.json` faylining ildiz sxemasi — mijozga
/// xos, tarmoqlanuvchi (`docs/18`) so'rovnoma namunasi uchun. `TestDefinitionSeedDto`dan
/// FARQLI: bu yerdagi anketalar `Kind = Custom`/`IsSystem = false`/`ScoringMode = Survey`
/// va natija HAR DOIM `Status = Draft` bo'ladi — superadmin tahrirlab (masalan, 2-B
/// bo'limidagi o'quv markaz nomlarini) o'zi nashr qiladi (`docs/18` §7).
/// </summary>
public sealed record SurveySeedDto
{
    public string Code { get; init; } = string.Empty;

    public string NameUz { get; init; } = string.Empty;

    public string? DescriptionUz { get; init; }

    public int DisplayOrder { get; init; }

    public int EstimatedMinutes { get; init; }

    public int PageSize { get; init; } = 10;

    public bool ShuffleQuestions { get; init; }

    /// <summary>`TestScoringMode` enum nomi — namunaviy so'rovnomalarda har doim "Survey".</summary>
    public string ScoringMode { get; init; } = "Survey";

    public IReadOnlyList<SurveySectionSeedDto> Sections { get; init; } = [];

    public IReadOnlyList<SurveyQuestionSeedDto> Questions { get; init; } = [];
}

/// <summary>Bitta bo'lim yozuvi (`docs/18` §2.2, §7).</summary>
public sealed record SurveySectionSeedDto
{
    public string Code { get; init; } = string.Empty;

    public string TitleUz { get; init; } = string.Empty;

    public string? DescriptionUz { get; init; }

    public int DisplayOrder { get; init; }

    public VisibilityRule? Visibility { get; init; }
}

/// <summary>
/// Bitta savol yozuvi — `ImportQuestionItemDto` (`Admin/Catalog/Questions/Import`) bilan BIR
/// XIL maydonlar to'plami (`docs/18` §5 kengaytmasi). Ataylab alohida tur: seed qatlami
/// (`Application/Seeding`) admin buyruqlar qatlamiga (`Application/Admin/*`) BOG'LIQ
/// BO'LMASLIGI kerak (faqat umumiy `QuestionOptionInputDto`/`VisibilityRule` ishlatiladi).
/// </summary>
public sealed record SurveyQuestionSeedDto
{
    public string Code { get; init; } = string.Empty;

    public int Order { get; init; }

    /// <summary>`null` — bo'limga tegishli emas.</summary>
    public string? SectionCode { get; init; }

    public string TextUz { get; init; } = string.Empty;

    /// <summary>`QuestionType` enum nomi.</summary>
    public string Type { get; init; } = string.Empty;

    public string Scale { get; init; } = string.Empty;

    /// <summary>+1 yoki -1.</summary>
    public int Direction { get; init; } = 1;

    public decimal Weight { get; init; } = 1.0m;

    public bool? IsRequired { get; init; }

    public string? Placeholder { get; init; }

    public string? InputPattern { get; init; }

    public int? MaxLength { get; init; }

    public int? MinSelections { get; init; }

    public int? MaxSelections { get; init; }

    public VisibilityRule? Visibility { get; init; }

    public IReadOnlyList<QuestionOptionInputDto>? Options { get; init; }
}
