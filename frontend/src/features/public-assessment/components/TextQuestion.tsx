import { useId, useState, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';
import { RequiredMark } from '@/shared/ui/RequiredMark';

export interface TextQuestionProps {
  /** `type` — `ShortText` yoki `Phone` (`docs/18` §6.2). */
  question: BranchingQuestion;
  /** Joriy matn javobi (mahalliy kesh yoki server `currentText`dan) — hali javob yo'q bo'lsa `null`. */
  value: string | null;
  /** "Keyingi" bosilganda to'ldirilmagan deb belgilangan bo'lsa qizil ramka + xabar. */
  invalid: boolean;
  onAnswer: (payload: { text: string }) => void;
  /** `Enter` bosilganda keyingi savolga o'tish uchun (`LikertQuestion` bilan bir xil naqsh). */
  onAdvance: () => void;
  questionRef?: (node: HTMLDivElement | null) => void;
}

/**
 * `docs/18` §2.1 — `Phone` standart shabloni: O'zbekiston raqami. Superadmin `inputPattern`ni
 * o'zgartira oladi, shu sabab bu faqat FALLBACK (savolda `inputPattern` bo'lmasa ishlatiladi).
 */
const DEFAULT_PHONE_PATTERN = '^\\+?998[0-9]{9}$';
const DEFAULT_PHONE_PLACEHOLDER = '+998 90 123 45 67';
const DEFAULT_SHORT_TEXT_MAX_LENGTH = 200;

/**
 * Bitta qatorli matn savoli — `ShortText`/`Phone` (`docs/18` §6.2). `Phone` uchun
 * `inputMode="tel"`, standart shablon va jonli (typing paytida) tekshiruv bilan o'zbekcha xato
 * matni ko'rsatiladi — server ham xuddi shu shablonni qo'llaydi (`docs/18` §4.2), shu sabab
 * mijoz tomon tekshiruvi foydalanuvchiga ERTAROQ signal beradi, lekin yagona haqiqat manbai
 * emas (server yana bir bor tekshiradi).
 *
 * `LikertQuestion` bilan bir xil vizual "karta" uslubi (raqam medalyoni, `card` foni,
 * javob berilganda firuza ramka) — faqat guruh o'rniga bitta `<label>`/`<input>` bog'lanishi
 * ishlatiladi (bitta boshqaruv elementi uchun `fieldset` ortiqcha, `jsx-a11y` ham buni yorliq
 * bilan yechishni tavsiya qiladi).
 */
export function TextQuestion({
  question,
  value,
  invalid,
  onAnswer,
  onAdvance,
  questionRef,
}: TextQuestionProps) {
  const { t } = useTranslation();
  const legendId = useId();
  const inputId = useId();
  const errorId = useId();
  const [patternMismatch, setPatternMismatch] = useState(false);

  const isPhone = question.type === 'Phone';
  const pattern = question.inputPattern ?? (isPhone ? DEFAULT_PHONE_PATTERN : null);
  const placeholder = question.placeholder ?? (isPhone ? DEFAULT_PHONE_PLACEHOLDER : undefined);
  const maxLength = question.maxLength ?? DEFAULT_SHORT_TEXT_MAX_LENGTH;
  const draft = value ?? '';
  const answered = draft.trim().length > 0;

  function handleChange(next: string) {
    if (pattern) {
      try {
        const regex = new RegExp(pattern);
        setPatternMismatch(next.trim().length > 0 && !regex.test(next));
      } catch {
        // Noto'g'ri regex — admin tomonda `INPUT_PATTERN_INVALID` bilan rad etiladi (docs/18
        // §4.2), ommaviy oqimda esa xavfsiz tomonga o'tib shablon e'tiborsiz qoldiriladi.
        setPatternMismatch(false);
      }
    }
    onAnswer({ text: next });
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault();
      onAdvance();
    }
  }

  const showError = invalid || patternMismatch;
  const errorMessage = patternMismatch
    ? isPhone
      ? t('test.question.phoneInvalid')
      : t('test.question.formatInvalid')
    : t('test.question.requiredError');

  return (
    <div
      className={cn(
        'card px-5 py-7 transition-colors duration-300 sm:px-8',
        answered && 'border-firuza-200 bg-firuza-50/40',
        showError && 'border-terakota-600 bg-terakota-50/50',
      )}
    >
      <label
        htmlFor={inputId}
        id={legendId}
        className="float-left mb-7 block w-full text-center font-display text-lg leading-snug font-extrabold text-balance text-ink sm:text-xl"
      >
        <span className="mx-auto mb-3 grid size-8 place-items-center rounded-full bg-firuza-50 font-sans text-[13px] font-bold text-firuza-700">
          {question.order}
        </span>
        {question.text}
        {question.isRequired && <RequiredMark />}
      </label>

      <div
        ref={questionRef}
        data-question-id={question.id}
        tabIndex={-1}
        className="clear-both focus:outline-none"
      >
        <input
          id={inputId}
          type={isPhone ? 'tel' : 'text'}
          inputMode={isPhone ? 'tel' : undefined}
          value={draft}
          maxLength={maxLength}
          placeholder={placeholder}
          autoComplete={isPhone ? 'tel' : 'off'}
          aria-required={question.isRequired || undefined}
          aria-invalid={showError || undefined}
          aria-describedby={showError ? errorId : undefined}
          onChange={(event) => {
            handleChange(event.target.value);
          }}
          onKeyDown={handleKeyDown}
          className={cn(
            'h-11 w-full rounded-2xl border border-ink-faint bg-paper-card px-4 text-base text-ink',
            'placeholder:text-ink-muted',
            'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600',
            showError && 'border-terakota-600',
          )}
        />
      </div>

      {showError && (
        <p id={errorId} role="alert" className="mt-4 text-center text-sm font-medium text-terakota-700">
          {errorMessage}
        </p>
      )}
    </div>
  );
}
