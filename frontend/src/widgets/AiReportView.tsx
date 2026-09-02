import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export interface AiStrength {
  title: string;
  description: string;
  evidence: string;
}

export interface AiGrowthArea {
  title: string;
  description: string;
  actionStep: string;
}

export interface AiCareerSuggestion {
  field: string;
  why: string;
  exampleProfessions?: string[] | null;
  nextSteps: string[];
}

export type AiAttentionFlagSeverity = 'info' | 'attention' | 'high';

export interface AiAttentionFlag {
  code: string;
  message: string;
  severity: AiAttentionFlagSeverity;
}

/**
 * AI hisobotining mazmun bo'limlari — docs/09-ai-analiz-moduli.md, 5-bo'lim JSON sxemasi bilan
 * bir xil maydonlar. Har biri **ixtiyoriy/`null`** — CLAUDE.md "MAXSUS DIQQAT — Bo'sh
 * ma'lumotga chidamlilik": P16–P18 (AI backend) hali yozilmagan, shu sabab bu widget har doim
 * bo'sh/`null` massiv va matnlar bilan ham yiqilmasdan render bo'lishi shart.
 */
export interface AiReportSections {
  summary?: string | null;
  personalityPortrait?: string | null;
  strengths?: AiStrength[] | null;
  growthAreas?: AiGrowthArea[] | null;
  learningStyle?: string | null;
  motivationProfile?: string | null;
  activityAssessment?: string | null;
  careerSuggestions?: AiCareerSuggestion[] | null;
  studentRecommendations?: string[] | null;
  teacherNotes?: string[] | null;
  parentNotes?: string[] | null;
  attentionFlags?: AiAttentionFlag[] | null;
  disclaimer?: string | null;
}

export interface AiReportViewProps {
  sections: AiReportSections;
  className?: string;
}

const SEVERITY_CLASSES: Record<AiAttentionFlagSeverity, string> = {
  info: 'bg-neutral-100 text-neutral-700',
  attention: 'bg-warning-100 text-warning-700',
  high: 'bg-danger-100 text-danger-700',
};

/**
 * Bitta bo'lim — native `<details open>` orqali akkordeon (P26 7-band: "akkordeon yoki
 * ketma-ket kartalar"). `open` standart holatda bor — chop etishda (`@media print`) hech
 * narsa yashirilmasin (P25 9-band) va klaviatura/ekran o'quvchisi uchun qo'shimcha ARIA
 * kerak bo'lmasin (native semantika bepul beradi).
 */
function ReportSection({ title, children }: { title: ReactNode; children: ReactNode }) {
  return (
    <details
      open
      className="rounded-xl border border-neutral-200 bg-white p-4 [&_summary::-webkit-details-marker]:hidden"
    >
      <summary className="cursor-pointer text-sm font-semibold text-neutral-900 marker:content-none">
        {title}
      </summary>
      <div className="mt-3 flex flex-col gap-3 text-sm text-neutral-700">{children}</div>
    </details>
  );
}

/**
 * AI javobini bo'limlarga ajratib render qiladi — docs/11 A-5, P26 7-band. Matn **oddiy
 * matn** sifatida chiqadi (`{text}` — React avtomatik escape qiladi), `dangerouslySetInnerHTML`
 * ISHLATILMAGAN (CLAUDE.md 12-qoida, `docs/10` 7-bo'lim).
 */
