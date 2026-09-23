/**
 * Ma'lum harakat/obyekt turlari — filtr select'lari uchun (`docs/11` A-9: "filtr: harakat
 * turi, obyekt, sana"). `GET /api/admin/audit-logs?action=&entityType=` ANIQ moslikda ishlaydi
 * (`ListAuditLogsQueryHandler` izohi), shu sabab erkin matn emas — qat'iy ro'yxat.
 *
 * Ro'yxat ikki manbadan birlashtirilgan:
 * 1. `src/StudentRoadMap.Domain/Identity/AuditActions.cs` — HOZIR real yoziladigan qiymatlar
 *    (Auth/School/Student/Assessment/Program).
 * 2. `docs/08-auth-va-xavfsizlik.md` §8 — to'liq REJALASHTIRILGAN ro'yxat, shu jumladan
 *    `AiConfig.*`/`Catalog.QuestionUpdated`/`Export.StudentsDownloaded` — bular hali
 *    `AuditActions.cs`da yo'q (AI sozlamalari va katalog UI'lari parallel/boshqa promptlarda
 *    yozilmoqda). Bu qiymatlar filtrga OLDINDAN qo'shilgan: agar hali yozuv yo'q bo'lsa filtr
 *    shunchaki bo'sh natija qaytaradi — xato emas.
 */
export interface AuditActionOption {
  value: string;
  labelKey: string;
}

export const AUDIT_ACTION_OPTIONS: readonly AuditActionOption[] = [
  { value: 'Auth.LoginSucceeded', labelKey: 'audit.actionLabels.AuthLoginSucceeded' },
  { value: 'Auth.LoginFailed', labelKey: 'audit.actionLabels.AuthLoginFailed' },
  { value: 'Auth.PasswordChanged', labelKey: 'audit.actionLabels.AuthPasswordChanged' },
  {
    value: 'Auth.TotpEnrollmentStarted',
    labelKey: 'audit.actionLabels.AuthTotpEnrollmentStarted',
  },
  { value: 'Auth.TotpEnabled', labelKey: 'audit.actionLabels.AuthTotpEnabled' },
  { value: 'Auth.TotpDisabled', labelKey: 'audit.actionLabels.AuthTotpDisabled' },
  { value: 'Security.RefreshReuse', labelKey: 'audit.actionLabels.SecurityRefreshReuse' },
  { value: 'School.Created', labelKey: 'audit.actionLabels.SchoolCreated' },
  { value: 'School.Updated', labelKey: 'audit.actionLabels.SchoolUpdated' },
  { value: 'School.Deleted', labelKey: 'audit.actionLabels.SchoolDeleted' },
  { value: 'School.LinkRegenerated', labelKey: 'audit.actionLabels.SchoolLinkRegenerated' },
  { value: 'School.ToggledActive', labelKey: 'audit.actionLabels.SchoolToggledActive' },
  { value: 'Student.Deleted', labelKey: 'audit.actionLabels.StudentDeleted' },
  { value: 'Assessment.Deleted', labelKey: 'audit.actionLabels.AssessmentDeleted' },
  {
    value: 'Assessment.ScoresRecalculated',
    labelKey: 'audit.actionLabels.AssessmentScoresRecalculated',
  },
  { value: 'Assessment.AnalysisRerun', labelKey: 'audit.actionLabels.AssessmentAnalysisRerun' },
  { value: 'Program.Created', labelKey: 'audit.actionLabels.ProgramCreated' },
  { value: 'Program.Updated', labelKey: 'audit.actionLabels.ProgramUpdated' },
  { value: 'Program.Published', labelKey: 'audit.actionLabels.ProgramPublished' },
  { value: 'Program.Archived', labelKey: 'audit.actionLabels.ProgramArchived' },
  { value: 'Program.Restored', labelKey: 'audit.actionLabels.ProgramRestored' },
  { value: 'Program.ToggledActive', labelKey: 'audit.actionLabels.ProgramToggledActive' },
  { value: 'Program.TestAdded', labelKey: 'audit.actionLabels.ProgramTestAdded' },
  { value: 'Program.TestRemoved', labelKey: 'audit.actionLabels.ProgramTestRemoved' },
  { value: 'Program.TestsReordered', labelKey: 'audit.actionLabels.ProgramTestsReordered' },
  { value: 'Program.Assigned', labelKey: 'audit.actionLabels.ProgramSchoolAssigned' },
  { value: 'Program.Unassigned', labelKey: 'audit.actionLabels.ProgramSchoolUnassigned' },
  { value: 'AiConfig.Updated', labelKey: 'audit.actionLabels.AiConfigUpdated' },
  { value: 'AiConfig.KeyChanged', labelKey: 'audit.actionLabels.AiConfigKeyChanged' },
  { value: 'Catalog.QuestionUpdated', labelKey: 'audit.actionLabels.CatalogQuestionUpdated' },
  // 2026-09-23 (`docs/07` §3.4.1): test biriktirmasi — ommaviy/maktablar/ommaviy makon/rejim.
  {
    value: 'Catalog.TestAssignmentUpdated',
    labelKey: 'audit.actionLabels.CatalogTestAssignmentUpdated',
  },
  { value: 'Export.StudentsDownloaded', labelKey: 'audit.actionLabels.ExportStudentsDownloaded' },
];

export interface AuditEntityTypeOption {
  value: string;
  labelKey: string;
}

export const AUDIT_ENTITY_TYPE_OPTIONS: readonly AuditEntityTypeOption[] = [
  { value: 'School', labelKey: 'audit.entityTypeLabels.School' },
  { value: 'Student', labelKey: 'audit.entityTypeLabels.Student' },
  { value: 'Assessment', labelKey: 'audit.entityTypeLabels.Assessment' },
  { value: 'AssessmentProgram', labelKey: 'audit.entityTypeLabels.AssessmentProgram' },
  { value: 'TestDefinition', labelKey: 'audit.entityTypeLabels.TestDefinition' },
  { value: 'AiProviderConfig', labelKey: 'audit.entityTypeLabels.AiProviderConfig' },
];

/** Harakat kodini (masalan `School.Created`) o'zbekcha nomga o'giradi; noma'lum bo'lsa xom kodni qaytaradi. */
export function findAuditActionLabelKey(action: string): string | undefined {
  return AUDIT_ACTION_OPTIONS.find((option) => option.value === action)?.labelKey;
}

/** Obyekt turini o'zbekcha nomga o'giradi; noma'lum bo'lsa xom qiymatni qaytaradi. */
export function findAuditEntityTypeLabelKey(entityType: string): string | undefined {
  return AUDIT_ENTITY_TYPE_OPTIONS.find((option) => option.value === entityType)?.labelKey;
}
