using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Anketa savoli. `IsSystem = true` bo'lsa — seed'dan kelgan, `Scale`/`ScaleDirection`/`Weight`
/// o'zgarmaydi (BR-8, `docs/04` 2.7-bo'lim, xato kodi `SYSTEM_TEST_LOCKED`).
/// </summary>
public sealed class Question : Entity
{
    private readonly List<AnswerOption> _options = [];

    public Guid TestDefinitionId { get; private set; }

    public string Code { get; private set; } = null!;

    public int DisplayOrder { get; private set; }

    public string TextUz { get; private set; } = null!;

    public string? TextRu { get; private set; }

    public string? TextEn { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public string Scale { get; private set; } = null!;

    /// <summary>+1 yoki -1 — savolning shkalaga ta'sir yo'nalishi.</summary>
    public int ScaleDirection { get; private set; }

    public decimal Weight { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Seed'dan kelgan savol — o'chirilmaydi, shkalasi o'zgarmaydi (BR-8).</summary>
    public bool IsSystem { get; private set; }

    /// <summary>`null` — bo'limga tegishli emas (`docs/18` §2.3).</summary>
    public Guid? SectionId { get; private set; }

    /// <summary>Savol darajasidagi ko'rsatish sharti — FAQAT `Survey` anketalarda (B-2, `docs/18` §2.3/§2.4).</summary>
    public VisibilityRule? VisibilityRule { get; private set; }

    /// <summary>Matn turlari uchun bo'sh maydon ko'rsatkichi (`docs/18` §2.3).</summary>
    public string? Placeholder { get; private set; }

    /// <summary>`ShortText`/`Phone` uchun regex shabloni (.NET va JS ikkalasida ham ishlaydigan).</summary>
    public string? InputPattern { get; private set; }

    /// <summary>Matn turlari uchun maksimal uzunlik — standart `ShortText`/`Phone` 200, `LongText` 2000, chegara 4000.</summary>
    public int? MaxLength { get; private set; }

    /// <summary>`MultiChoice` uchun minimal tanlovlar soni.</summary>
    public int? MinSelections { get; private set; }

    /// <summary>`MultiChoice` uchun maksimal tanlovlar soni — `null` cheklovsiz.</summary>
    public int? MaxSelections { get; private set; }

    public IReadOnlyCollection<AnswerOption> Options => _options.AsReadOnly();

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private Question()
    {
    }

    private Question(
        Guid id,
        Guid testDefinitionId,
        string code,
        int displayOrder,
        string textUz,
        string? textRu,
        string? textEn,
        QuestionType questionType,
        string scale,
        int scaleDirection,
        decimal weight,
        bool isRequired,
        bool isSystem,
        Guid? sectionId,
        VisibilityRule? visibilityRule,
        string? placeholder,
        string? inputPattern,
        int? maxLength,
        int? minSelections,
        int? maxSelections)
        : base(id)
    {
        TestDefinitionId = testDefinitionId;
        Code = code;
        DisplayOrder = displayOrder;
        TextUz = textUz;
        TextRu = textRu;
        TextEn = textEn;
        QuestionType = questionType;
        Scale = scale;
        ScaleDirection = scaleDirection;
        Weight = weight;
        IsRequired = isRequired;
        IsActive = true;
        IsSystem = isSystem;
        SectionId = sectionId;
        VisibilityRule = visibilityRule;
        Placeholder = placeholder;
        InputPattern = inputPattern;
        MaxLength = maxLength;
        MinSelections = minSelections;
        MaxSelections = maxSelections;
    }

    public static Question Create(
        Guid id,
        Guid testDefinitionId,
        string code,
        int displayOrder,
        string textUz,
        QuestionType questionType,
        string scale,
        int scaleDirection,
        decimal weight,
        bool isRequired = true,
        bool isSystem = false,
        string? textRu = null,
        string? textEn = null,
        Guid? sectionId = null,
        VisibilityRule? visibilityRule = null,
        string? placeholder = null,
        string? inputPattern = null,
        int? maxLength = null,
        int? minSelections = null,
        int? maxSelections = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Savol kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(textUz))
        {
            throw new ArgumentException("Savol matni bo'sh bo'lishi mumkin emas.", nameof(textUz));
        }

        if (string.IsNullOrWhiteSpace(scale))
        {
            throw new ArgumentException("Shkala kodi bo'sh bo'lishi mumkin emas.", nameof(scale));
        }

        ValidateScaleDirection(scaleDirection);
        visibilityRule?.Validate();
        ValidateMaxLength(maxLength);

        return new Question(
            id, testDefinitionId, code, displayOrder, textUz, textRu, textEn, questionType, scale, scaleDirection, weight, isRequired, isSystem,
            sectionId, visibilityRule, placeholder, inputPattern, maxLength, minSelections, maxSelections);
    }

    /// <summary>Shkala/yo'nalish/vazn — faqat `IsSystem = false` savollarda o'zgartiriladi (BR-8).</summary>
    public void UpdateScale(string scale, int scaleDirection, decimal weight)
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasi savolining shkalasini o'zgartirib bo'lmaydi.");
        }

        if (string.IsNullOrWhiteSpace(scale))
        {
            throw new ArgumentException("Shkala kodi bo'sh bo'lishi mumkin emas.", nameof(scale));
        }

        ValidateScaleDirection(scaleDirection);

