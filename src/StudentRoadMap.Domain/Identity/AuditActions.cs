namespace StudentRoadMap.Domain.Identity;

/// <summary>
/// `AuditLog.Action` uchun qat'iy qiymatlar ro'yxati — `docs/08-auth-va-xavfsizlik.md`
/// 8-bo'limidagi auth bilan bog'liq harakatlar. `Auth.TotpDisabled` — `EnableTotp` bilan
/// simmetrik (`docs/13-auth-va-jwt.md` DisableTotp use-case'i talab qiladi), PM tomonidan
/// `docs/08`ga rasman kiritilgan.
/// </summary>
public static class AuditActions
{
    public const string AuthLoginSucceeded = "Auth.LoginSucceeded";
    public const string AuthLoginFailed = "Auth.LoginFailed";
    public const string AuthPasswordChanged = "Auth.PasswordChanged";
    /// <summary>
    /// `POST /api/auth/totp/enable` — o'rnatish BOSHLANDI (sir kutish holatida saqlandi).
    /// P46da qo'shildi: `Auth.TotpEnabled` endi faqat `confirm` muvaffaqiyatli tugaganda,
    /// ya'ni 2FA HAQIQATDAN yoqilganda yoziladi. Sirning o'zi hech qachon yozilmaydi.
    /// </summary>
    public const string AuthTotpEnrollmentStarted = "Auth.TotpEnrollmentStarted";

    public const string AuthTotpEnabled = "Auth.TotpEnabled";
    public const string AuthTotpDisabled = "Auth.TotpDisabled";
    public const string SecurityRefreshReuse = "Security.RefreshReuse";

    // --- P14 (`prompts/14-admin-maktab-va-oquvchi-api.md`) — `docs/08` 8-bo'limida ro'yxat
    // qilingan maktab/o'quvchi harakatlari. ---

    public const string SchoolCreated = "School.Created";
    public const string SchoolUpdated = "School.Updated";
    public const string SchoolDeleted = "School.Deleted";
    public const string SchoolLinkRegenerated = "School.LinkRegenerated";
    public const string SchoolToggledActive = "School.ToggledActive";
    public const string StudentDeleted = "Student.Deleted";

    // --- P15 (`prompts/15-sessiya-va-dashboard-api.md`) — `docs/05` §8 / `docs/08` §8da
    // ro'yxat qilingan sessiya harakatlari. `Assessment.AnalysisRerun` bu yerda YO'Q edi — u
    // `rerun-analysis` (AI, P18) uchun, P15 qamroviga kirmagan edi; P18da shu yerga qo'shildi. ---

    public const string AssessmentDeleted = "Assessment.Deleted";
    public const string AssessmentScoresRecalculated = "Assessment.ScoresRecalculated";

    // --- P18 (`prompts/18-ai-navbat-va-orkestratsiya.md`) — AI qayta tahlil va sozlamalar
    // harakatlari (`docs/07` §3.3/§3.5, `CLAUDE.md` MAXSUS DIQQAT #6: "Audit: AiConfig.Updated,
    // AiConfig.KeyChanged (kalit qiymati emas!)"). ---

    /// <summary>`POST /api/admin/assessments/{id}/rerun-analysis`.</summary>
    public const string AssessmentAnalysisRerun = "Assessment.AnalysisRerun";

    /// <summary>`PUT /api/admin/ai/providers/{provider}` — model/limit/faollik/tartib o'zgarganda (kalit o'zgarmasa ham).</summary>
    public const string AiConfigUpdated = "AiConfig.Updated";

    /// <summary>
    /// `PUT /api/admin/ai/providers/{provider}` ICHIDA, faqat API kalitining o'zi almashtirilganda
    /// QO'SHIMCHA ravishda (`AiConfigUpdated` bilan BIRGA) yoziladi — kalit QIYMATI hech qachon
    /// audit yozuvida bo'lmaydi, faqat "o'zgardi" fakti.
    /// </summary>
    public const string AiConfigKeyChanged = "AiConfig.KeyChanged";

    // --- P34 (`prompts/34-dastur-modeli-va-biriktirish.md`) — `AssessmentProgram` admin
    // harakatlari (E16-band ro'yxati: "Program.Created/Updated/Published/Archived/Assigned/
    // Unassigned"). ---

    public const string ProgramCreated = "Program.Created";
    public const string ProgramUpdated = "Program.Updated";
    public const string ProgramPublished = "Program.Published";
    public const string ProgramArchived = "Program.Archived";
    public const string ProgramRestored = "Program.Restored";
    public const string ProgramToggledActive = "Program.ToggledActive";
    public const string ProgramTestAdded = "Program.TestAdded";
    public const string ProgramTestRemoved = "Program.TestRemoved";
    public const string ProgramTestsReordered = "Program.TestsReordered";
    public const string ProgramSchoolAssigned = "Program.Assigned";
    public const string ProgramSchoolUnassigned = "Program.Unassigned";

    // --- P37 (`prompts/37-katalog-crud-backend.md`) — test katalogi CRUD admin harakatlari
    // (vazifa 11-band ro'yxati). ---

    public const string CatalogTestCreated = "Catalog.TestCreated";
    public const string CatalogTestUpdated = "Catalog.TestUpdated";
    public const string CatalogTestPublished = "Catalog.TestPublished";
    public const string CatalogTestArchived = "Catalog.TestArchived";
    public const string CatalogTestDeleted = "Catalog.TestDeleted";
    public const string CatalogTestDuplicated = "Catalog.TestDuplicated";
    public const string CatalogTestToggledActive = "Catalog.TestToggledActive";
    public const string CatalogScaleChanged = "Catalog.ScaleChanged";
    public const string CatalogQuestionAdded = "Catalog.QuestionAdded";
    public const string CatalogQuestionRemoved = "Catalog.QuestionRemoved";
    public const string CatalogQuestionUpdated = "Catalog.QuestionUpdated";
    public const string CatalogVersionBumped = "Catalog.VersionBumped";

    // --- P27 (`prompts/27-eksport-excel-va-pdf.md`) — `docs/08` §8 ro'yxatida bor. ---

    /// <summary>O'quvchilar ro'yxati `.xlsx` eksporti (`docs/08` §8) — kim, qachon, qaysi filtr, nechta qator.</summary>
    public const string ExportStudentsDownloaded = "Export.StudentsDownloaded";
}
