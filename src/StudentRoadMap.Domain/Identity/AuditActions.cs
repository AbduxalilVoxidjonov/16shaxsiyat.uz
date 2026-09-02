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
    // ro'yxat qilingan sessiya harakatlari. `Assessment.AnalysisRerun` bu yerda YO'Q — u
    // `rerun-analysis` (AI, P16-P18) uchun, P15 qamroviga kirmaydi. ---

    public const string AssessmentDeleted = "Assessment.Deleted";
    public const string AssessmentScoresRecalculated = "Assessment.ScoresRecalculated";

    // --- P34 (`prompts/34-dastur-modeli-va-biriktirish.md`) — `AssessmentProgram` admin
    // harakatlari (E16-band ro'yxati: "Program.Created/Updated/Published/Archived/Assigned/
    // Unassigned"). ---

    public const string ProgramCreated = "Program.Created";
    public const string ProgramUpdated = "Program.Updated";
    public const string ProgramPublished = "Program.Published";
    public const string ProgramArchived = "Program.Archived";
    public const string ProgramToggledActive = "Program.ToggledActive";
    public const string ProgramTestAdded = "Program.TestAdded";
    public const string ProgramTestRemoved = "Program.TestRemoved";
    public const string ProgramTestsReordered = "Program.TestsReordered";
    public const string ProgramSchoolAssigned = "Program.Assigned";
    public const string ProgramSchoolUnassigned = "Program.Unassigned";
}
