namespace StudentRoadMap.Domain.Jobs;

/// <summary>
/// `analysis_jobs.status` — fon navbatidagi bitta vazifaning holati (P18, `prompts/18`
/// "IBackgroundJobQueue implementatsiyasi: ... `analysis_jobs` jadvali + `BackgroundService`").
/// Bu — TASHQI navbat yozuvi holati (bir marta ishlaydi/ishlamaydi), `AiAnalysis.Status`
/// (har HTTP urinishning natijasi) bilan ARALASHTIRILMASIN.
/// </summary>
public enum AnalysisJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}
