using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `SingleChoice`/`ForcedChoice` savollari uchun tanlov varianti (`docs/04` 2.7-bo'lim).
/// </summary>
public sealed class AnswerOption : Entity
{
    public Guid QuestionId { get; private set; }

    public string TextUz { get; private set; } = null!;

    public int Value { get; private set; }

    public string? Scale { get; private set; }

    public int DisplayOrder { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AnswerOption()
    {
    }

    private AnswerOption(Guid id, Guid questionId, string textUz, int value, string? scale, int displayOrder)
        : base(id)
    {
        QuestionId = questionId;
        TextUz = textUz;
        Value = value;
        Scale = scale;
        DisplayOrder = displayOrder;
    }

    public static AnswerOption Create(Guid id, Guid questionId, string textUz, int value, int displayOrder, string? scale = null)
    {
        if (string.IsNullOrWhiteSpace(textUz))
        {
            throw new ArgumentException("Variant matni bo'sh bo'lishi mumkin emas.", nameof(textUz));
        }

        return new AnswerOption(id, questionId, textUz, value, scale, displayOrder);
    }
}
