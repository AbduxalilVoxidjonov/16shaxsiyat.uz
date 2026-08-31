using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Events;

namespace StudentRoadMap.Domain.Ai;

/// <summary>
/// Bitta AI tahlil urinishi. `AssessmentId` bo'yicha `IsCurrent = true` yozuv faqat bitta bo'lishi
/// kerak — invariantni Application qatlami (avvalgi joriy yozuvni `MarkNotCurrent()` bilan) ta'minlaydi
/// (`docs/04` 2.8-bo'lim).
/// </summary>
public sealed class AiAnalysis : AggregateRoot
{
    public Guid AssessmentId { get; private set; }

    public AiProvider Provider { get; private set; }

    public string Model { get; private set; } = null!;

    public string PromptVersion { get; private set; } = null!;

    public AiAnalysisStatus Status { get; private set; }

    public string? RequestPayloadJson { get; private set; }

    public string? ResponseJson { get; private set; }

    public string? Summary { get; private set; }

    public string? PersonalityPortrait { get; private set; }

    public string? StrengthsJson { get; private set; }

    public string? GrowthAreasJson { get; private set; }

    public string? RecommendationsJson { get; private set; }

    public string? CareerSuggestionsJson { get; private set; }

    public string? TeacherNotes { get; private set; }

    public string? ParentNotes { get; private set; }

    public string? AttentionFlagsJson { get; private set; }

    public int? InputTokens { get; private set; }

    public int? OutputTokens { get; private set; }

    public decimal? EstimatedCostUsd { get; private set; }

    public int? DurationMs { get; private set; }

    public string? ErrorMessage { get; private set; }

    public int AttemptNumber { get; private set; }

    public bool IsCurrent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AiAnalysis()
    {
    }

    private AiAnalysis(Guid id, Guid assessmentId, AiProvider provider, string model, string promptVersion, string? requestPayloadJson, int attemptNumber, DateTimeOffset now)
        : base(id)
    {
        AssessmentId = assessmentId;
        Provider = provider;
        Model = model;
        PromptVersion = promptVersion;
        RequestPayloadJson = requestPayloadJson;
        Status = AiAnalysisStatus.Pending;
        AttemptNumber = attemptNumber;
        IsCurrent = false;
        CreatedAt = now;
    }

    public static AiAnalysis Create(
        Guid id,
        Guid assessmentId,
        AiProvider provider,
        string model,
        string promptVersion,
        DateTimeOffset now,
        int attemptNumber = 1,
        string? requestPayloadJson = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model nomi bo'sh bo'lishi mumkin emas.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(promptVersion))
        {
            throw new ArgumentException("Prompt versiyasi bo'sh bo'lishi mumkin emas.", nameof(promptVersion));
        }

        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Urinish raqami musbat bo'lishi kerak.");
        }

        return new AiAnalysis(id, assessmentId, provider, model, promptVersion, requestPayloadJson, attemptNumber, now);
    }

    public void Start()
    {
        if (Status != AiAnalysisStatus.Pending)
        {
            throw new DomainException("AI_ANALYSIS_INVALID_TRANSITION", $"Tahlil '{Status}' holatidan 'Running' ga o'ta olmaydi.");
        }

        Status = AiAnalysisStatus.Running;
    }

    /// <summary>AI muvaffaqiyatli javob berganda — `AiAnalysisSucceededEvent` ko'taradi.</summary>
    public void Succeed(
        string responseJson,
        string summary,
        string personalityPortrait,
        DateTimeOffset now,
        string? strengthsJson = null,
        string? growthAreasJson = null,
        string? recommendationsJson = null,
        string? careerSuggestionsJson = null,
        string? teacherNotes = null,
        string? parentNotes = null,
        string? attentionFlagsJson = null,
        int? inputTokens = null,
        int? outputTokens = null,
        decimal? estimatedCostUsd = null,
        int? durationMs = null)
    {
        if (Status != AiAnalysisStatus.Running)
        {
            throw new DomainException("AI_ANALYSIS_INVALID_TRANSITION", $"Tahlil '{Status}' holatidan 'Succeeded' ga o'ta olmaydi.");
        }

        Status = AiAnalysisStatus.Succeeded;
        ResponseJson = responseJson;
        Summary = summary;
        PersonalityPortrait = personalityPortrait;
        StrengthsJson = strengthsJson;
        GrowthAreasJson = growthAreasJson;
        RecommendationsJson = recommendationsJson;
        CareerSuggestionsJson = careerSuggestionsJson;
        TeacherNotes = teacherNotes;
        ParentNotes = parentNotes;
        AttentionFlagsJson = attentionFlagsJson;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        EstimatedCostUsd = estimatedCostUsd;
        DurationMs = durationMs;
        IsCurrent = true;

        RaiseDomainEvent(new AiAnalysisSucceededEvent(AssessmentId, Id, now));
    }

    /// <summary>AI xato qaytarganda/timeout bo'lganda — `AiAnalysisFailedEvent` ko'taradi.</summary>
    public void Fail(string errorMessage, DateTimeOffset now, int? durationMs = null)
    {
        if (Status != AiAnalysisStatus.Running)
        {
            throw new DomainException("AI_ANALYSIS_INVALID_TRANSITION", $"Tahlil '{Status}' holatidan 'Failed' ga o'ta olmaydi.");
        }

        Status = AiAnalysisStatus.Failed;
        ErrorMessage = errorMessage;
        DurationMs = durationMs;

        RaiseDomainEvent(new AiAnalysisFailedEvent(AssessmentId, Id, errorMessage, now));
    }

    /// <summary>Yangi urinish joriy bo'lganda avvalgi muvaffaqiyatli yozuvda chaqiriladi.</summary>
    public void MarkNotCurrent() => IsCurrent = false;
}
