using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Jobs;

/// <summary>
/// Fon navbatidagi bitta "shu sessiya uchun AI tahlilini ishga tushir" vazifasi
/// (`docs/09-ai-analiz-moduli.md` 8-bo'lim, `prompts/18`). `AggregateRoot`dan meros OLMAYDI —
/// domen hodisasi ko'tarmaydi (bu ish-yuritish/orkestratsiya yozuvi, biznes agregat emas);
/// `Entity`dan meros oladi (`Guid Id` kerak — `RegistrationCounter`/`SchoolLinkView`dan farqli
/// o'laroq bu yerda kompozit kalit emas, chunki bitta sessiya uchun VAQT o'tishi bilan bir nechta
/// (masalan qayta urinish) vazifa yozuvi bo'lishi mumkin).
///
/// <para>
/// Bitta jarayon (single instance, MVP, `docs/13-deploy-va-infratuzilma.md`) ichida ishlaydigan
/// `AnalysisWorkerBackgroundService` ketma-ket (bitta poll-tsiklda, bitta `DbContext` bilan)
/// `Pending` yozuvlarni `Running`ga o'tkazadi — shu sabab bu yerda ko'p-instansli poyga holatidan
/// himoya (masalan `SELECT ... FOR UPDATE SKIP LOCKED`/optimistik konkurentlik) YO'Q. Gorizontal
/// masshtablashda (bir nechta API instansi) bu klass kengaytirilishi kerak (PM'ga hisobotda
/// alohida qayd etilgan — `docs/06` qarorlar jurnaliga yozish PM zimmasida, chunki `docs/` bu
/// promptning fayl egaligi tashqarisida).
/// </para>
/// </summary>
public sealed class AnalysisJob : Entity
{
    public Guid AssessmentId { get; private set; }

    /// <summary>`RerunAnalysisCommand`da aniq provider so'ralgan bo'lsa — aks holda `null` (default tanlanadi).</summary>
    public AiProvider? RequestedProvider { get; private set; }

    /// <summary>`RerunAnalysisCommand`da aniq prompt versiyasi so'ralgan bo'lsa — aks holda `null` (faol versiya).</summary>
    public string? RequestedPromptVersion { get; private set; }

    public AnalysisJobStatus Status { get; private set; }

    /// <summary>Vazifa nechchi marta olib ishga tushirilgani (crash-recovery diagnostikasi uchun) — orkestratsiya ICHIDAGI AI urinishlari (`AiAnalysis.AttemptNumber`) EMAS.</summary>
    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AnalysisJob()
    {
    }

    private AnalysisJob(Guid id, Guid assessmentId, AiProvider? requestedProvider, string? requestedPromptVersion, DateTimeOffset now)
        : base(id)
    {
        AssessmentId = assessmentId;
        RequestedProvider = requestedProvider;
        RequestedPromptVersion = requestedPromptVersion;
        Status = AnalysisJobStatus.Pending;
        AttemptCount = 0;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static AnalysisJob Create(
        Guid id,
        Guid assessmentId,
        DateTimeOffset now,
        AiProvider? requestedProvider = null,
        string? requestedPromptVersion = null)
    {
        if (assessmentId == Guid.Empty)
        {
            throw new ArgumentException("Sessiya identifikatori bo'sh bo'lishi mumkin emas.", nameof(assessmentId));
        }

        return new AnalysisJob(id, assessmentId, requestedProvider, requestedPromptVersion, now);
    }

    /// <summary>`Pending`/`Failed` (qayta urinish uchun) ──▶ `Running`: worker vazifani olganda.</summary>
    public void Start(DateTimeOffset now)
    {
        if (Status is not (AnalysisJobStatus.Pending or AnalysisJobStatus.Failed))
        {
            throw new DomainException("ANALYSIS_JOB_INVALID_TRANSITION", $"Vazifa '{Status}' holatidan 'Running' ga o'ta olmaydi.");
        }

        Status = AnalysisJobStatus.Running;
        AttemptCount++;
        UpdatedAt = now;
    }

    /// <summary>`Running ──▶ Completed`: orkestrator xatosiz tugatganda (natija — muvaffaqiyat YOKI to'liq muvaffaqiyatsizlik — orkestratsiya ICHIDA hal qilinadi; bu shunchaki "vazifa ishlab bo'ldi").</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status != AnalysisJobStatus.Running)
        {
            throw new DomainException("ANALYSIS_JOB_INVALID_TRANSITION", $"Vazifa '{Status}' holatidan 'Completed' ga o'ta olmaydi.");
        }

        Status = AnalysisJobStatus.Completed;
        UpdatedAt = now;
    }

    /// <summary>`Running ──▶ Failed`: orkestratorning o'zi kutilmagan istisno bilan yiqilganda (masalan DB ulanishi uzilganda) — AI provayder xatolari BU YERGA kirmaydi, ular orkestrator ichida `AiAnalysis.Fail`ga yoziladi va vazifa baribir `Completed` bo'ladi.</summary>
    public void Fail(string error, DateTimeOffset now)
    {
        if (Status != AnalysisJobStatus.Running)
        {
            throw new DomainException("ANALYSIS_JOB_INVALID_TRANSITION", $"Vazifa '{Status}' holatidan 'Failed' ga o'ta olmaydi.");
        }

        Status = AnalysisJobStatus.Failed;
        LastError = error;
        UpdatedAt = now;
    }
}
