import { useId, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import type { PublicQuestion, PublicScaleLabel } from '@/shared/api/types';

export interface LikertQuestionProps {
  question: PublicQuestion;
  scaleLabels: readonly PublicScaleLabel[];
  /** Tanlangan qiymat (mahalliy kesh yoki server `currentValue`dan) — hali javob yo'q bo'lsa `null`. */
  value: number | null;
  /** "Keyingi" bosilganda to'ldirilmagan deb belgilangan bo'lsa qizil ramka + xabar (docs/11 E-3). */
  invalid: boolean;
  onAnswer: (value: number) => void;
  /** `Enter` bosilganda yoki javob tanlangandan keyin keyingi savolga o'tish uchun (docs/10, 4.3-bo'lim). */
  onAdvance: () => void;
  questionRef?: (node: HTMLDivElement | null) => void;
}

/**
 * Bitta Likert savoli — 5 ta katta tugma (≥ 44px), `fieldset`/`legend` + `radiogroup` (a11y,
 * docs/11 4-bo'lim). Klaviatura: `1..5` javob tanlaydi, `Enter` keyingi savolga o'tadi
 * (CLAUDE.md MAXSUS DIQQAT 3-band). Klaviatura tinglovchisi va fokus/ko'rinish `ref`i `radiogroup`
 * ustida (`fieldset`da EMAS) — `fieldset` interaktiv ARIA rolga ega emas
 * (`jsx-a11y/no-noninteractive-element-interactions`), `radiogroup` esa mos widget rol.
 */
export function LikertQuestion({
  question,
  scaleLabels,
  value,
  invalid,
  onAnswer,
  onAdvance,
  questionRef,
}: LikertQuestionProps) {
  const { t } = useTranslation();
  const legendId = useId();
  const errorId = useId();

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key >= '1' && event.key <= '5') {
      const index = Number(event.key) - 1;
      const option = scaleLabels[index];
      if (option) {
        event.preventDefault();
        onAnswer(option.value);
      }
      return;
    }
    if (event.key === 'Enter') {
      event.preventDefault();
      onAdvance();
    }
  }

  const answered = value !== null;

  return (
    <fieldset
      className={cn(
        'rounded-xl border-2 bg-white p-4',
        answered ? 'border-success-300' : invalid ? 'border-danger-500' : 'border-neutral-200',
      )}
    >
      <legend id={legendId} className="mb-3 text-base font-medium text-neutral-900">
        <span className="mr-1.5 text-neutral-400">{question.order}.</span>
        {question.text}
      </legend>
      <div
        ref={questionRef}
        data-question-id={question.id}
        tabIndex={-1}
        role="radiogroup"
        aria-labelledby={legendId}
        aria-describedby={invalid ? errorId : undefined}
        onKeyDown={handleKeyDown}
        className="flex flex-col gap-2 focus:outline-none"
      >
        {scaleLabels.map((option) => {
          const checked = value === option.value;
          return (
            <label
              key={option.value}
              className={cn(
                'flex min-h-11 cursor-pointer items-center gap-3 rounded-lg border px-3 py-2 text-sm transition-colors',
                'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-primary-600',
                checked
                  ? 'border-primary-600 bg-primary-50 font-medium text-primary-800'
                  : 'border-neutral-300 text-neutral-700 hover:bg-neutral-50',
              )}
            >
              <input
                type="radio"
                name={`question-${question.id}`}
                value={option.value}
                checked={checked}
                onChange={() => onAnswer(option.value)}
                className="sr-only"
              />
              {option.label}
            </label>
          );
        })}
      </div>
      {invalid && (
        <p id={errorId} role="alert" className="mt-2 text-sm text-danger-600">
          {t('test.question.requiredError')}
        </p>
      )}
    </fieldset>
  );
}
