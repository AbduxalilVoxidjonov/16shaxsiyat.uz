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
        bool isSystem)
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
        string? textEn = null)
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

        return new Question(id, testDefinitionId, code, displayOrder, textUz, textRu, textEn, questionType, scale, scaleDirection, weight, isRequired, isSystem);
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

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void AddOption(AnswerOption option)
    {
        if (QuestionType is not (QuestionType.SingleChoice or QuestionType.ForcedChoice))
        {
            throw new DomainException("QUESTION_OPTIONS_NOT_ALLOWED", "Faqat 'SingleChoice'/'ForcedChoice' savollariga variant qo'shish mumkin.");
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
}
