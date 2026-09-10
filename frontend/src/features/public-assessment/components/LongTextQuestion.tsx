import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';

export interface LongTextQuestionProps {
  /** `type === 'LongText'` (`docs/18` §6.2). */
  question: BranchingQuestion;
  /** Joriy matn javobi (mahalliy kesh yoki server `currentText`dan) — hali javob yo'q bo'lsa `null`. */
  value: string | null;
  /** "Keyingi" bosilganda to'ldirilmagan deb belgilangan bo'lsa qizil ramka + xabar. */
  invalid: boolean;
  onAnswer: (payload: { text: string }) => void;
  questionRef?: (node: HTMLDivElement | null) => void;
}

const DEFAULT_MAX_LENGTH = 2000;

/**
 * Ko'p qatorli matn savoli — `LongText` (`docs/18` §6.2): `textarea` + belgi hisoblagichi
 * (`n / maxLength`). `onAdvance` QABUL QILINMAYDI — `Enter` textareada yangi qator yaratishi
 * kerak (bitta qatorli `TextQuestion`dan farqli o'laroq keyingi savolga o'tishni to'sib
 * qo'ymasligi uchun), shu sabab klaviatura bilan keyingi savolga o'tish "Keyingi" tugmasi
 * orqali bo'ladi.
 */
export function LongTextQuestion({
  question,
  value,
  invalid,
  onAnswer,
  questionRef,
}: LongTextQuestionProps) {
  const { t } = useTranslation();
  const legendId = useId();
  const textareaId = useId();
  const errorId = useId();
  const counterId = useId();

  const maxLength = question.maxLength ?? DEFAULT_MAX_LENGTH;
  const draft = value ?? '';
  const answered = draft.trim().length > 0;

  return (
    <div
      className={cn(
        'card px-5 py-7 transition-colors duration-300 sm:px-8',
        answered && 'border-firuza-200 bg-firuza-50/40',
        invalid && 'border-terakota-600 bg-terakota-50/50',
      )}
    >
      <label
        htmlFor={textareaId}
        id={legendId}
        className="float-left mb-7 block w-full text-center font-display text-lg leading-snug font-extrabold text-balance text-ink sm:text-xl"
      >
        <span className="mx-auto mb-3 grid size-8 place-items-center rounded-full bg-firuza-50 font-sans text-[13px] font-bold text-firuza-700">
          {question.order}
        </span>
        {question.text}
      </label>

      <div
        ref={questionRef}
        data-question-id={question.id}
        tabIndex={-1}
        className="clear-both focus:outline-none"
      >
        <textarea
          id={textareaId}
          value={draft}
          maxLength={maxLength}
          placeholder={question.placeholder ?? undefined}
          rows={4}
          aria-invalid={invalid || undefined}
          aria-describedby={[invalid ? errorId : null, counterId].filter(Boolean).join(' ')}
          onChange={(event) => {
            onAnswer({ text: event.target.value });
          }}
          className={cn(
            'min-h-28 w-full rounded-2xl border border-ink-faint bg-paper-card px-4 py-3 text-base text-ink',
            'placeholder:text-ink-muted',
            'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600',
            invalid && 'border-terakota-600',
          )}
        />
        <p id={counterId} className="mt-2 text-right text-xs font-medium text-ink-muted">
          {t('test.question.charCount', { count: draft.length, max: maxLength })}
        </p>
      </div>

      {invalid && (
        <p id={errorId} role="alert" className="mt-2 text-center text-sm font-medium text-terakota-700">
          {t('test.question.requiredError')}
        </p>
      )}
    </div>
  );
}
