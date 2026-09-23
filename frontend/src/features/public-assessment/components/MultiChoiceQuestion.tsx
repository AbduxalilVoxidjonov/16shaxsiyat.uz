import { useId } from 'react';
import { Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';
import { RequiredMark } from '@/shared/ui/RequiredMark';

export interface MultiChoiceQuestionProps {
  /** `type === 'MultiChoice'` (`docs/18` §6.2). `options` majburiy (2 tadan kam — nashrda `QUESTION_OPTIONS_REQUIRED`). */
  question: BranchingQuestion;
  /** Joriy tanlangan qiymatlar (server `currentValues`dan yoki mahalliy kesh) — javob yo'q bo'lsa `[]`. */
  value: readonly number[];
  /** "Keyingi" bosilganda to'ldirilmagan deb belgilangan bo'lsa qizil ramka + xabar. */
  invalid: boolean;
  onAnswer: (payload: { selectedValues: number[] }) => void;
  questionRef?: (node: HTMLDivElement | null) => void;
}

/**
 * Ko'p tanlovli savol — `MultiChoice` (`docs/18` §6.2): checkbox kartalari, `LikertQuestion`
 * dagi `asCards` uslubiga vizual mos (yorliq to'liq bosiladi, ≥44px teginish maydoni).
 *
 * `minSelections`/`maxSelections`: standart `minSelections` — majburiy bo'lsa 1, aks holda 0
 * (`docs/18` §2.3 "Standart 1 (majburiy bo'lsa)"). `maxSelections`ga YETGANDA qolgan
 * belgilanmagan variantlar `disabled` bo'ladi — foydalanuvchi cheklovdan XATOLIKSIZ, oldindan
 * bilib o'tadi (avval belgilanganlardan birini bekor qilsa yana ochiladi).
 */
export function MultiChoiceQuestion({
  question,
  value,
  invalid,
  onAnswer,
  questionRef,
}: MultiChoiceQuestionProps) {
  const { t } = useTranslation();
  const legendId = useId();
  const errorId = useId();
  const hintId = useId();

  const options = [...(question.options ?? [])].sort((a, b) => a.order - b.order);
  const minSelections = question.minSelections ?? (question.isRequired ? 1 : 0);
  const maxSelections = question.maxSelections ?? null;
  const count = value.length;
  const atMax = maxSelections !== null && count >= maxSelections;
  const answered = count > 0;

  function toggle(optionValue: number) {
    if (value.includes(optionValue)) {
      onAnswer({ selectedValues: value.filter((v) => v !== optionValue) });
      return;
    }
    if (atMax) return; // chegaraga yetgan — yangi tanlov qo'shilmaydi (checkbox ham `disabled`)
    onAnswer({ selectedValues: [...value, optionValue] });
  }

  const hint =
    minSelections > 0 && maxSelections !== null
      ? t('test.question.selectionRangeHint', { min: minSelections, max: maxSelections })
      : minSelections > 0
        ? t('test.question.minSelectionsHint', { count: minSelections })
        : maxSelections !== null
          ? t('test.question.maxSelectionsHint', { count: maxSelections })
          : null;

  const showError = invalid;
  const errorMessage =
    minSelections > 0 && count < minSelections
      ? t('test.question.minSelectionsError', { count: minSelections })
      : t('test.question.requiredError');

  const describedBy = [hint ? hintId : null, showError ? errorId : null].filter(Boolean).join(' ');

  return (
    <fieldset
      className={cn(
        'card px-5 py-7 transition-colors duration-300 sm:px-8',
        answered && 'border-firuza-200 bg-firuza-50/40',
        showError && 'border-terakota-600 bg-terakota-50/50',
      )}
    >
      <legend
        id={legendId}
        className="float-left mb-2 w-full text-center font-display text-lg leading-snug font-extrabold text-balance text-ink sm:text-xl"
      >
        <span className="mx-auto mb-3 grid size-8 place-items-center rounded-full bg-firuza-50 font-sans text-[13px] font-bold text-firuza-700">
          {question.order}
        </span>
        {question.text}
        {question.isRequired && <RequiredMark />}
      </legend>

      {hint && (
        <p id={hintId} className="clear-both mb-5 text-center text-sm text-ink-muted">
          {hint}
        </p>
      )}

      <div
        ref={questionRef}
        data-question-id={question.id}
        tabIndex={-1}
        // `aria-labelledby` shart emas — `fieldset`/`legend` allaqachon guruhga nom beradi;
        // bu yerda faqat hint/xato matnini bog'lash uchun `aria-describedby` kerak.
        aria-describedby={describedBy || undefined}
        className={cn('flex flex-col gap-2.5 focus:outline-none', !hint && 'clear-both')}
      >
        {options.map((option) => {
          const checked = value.includes(option.value);
          const disabled = !checked && atMax;
          return (
            <label
              key={option.id}
              className={cn(
                'flex min-h-11 items-center gap-3 rounded-2xl border px-4 py-3 text-left text-sm transition-colors',
                'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-firuza-500',
                disabled
                  ? 'cursor-not-allowed border-line bg-paper-deep text-ink-faint'
                  : 'cursor-pointer border-line bg-paper-card text-ink-soft hover:border-firuza-300 hover:bg-firuza-50/40',
                checked && 'border-firuza-500 bg-firuza-50 font-semibold text-firuza-900',
              )}
            >
              <input
                type="checkbox"
                value={option.value}
                checked={checked}
                disabled={disabled}
                onChange={() => {
                  toggle(option.value);
                }}
                className="sr-only"
              />
              <span
                aria-hidden="true"
                className={cn(
                  'grid size-5 shrink-0 place-items-center rounded-md border-2 transition-colors',
                  checked ? 'border-firuza-500 bg-firuza-500 text-white' : 'border-line-strong',
                )}
              >
                {checked && <Check className="size-3" strokeWidth={3} aria-hidden="true" />}
              </span>
              {option.text}
            </label>
          );
        })}
      </div>

      {showError && (
        <p id={errorId} role="alert" className="mt-4 text-center text-sm font-medium text-terakota-700">
          {errorMessage}
        </p>
      )}
    </fieldset>
  );
}
