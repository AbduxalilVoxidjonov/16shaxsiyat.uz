import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { formatDateTime } from '@/shared/lib/formatDate';
import { AnswersBody, AnswersSection } from '@/widgets/AnswersSection';
import { useRawAnswersQuery } from '@/widgets/useRawAnswersQuery';
import type { AssessmentSummaryDto } from '../model/profileTypes';

export interface StudentAnswersSectionProps {
  /** Profildagi barcha sessiyalar (`assessments[]`, eng yangisi birinchi). */
  assessments: readonly AssessmentSummaryDto[];
  /** Eng so'nggi sessiya ID'si — bitta urinish holatida hozirgidek shu ko'rsatiladi. */
  latestAssessmentId: string | null;
  /** Kod → katalog nomi zaxirasi (`latestAssessment.tests`). */
  testNames?: Readonly<Record<string, string>>;
}

/** Urinish — YAKUNLANGAN sessiya (`completedAt` bor) va uning xronologik tartib raqami. */
interface Attempt {
  assessment: AssessmentSummaryDto;
  number: number;
}

/**
 * Profildagi "Savolma-savol javoblar" (egasining talabi, 2026-09-23).
 *
 * - Yakunlangan urinish 0 yoki 1 ta bo'lsa — HOZIRGIDEK: eng so'nggi sessiya javoblari
 *   yig'ish tugmasisiz, doim ochiq (`widgets/AnswersSection`).
 * - 2+ bo'lsa — har urinish alohida yig'iladigan blok (eng yangisi tepada va standart
 *   OCHIQ, qolganlari YIG'ILGAN). Yig'ilgan blok tanasi render qilinmaydi, ya'ni javoblari
 *   faqat ochilganda so'raladi.
 */
export function StudentAnswersSection({
  assessments,
  latestAssessmentId,
  testNames,
}: StudentAnswersSectionProps) {
  const { t } = useTranslation();

  const completed = assessments.filter((assessment) => assessment.completedAt);
  if (completed.length <= 1) {
    return <AnswersSection assessmentId={latestAssessmentId} testNames={testNames} />;
  }

  // Tartib raqami eng eskisidan (1-urinish), ko'rsatish esa eng yangisidan boshlab.
  const chronological = [...completed].sort((a, b) => a.startedAt.localeCompare(b.startedAt));
  const attempts: Attempt[] = chronological
    .map((assessment, index) => ({ assessment, number: index + 1 }))
    .reverse();

  return (
    <Card>
      <h3 className="text-base font-semibold text-neutral-900">
        {t('studentProfile.answers.title')}
      </h3>
      <p className="text-xs text-neutral-500">{t('studentProfile.answers.description')}</p>

      <div className="mt-4 flex flex-col gap-3">
        {attempts.map((attempt, index) => (
          <AttemptBlock
            key={attempt.assessment.id}
            attempt={attempt}
            defaultOpen={index === 0}
            testNames={testNames}
          />
        ))}
      </div>
    </Card>
  );
}

function AttemptBlock({
  attempt,
  defaultOpen,
  testNames,
}: {
  attempt: Attempt;
  defaultOpen: boolean;
  testNames?: Readonly<Record<string, string>>;
}) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(defaultOpen);
  const panelId = useId();
  const { assessment } = attempt;

  // Javoblar soni faqat blok ochilganda ma'lum (yig'ilgan blok uchun so'rov yuborilmaydi).
  // Kalit `AnswersBody` bilan bir xil — bitta so'rov, ikki o'quvchi.
  const answersQuery = useRawAnswersQuery(assessment.id, open);
  const answerCount = answersQuery.data?.answers.length;

  return (
    <div className="rounded-xl border border-neutral-200">
      <h4>
        <button
          type="button"
          aria-expanded={open}
          aria-controls={panelId}
          onClick={() => setOpen((value) => !value)}
          className="flex w-full items-center justify-between gap-2 px-4 py-3 text-left"
        >
          <span className="text-sm">
            <span className="font-semibold text-neutral-900">
              {assessment.programNameUz ?? t('studentProfile.answers.attempt.fallbackTitle')}
            </span>{' '}
            <span className="text-neutral-500">
              {t('studentProfile.answers.attempt.meta', {
                number: attempt.number,
                date: formatDateTime(assessment.completedAt ?? assessment.startedAt),
              })}
              {answerCount !== undefined &&
                ` ${t('studentProfile.answers.groupCount', { count: answerCount })}`}
            </span>
          </span>
          <ChevronDown
            size={16}
            aria-hidden="true"
            className={open ? 'shrink-0 rotate-180 text-neutral-500' : 'shrink-0 text-neutral-500'}
          />
        </button>
      </h4>

      {open && (
        <div id={panelId} className="border-t border-neutral-200 px-4 py-3">
          <AnswersBody assessmentId={assessment.id} testNames={testNames} groupHeadingLevel="h5" />
        </div>
      )}
    </div>
  );
}
