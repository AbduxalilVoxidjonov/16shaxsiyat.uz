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

    /// <summary>
    /// `true` — bu yozuv haqiqiy AI javobi EMAS, barcha provayderlar/urinishlar muvaffaqiyatsiz
    /// bo'lgandan keyin `TypeCatalog`/`CareerMap`dan yig'ilgan shablon hisobot (`docs/09-ai-analiz-moduli.md`
    /// 11-bo'lim, P18). Faqat `CreateFallbackReport` orqali `true` bo'ladi. UI'da "Avtomatik
    /// shablon hisobot" deb belgilanishi va "Qayta urinish" tugmasi ko'rsatilishi kerak
    /// (bu bayroqni admin DTO'ga ulash P18 qamroviga kirmaydi — `Application.Admin.Students`
    /// boshqa agent hududi, PM'ga hisobotda alohida qayd etilgan).
    /// </summary>
    public bool IsFallbackReport { get; private set; }

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

    /// <summary>
    /// `CreateFallbackReport` yozuvini "joriy" deb belgilaydi — chaqiruvchi (orkestrator) buni
    /// FAQAT shu `Assessment` uchun ILGARI hech qanday `IsCurrent = true` yozuv bo'lmaganda
    /// chaqiradi (eski, haqiqiy muvaffaqiyatli tahlil zaxira shablon bilan yashirilmasligi kerak).
    /// </summary>
    public void MarkCurrent() => IsCurrent = true;

    /// <summary>
    /// Faqat butun fallback zanjiri (`docs/09` 7-bo'lim) muvaffaqiyatsiz bo'lgandan KEYIN,
    /// zanjirdagi OXIRGI urinishning xabarini aniqroq qilish uchun (`CLAUDE.md` MAXSUS DIQQAT
    /// #1: "hamma provider BadRequest bersa ... 'so'rov shakli noto'g'ri' deb yozilsin"). Har
    /// bir alohida urinishning texnik xabari `Fail()` orqali allaqachon yozilgan — bu faqat
    /// ZANJIRNING YAKUNIY sababini almashtiradi.
    /// </summary>
    public void OverrideErrorMessage(string errorMessage)
    {
        if (Status != AiAnalysisStatus.Failed)
        {
            throw new DomainException("AI_ANALYSIS_INVALID_TRANSITION", $"Xato xabarini faqat 'Failed' holatida almashtirish mumkin (joriy: '{Status}').");
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Xato xabari bo'sh bo'lishi mumkin emas.", nameof(errorMessage));
        }

        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Zaxira (fallback) shablon hisobot — haqiqiy AI chaqiruvi YO'Q, `Pending → Running` bosqichlarini
    /// aylanib o'tadi va `AiAnalysisSucceededEvent` KO'TARMAYDI (bu haqiqiy AI muvaffaqiyati emas —
    /// `Assessment.Status` chaqiruvchida ataylab `AnalysisFailed`da qoldiriladi, `docs/09` 11-bo'lim).
    /// `lastAttemptedProvider` — faqat izoh uchun (qaysi provider oxirgi bo'lib urinilgani);
    /// hech qanday provider sinalmagan bo'lsa chaqiruvchi shartli qiymat beradi
    /// (`MockAiProvider.Kind` bilan bir xil naqsh — enumda "yo'q" degan a'zo yo'q).
    /// </summary>
    public static AiAnalysis CreateFallbackReport(
        Guid id,
        Guid assessmentId,
        AiProvider lastAttemptedProvider,
        string promptVersion,
        int attemptNumber,
        string summary,
        string personalityPortrait,
        string? strengthsJson,
        string? growthAreasJson,
        string? careerSuggestionsJson,
        string? teacherNotes,
        string? parentNotes,
        string? attentionFlagsJson,
        DateTimeOffset now)
    {
        var analysis = new AiAnalysis(id, assessmentId, lastAttemptedProvider, "template", promptVersion, null, attemptNumber, now)
        {
            Status = AiAnalysisStatus.Succeeded,
            Summary = summary,
            PersonalityPortrait = personalityPortrait,
            StrengthsJson = strengthsJson,
            GrowthAreasJson = growthAreasJson,
            CareerSuggestionsJson = careerSuggestionsJson,
            TeacherNotes = teacherNotes,
            ParentNotes = parentNotes,
            AttentionFlagsJson = attentionFlagsJson,
            IsFallbackReport = true,
        };

        return analysis;
    }
}
