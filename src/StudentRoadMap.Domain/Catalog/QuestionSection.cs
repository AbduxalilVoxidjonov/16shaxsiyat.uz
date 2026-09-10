using System.Text.RegularExpressions;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Anketa savollarini guruhlaydigan bo'lim — `TestDefinition` agregati ichidagi bola
/// (`docs/18` §2.2). Faqat `Custom` (`IsSystem = false`) anketalarda qo'shiladi/o'zgartiriladi
/// (BR-8/B-3, `SYSTEM_TEST_LOCKED`).
/// </summary>
public sealed partial class QuestionSection : Entity
{
    public Guid TestDefinitionId { get; private set; }

    public string Code { get; private set; } = null!;

    public string TitleUz { get; private set; } = null!;

    public string? DescriptionUz { get; private set; }

    public int DisplayOrder { get; private set; }

    /// <summary>Bo'lim ko'rinishi sharti — `null` bo'lsa shartsiz, har doim ko'rinadi.</summary>
    public VisibilityRule? VisibilityRule { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private QuestionSection()
    {
    }

    private QuestionSection(
        Guid id,
        Guid testDefinitionId,
        string code,
        string titleUz,
        string? descriptionUz,
        int displayOrder,
        VisibilityRule? visibilityRule)
        : base(id)
    {
        TestDefinitionId = testDefinitionId;
        Code = code;
        TitleUz = titleUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
        VisibilityRule = visibilityRule;
    }

    public static QuestionSection Create(
        Guid id,
        Guid testDefinitionId,
        string code,
        string titleUz,
        int displayOrder,
        string? descriptionUz = null,
        VisibilityRule? visibilityRule = null)
    {
        ValidateCode(code);

        if (string.IsNullOrWhiteSpace(titleUz))
        {
            throw new ArgumentException("Bo'lim sarlavhasi bo'sh bo'lishi mumkin emas.", nameof(titleUz));
        }

        if (titleUz.Length > 200)
        {
            throw new ArgumentException("Bo'lim sarlavhasi 200 belgidan oshmasligi kerak.", nameof(titleUz));
        }

        if (descriptionUz is { Length: > 1000 })
        {
            throw new ArgumentException("Bo'lim tavsifi 1000 belgidan oshmasligi kerak.", nameof(descriptionUz));
        }

        visibilityRule?.Validate();

        return new QuestionSection(id, testDefinitionId, code.Trim(), titleUz, descriptionUz, displayOrder, visibilityRule);
    }

    public void UpdateMetadata(string titleUz, string? descriptionUz, VisibilityRule? visibilityRule)
    {
        if (string.IsNullOrWhiteSpace(titleUz))
        {
            throw new ArgumentException("Bo'lim sarlavhasi bo'sh bo'lishi mumkin emas.", nameof(titleUz));
        }

        if (titleUz.Length > 200)
        {
            throw new ArgumentException("Bo'lim sarlavhasi 200 belgidan oshmasligi kerak.", nameof(titleUz));
        }

        if (descriptionUz is { Length: > 1000 })
        {
            throw new ArgumentException("Bo'lim tavsifi 1000 belgidan oshmasligi kerak.", nameof(descriptionUz));
        }

        visibilityRule?.Validate();

        TitleUz = titleUz;
        DescriptionUz = descriptionUz;
        VisibilityRule = visibilityRule;
    }

    public void UpdateOrder(int displayOrder) => DisplayOrder = displayOrder;

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Bo'lim kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        var trimmed = code.Trim();

        if (trimmed.Length > 20)
        {
            throw new ArgumentException("Bo'lim kodi 20 belgidan oshmasligi kerak.", nameof(code));
        }

        if (!CodePattern().IsMatch(trimmed))
        {
            throw new ArgumentException("Bo'lim kodi faqat lotin harf, raqam, '-' va '_' belgilaridan iborat bo'lishi mumkin.", nameof(code));
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex CodePattern();
}
