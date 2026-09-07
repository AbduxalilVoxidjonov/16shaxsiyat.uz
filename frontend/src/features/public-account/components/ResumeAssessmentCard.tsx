import { useTranslation } from 'react-i18next';
import { Play } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { formatDate } from '@/shared/lib/formatDate';
import type { MyAssessment } from '@/shared/api/types';

export interface ResumeAssessmentCardProps {
  assessment: MyAssessment;
  onResume: () => void;
  isResuming: boolean;
  error?: string | null;
}

/**
 * Kabinet tepasidagi "Sizda tugallanmagan test bor" kartasi — faqat tarixda tugallanmagan
 * sessiya bo'lganda ko'rsatiladi (`findUnfinishedAssessment`).
 *
 * Bu yerda faqat dastur nomi va boshlangan sana: `GET /api/me/assessments` da blok/savol
 * progressi YO'Q (`docs/07` §5.2), u faqat sessiya tiklangach `tests[]` da keladi. Progressni
 * bu yerda ko'rsatish alohida (backend) vazifa.
 */
export function ResumeAssessmentCard({
  assessment,
  onResume,
  isResuming,
  error,
}: ResumeAssessmentCardProps) {
  const { t } = useTranslation();

  return (
    <section
      className="card flex flex-col gap-4 border-firuza-200 bg-firuza-50 p-5 sm:flex-row sm:items-center sm:justify-between sm:p-6"
      aria-labelledby="account-resume-heading"
    >
      <div className="min-w-0">
        <p className="eyebrow text-firuza-700">{t('account.resume.eyebrow')}</p>
        <h2
          id="account-resume-heading"
          className="font-display mt-1 text-lg font-extrabold tracking-tight text-ink"
        >
          {t('account.resume.title')}
        </h2>
        <p className="mt-1 text-[13px] text-ink-soft">
          {t('account.resume.lead', {
            programName: assessment.programName,
            date: formatDate(assessment.startedAt),
          })}
        </p>
        {error && (
          <p
            role="alert"
            className="mt-3 rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
          >
            {error}
          </p>
        )}
      </div>

      <Button type="button" size="lg" className="shrink-0" isLoading={isResuming} onClick={onResume}>
        <Play className="size-4" aria-hidden="true" />
        {t('account.resume.cta')}
      </Button>
    </section>
  );
}