        Scale = scale;
        ScaleDirection = scaleDirection;
        Weight = weight;
    }

    public void UpdateText(string textUz, string? textRu, string? textEn)
    {
        if (string.IsNullOrWhiteSpace(textUz))
        {
            throw new ArgumentException("Savol matni bo'sh bo'lishi mumkin emas.", nameof(textUz));
        }

        TextUz = textUz;
        TextRu = textRu;
        TextEn = textEn;
    }

    /// <summary>
    /// Ko'rsatish tartibini yangilaydi. BR-8 faqat `Scale`/`ScaleDirection`/`Weight`ni qulflaydi —
    /// tartib tizim savollarida ham seed orqali yangilanishi mumkin.
    /// </summary>
    public void UpdateOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>BR-8 doirasiga kirmaydi — tizim savolida ham o'zgartirish mumkin (`docs/07` §3.4 faqat `Scale`/`Direction`/`Weight`ni cheklaydi).</summary>
    public void UpdateRequired(bool isRequired) => IsRequired = isRequired;

    /// <summary>Savolni bo'limga biriktiradi/bo'limdan chiqaradi (`null`). `IsSystem`da qulflangan (`docs/18` §2.2).</summary>
    public void AssignSection(Guid? sectionId)
    {
        EnsureNotSystemLocked();
        SectionId = sectionId;
    }

    /// <summary>Savol darajasidagi ko'rsatish sharti — FAQAT `Survey` anketalarda ma'noli (B-2), `IsSystem`da qulflangan (`docs/18` §2.3).</summary>
    public void UpdateVisibility(VisibilityRule? visibilityRule)
    {
        EnsureNotSystemLocked();
        visibilityRule?.Validate();
        VisibilityRule = visibilityRule;
    }

    /// <summary>`docs/18` §2.3 — matn turlari uchun bo'sh maydon ko'rsatkichi. `IsSystem`da qulflangan.</summary>
    public void UpdatePlaceholder(string? placeholder)
    {
        EnsureNotSystemLocked();

        if (placeholder is { Length: > 200 })
        {
            throw new ArgumentException("Placeholder 200 belgidan oshmasligi kerak.", nameof(placeholder));
        }

        Placeholder = placeholder;
    }

    /// <summary>`docs/18` §2.3 — `ShortText`/`Phone` uchun regex shablon. `IsSystem`da qulflangan.</summary>
    public void UpdateInputPattern(string? inputPattern)
    {
        EnsureNotSystemLocked();

        if (inputPattern is { Length: > 200 })
        {
            throw new ArgumentException("InputPattern 200 belgidan oshmasligi kerak.", nameof(inputPattern));
        }

        InputPattern = inputPattern;
    }

    /// <summary>`docs/18` §2.3 — matn turlari uchun maksimal uzunlik (chegara 4000). `IsSystem`da qulflangan.</summary>
    public void UpdateMaxLength(int? maxLength)
    {
        EnsureNotSystemLocked();
        ValidateMaxLength(maxLength);
        MaxLength = maxLength;
    }

    /// <summary>`docs/18` §2.3 — `MultiChoice` uchun tanlovlar soni chegarasi. `IsSystem`da qulflangan.</summary>
    public void UpdateSelectionLimits(int? minSelections, int? maxSelections)
    {
        EnsureNotSystemLocked();

        if (minSelections is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minSelections), "Minimal tanlovlar soni manfiy bo'lishi mumkin emas.");
        }

        if (maxSelections is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSelections), "Maksimal tanlovlar soni musbat bo'lishi kerak.");
        }

        if (minSelections is not null && maxSelections is not null && minSelections > maxSelections)
        {
            throw new ArgumentException("Minimal tanlovlar soni maksimaldan katta bo'lishi mumkin emas.", nameof(minSelections));
        }

        MinSelections = minSelections;
        MaxSelections = maxSelections;
    }

    public void AddOption(AnswerOption option)
    {
        if (QuestionType is not (QuestionType.SingleChoice or QuestionType.ForcedChoice or QuestionType.MultiChoice))
        {
            throw new DomainException("QUESTION_OPTIONS_NOT_ALLOWED", "Faqat 'SingleChoice'/'ForcedChoice'/'MultiChoice' savollariga variant qo'shish mumkin.");
        }

        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasi savoliga variant qo'shib bo'lmaydi.");
        }

        _options.Add(option);
    }

    public void RemoveOption(Guid optionId)
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasi savolidan variant o'chirib bo'lmaydi.");
        }

        _options.RemoveAll(o => o.Id == optionId);
    }

    private static void ValidateScaleDirection(int scaleDirection)
    {
        if (scaleDirection is not (1 or -1))
        {
            throw new ArgumentOutOfRangeException(nameof(scaleDirection), "Shkala yo'nalishi faqat +1 yoki -1 bo'lishi mumkin.");
        }
    }

    /// <summary>`docs/18` §2.3 — matn maydonlari uchun chegara 4000.</summary>
    private static void ValidateMaxLength(int? maxLength)
    {
        if (maxLength is < 1 or > 4000)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "MaxLength 1 dan 4000 gacha bo'lishi kerak.");
        }
    }

    /// <summary>`docs/18` §2.2/§2.3 — tarmoqlanish bilan bog'liq maydonlar `IsSystem`da qulflangan (B-3/BR-8).</summary>
    private void EnsureNotSystemLocked()
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasi savolining bu maydonini o'zgartirib bo'lmaydi.");
        }
    }
}