export function AiReportView({ sections, className }: AiReportViewProps) {
  const { t } = useTranslation();
  const strengths = sections.strengths ?? [];
  const growthAreas = sections.growthAreas ?? [];
  const careerSuggestions = sections.careerSuggestions ?? [];
  const studentRecommendations = sections.studentRecommendations ?? [];
  const teacherNotes = sections.teacherNotes ?? [];
  const parentNotes = sections.parentNotes ?? [];
  const attentionFlags = sections.attentionFlags ?? [];

  const hasAnyContent =
    Boolean(sections.summary) ||
    Boolean(sections.personalityPortrait) ||
    strengths.length > 0 ||
    growthAreas.length > 0 ||
    Boolean(sections.learningStyle) ||
    Boolean(sections.motivationProfile) ||
    Boolean(sections.activityAssessment) ||
    careerSuggestions.length > 0 ||
    studentRecommendations.length > 0 ||
    teacherNotes.length > 0 ||
    parentNotes.length > 0;

  if (!hasAnyContent) {
    return (
      <p className={cn('text-sm text-neutral-500', className)}>{t('widgets.aiReportView.empty')}</p>
    );
  }

  return (
    <div className={cn('flex flex-col gap-3', className)}>
      {attentionFlags.length > 0 && (
        <div className="rounded-xl border border-warning-300 bg-warning-50 p-4">
          <h4 className="mb-2 text-sm font-semibold text-neutral-900">
            {t('widgets.aiReportView.attentionFlagsTitle')}
          </h4>
          <ul className="flex flex-col gap-2">
            {attentionFlags.map((flag, index) => (
              <li key={`${flag.code}-${String(index)}`} className="flex items-start gap-2 text-sm">
                <AlertTriangle
                  size={16}
                  className="mt-0.5 shrink-0 text-warning-600"
                  aria-hidden="true"
                />
                <span className="flex-1 text-neutral-800">{flag.message}</span>
                <span
                  className={cn(
                    'shrink-0 rounded-full px-2 py-0.5 text-xs font-medium',
                    SEVERITY_CLASSES[flag.severity],
                  )}
                >
                  {t(`widgets.aiReportView.severity.${flag.severity}`)}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {sections.summary && (
        <ReportSection title={t('widgets.aiReportView.section.summary')}>
          <p>{sections.summary}</p>
        </ReportSection>
      )}

      {sections.personalityPortrait && (
        <ReportSection title={t('widgets.aiReportView.section.personalityPortrait')}>
          <p className="whitespace-pre-line">{sections.personalityPortrait}</p>
        </ReportSection>
      )}

      {strengths.length > 0 && (
        <ReportSection
          title={t('widgets.aiReportView.section.strengths', { count: strengths.length })}
        >
          <ul className="flex flex-col gap-3">
            {strengths.map((strength, index) => (
              <li key={`${strength.title}-${String(index)}`}>
                <p className="font-medium text-neutral-900">{strength.title}</p>
                <p>{strength.description}</p>
                <p className="mt-0.5 text-xs text-neutral-500">{strength.evidence}</p>
              </li>
            ))}
          </ul>
        </ReportSection>
      )}

      {growthAreas.length > 0 && (
        <ReportSection
          title={t('widgets.aiReportView.section.growthAreas', { count: growthAreas.length })}
        >
          <ul className="flex flex-col gap-3">
            {growthAreas.map((area, index) => (
              <li key={`${area.title}-${String(index)}`}>
                <p className="font-medium text-neutral-900">{area.title}</p>
                <p>{area.description}</p>
                <p className="mt-0.5 text-neutral-600">
                  <span className="font-medium">{t('widgets.aiReportView.actionStepLabel')}</span>{' '}
                  {area.actionStep}
                </p>
              </li>
            ))}
          </ul>
        </ReportSection>
      )}

      {(sections.learningStyle || sections.motivationProfile || sections.activityAssessment) && (
        <ReportSection title={t('widgets.aiReportView.section.profile')}>
          {sections.learningStyle && (
            <div>
              <p className="font-medium text-neutral-900">
                {t('widgets.aiReportView.learningStyleLabel')}
              </p>
              <p>{sections.learningStyle}</p>
            </div>
          )}
          {sections.motivationProfile && (
            <div>
              <p className="font-medium text-neutral-900">
                {t('widgets.aiReportView.motivationProfileLabel')}
              </p>
              <p>{sections.motivationProfile}</p>
            </div>
          )}
          {sections.activityAssessment && (
            <div>
              <p className="font-medium text-neutral-900">
                {t('widgets.aiReportView.activityAssessmentLabel')}
              </p>
              <p>{sections.activityAssessment}</p>
            </div>
          )}
        </ReportSection>
      )}

      {careerSuggestions.length > 0 && (
        <ReportSection
          title={t('widgets.aiReportView.section.careerSuggestions', {
            count: careerSuggestions.length,
          })}
        >
          <ul className="flex flex-col gap-3">
            {careerSuggestions.map((suggestion, index) => (
              <li key={`${suggestion.field}-${String(index)}`}>
                <p className="font-medium text-neutral-900">{suggestion.field}</p>
                <p>{suggestion.why}</p>
                {suggestion.exampleProfessions && suggestion.exampleProfessions.length > 0 && (
                  <p className="mt-0.5 text-xs text-neutral-500">
                    {suggestion.exampleProfessions.join(' · ')}
                  </p>
                )}
                {suggestion.nextSteps.length > 0 && (
                  <ul className="mt-1 list-inside list-disc">
                    {suggestion.nextSteps.map((step, stepIndex) => (
                      <li key={`${suggestion.field}-step-${String(stepIndex)}`}>{step}</li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        </ReportSection>
      )}

      {(studentRecommendations.length > 0 ||
        teacherNotes.length > 0 ||
        parentNotes.length > 0) && (
        <ReportSection title={t('widgets.aiReportView.section.recommendations')}>
          {studentRecommendations.length > 0 && (
            <div>
              <p className="font-medium text-neutral-900">
                {t('widgets.aiReportView.audienceStudent')}
              </p>
              <ul className="list-inside list-disc">
                {studentRecommendations.map((item, index) => (
                  <li key={`student-${String(index)}`}>{item}</li>
                ))}
              </ul>
            </div>
          )}
          {teacherNotes.length > 0 && (
            <div>
              <p className="font-medium text-neutral-900">
                {t('widgets.aiReportView.audienceTeacher')}
              </p>
              <ul className="list-inside list-disc">
                {teacherNotes.map((item, index) => (
                  <li key={`teacher-${String(index)}`}>{item}</li>
                ))}
              </ul>
            </div>
          )}
          {parentNotes.length > 0 && (
            <div>
              <p className="font-medium text-neutral-900">
                {t('widgets.aiReportView.audienceParent')}
              </p>
              <ul className="list-inside list-disc">
                {parentNotes.map((item, index) => (
                  <li key={`parent-${String(index)}`}>{item}</li>
                ))}
              </ul>
            </div>
          )}
        </ReportSection>
      )}

      {sections.disclaimer && (
        <p className="rounded-xl border border-neutral-200 bg-neutral-50 p-4 text-xs text-neutral-500">
          {sections.disclaimer}
        </p>
      )}
    </div>
  );
}
