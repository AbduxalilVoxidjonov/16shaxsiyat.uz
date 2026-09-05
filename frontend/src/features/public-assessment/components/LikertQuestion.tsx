import { useId, type CSSProperties, type KeyboardEvent } from 'react';
import { Check } from 'lucide-react';
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

/** Bitta tanlov varianti — shkala darajasi (`scaleLabels`) yoki `options[]` elementi. */
interface Choice {
  key: string;
  value: number;
  label: string;
}

/** Chetdagi (eng kuchli fikr) doira o'lchami, px — `sm` va undan katta ekranda. */
const DOT_MAX = 60;
/** Markazdagi (betaraf) doira o'lchami, px. */
const DOT_MIN = 34;

/**
 * Doira o'lchami chetdan markazga kichrayadi (60 → 34 → 60 px): kuchliroq fikr kattaroq
 * nishon bilan ifodalanadi. Formula daraja soniga MOSLASHADI — 7 ta uchun
 * 60/51/43/34/43/51/60, 5 ta uchun 60/47/34/47/60, 2 ta uchun ikkalasi ham 60.
 */
function dotSize(index: number, count: number): number {
  if (count < 2) return DOT_MAX;
  const center = (count - 1) / 2;
  const distance = Math.abs(index - center) / center;
  return Math.round(DOT_MIN + (DOT_MAX - DOT_MIN) * distance);
}

type DotTone = 'agree' | 'neutral' | 'disagree';

/**
 * Rang qutbi: `scaleLabels` har doim "umuman qo'shilmayman → to'liq qo'shilaman" tartibida
 * keladi (`03-public-api.md` 6-bo'lim), shu sabab o'ng tomon — rozilik (firuza), chap tomon —
 * rad (binafsha), aynan o'rta (faqat TOQ sonli shkalada bo'ladi) — neytral.
 */
function toneOf(index: number, count: number): DotTone {
  const center = (count - 1) / 2;
  if (index === center) return 'neutral';
  return index > center ? 'agree' : 'disagree';
}

const DOT_IDLE: Record<DotTone, string> = {
  agree: 'border-firuza-300 text-firuza-600 group-hover:border-firuza-500 group-hover:bg-firuza-50',
  neutral:
    'border-line-strong text-ink-faint group-hover:border-ink-faint group-hover:bg-paper-deep',
  disagree:
    'border-binafsha-300 text-binafsha-600 group-hover:border-binafsha-500 group-hover:bg-binafsha-50',
};

const DOT_ACTIVE: Record<DotTone, string> = {
  agree: 'border-firuza-500 bg-firuza-500 text-white shadow-glow',
  neutral: 'border-ink-faint bg-ink-faint text-white shadow-soft',
  disagree: 'border-binafsha-500 bg-binafsha-500 text-white shadow-lift',
};

/**
 * Fokus halqasi yorliqda (`<label>`) ko'rsatiladi, chunki radio inputning o'zi `sr-only` —
 * aks holda klaviatura bilan yurganda fokus ko'rinmay qolardi (`docs/11`, 4-bo'lim).
 */
const FOCUS_RING =
  'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-firuza-500';

/**
 * Bitta test savoli — `fieldset`/`legend` + `radiogroup` (a11y, docs/11 4-bo'lim).
 * Klaviatura: `1..9` raqamlari mos variantni tanlaydi, `Enter` keyingi savolga o'tadi
 * (CLAUDE.md MAXSUS DIQQAT 3-band); variantlar HAQIQIY `<input type="radio">` bo'lgani uchun
 * guruh ichida o'q tugmalari brauzerning o'z (roving) mantiqi bilan ishlaydi. Klaviatura
 * tinglovchisi va fokus/ko'rinish `ref`i `radiogroup` ustida (`fieldset`da EMAS) — `fieldset`
 * interaktiv ARIA rolga ega emas (`jsx-a11y/no-noninteractive-element-interactions`),
 * `radiogroup` esa mos widget rol.
 *
 * **Ko'rinish daraja soniga MOSLASHADI** (`03-public-api.md` 6-bo'lim: `Likert5` → 1..5,
 * `Likert7` → 1..7, `Binary` → 0/1, `SingleChoice`/`ForcedChoice` → `options[]`,
 * `scaleLabels` esa `null`):
 * - `options[]` bo'lsa — variant KARTALARI (variant matni uzun bo'lishi mumkin, doiraga
 *   sig'maydi);
 * - 4 va undan ko'p daraja — doiralar qatori, chetdan markazga kichrayadi, ostida ikki qutb
 *   yorlig'i (birinchi va oxirgi daraja matni);
 * - 3 va undan kam daraja (masalan `Binary`) — markazlashgan yirik doiralar va har birining
 *   ostida O'Z yorlig'i (ikki qutb sarlavhasi bunday holatda ortiqcha bo'lardi).
 *
 * Teginish maydoni: yorliq `min-h-11` (44px) va qo'shni yorliqlar orasida bo'shliq yo'q —
 * qator bo'ylab "o'lik zona" qolmaydi. Doiraning O'ZI telefonda kichikroq (0.68×), lekin
 * bosiladigan maydon to'liq balandlikda saqlanadi (docs/11 4-bo'lim).
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

  // `options[]` — `SingleChoice`/`ForcedChoice`; aks holda shkala darajalari.
  const optionChoices: Choice[] = [...(question.options ?? [])]
    .sort((a, b) => a.order - b.order)
    .map((option) => ({ key: option.id, value: option.value, label: option.text }));
  const scaleChoices: Choice[] = scaleLabels.map((label) => ({
    key: String(label.value),
    value: label.value,
    label: label.label,
  }));
  const asCards = optionChoices.length > 0;
  const choices = asCards ? optionChoices : scaleChoices;
  const withInlineLabels = choices.length <= 3;

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (/^[1-9]$/.test(event.key)) {
      const option = choices[Number(event.key) - 1];
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
  const firstLabel = choices[0]?.label;
  const lastLabel = choices[choices.length - 1]?.label;

  return (
    <fieldset
      className={cn(
        'card px-5 py-7 transition-colors duration-300 sm:px-8',
        answered && 'border-firuza-200 bg-firuza-50/40',
        invalid && 'border-terakota-600 bg-terakota-50/50',
      )}
    >
      {/*
        Savol matni `legend`ning BEVOSITA matn tuguni bo'lib qolishi shart (tartib raqami
        alohida `span` ichida): shunda skrinrider "11, savol matni" deb bir butun o'qiydi va
        testlar ham `getByText(savolMatni)` bilan savolni topa oladi.

        `float-left w-full` — TARTIB uchun, bezak uchun emas. Brauzer `legend`ni odatda
        `fieldset` ning YUQORI CHEGARASI ustiga "o'tqazadi" (chegara chizig'i legend
        balandligining o'rtasidan o'tadi) va `fieldset` ning yuqori paddingi shu bilan
        yeb ketiladi. P45 reskinidan keyin bu ko'rinib qoldi: raqam medalyoni kartadan
        TASHQARIGA, ikki karta orasidagi bo'shliqqa chiqib ketardi, savol matni esa
        kartaning yuqori chetiga yopishib qolardi (`py-7` yo'qolardi). Suzuvchi (float)
        `legend` bu maxsus joylashtirishdan chiqadi va oddiy blok kabi padding ichida
        qoladi; keyingi element `clear-both` bilan uning ostiga tushadi.
      */}
      <legend
        id={legendId}
        className="float-left mb-7 w-full text-center font-display text-lg leading-snug font-extrabold text-balance text-ink sm:text-xl"
      >
        <span className="mx-auto mb-3 grid size-8 place-items-center rounded-full bg-firuza-50 font-sans text-[13px] font-bold text-firuza-700">
          {question.order}
        </span>
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
        className="clear-both focus:outline-none"
      >
        {asCards ? (
          <div className="flex flex-col gap-2.5">
            {choices.map((choice) => {
              const checked = value === choice.value;
              return (
                <label
                  key={choice.key}
                  className={cn(
                    'flex min-h-11 cursor-pointer items-center gap-3 rounded-2xl border px-4 py-3 text-left text-sm transition-colors',
                    FOCUS_RING,
                    checked
                      ? 'border-firuza-500 bg-firuza-50 font-semibold text-firuza-900'
                      : 'border-line bg-paper-card text-ink-soft hover:border-firuza-300 hover:bg-firuza-50/40',
                  )}
                >
                  <input
                    type="radio"
                    name={`question-${question.id}`}
                    value={choice.value}
                    checked={checked}
                    onChange={() => {
                      onAnswer(choice.value);
                    }}
                    className="sr-only"
                  />
                  <span
                    aria-hidden="true"
                    className={cn(
                      'grid size-5 shrink-0 place-items-center rounded-full border-2 transition-colors',
                      checked ? 'border-firuza-500 bg-firuza-500 text-white' : 'border-line-strong',
                    )}
                  >
                    {checked && <Check className="size-3" strokeWidth={3} aria-hidden="true" />}
                  </span>
                  {choice.label}
                </label>
              );
            })}
          </div>
        ) : (
          <>
            <div
              className={cn(
                'flex items-start',
                withInlineLabels ? 'justify-center gap-6 sm:gap-12' : 'justify-between',
              )}
            >
              {choices.map((choice, index) => {
                const checked = value === choice.value;
                const tone = toneOf(index, choices.length);
                const size = dotSize(index, choices.length);
                return (
                  <label
                    key={choice.key}
                    title={choice.label}
                    className={cn(
                      'group flex min-h-11 cursor-pointer flex-col items-center justify-center gap-2 rounded-2xl py-1',
                      withInlineLabels ? 'w-28 max-w-[40%]' : 'flex-1',
                      FOCUS_RING,
                    )}
                  >
                    <input
                      type="radio"
                      name={`question-${question.id}`}
                      value={choice.value}
                      checked={checked}
                      onChange={() => {
                        onAnswer(choice.value);
                      }}
                      // Doira — sof vizual belgi, shu sabab nom `aria-label`dan olinadi.
                      // Yorliq matni ko'rinadigan holatda (`withInlineLabels`) esa nom
                      // yorliqning o'zidan keladi — ikki marta o'qilmasligi uchun berilmaydi.
                      aria-label={withInlineLabels ? undefined : choice.label}
                      className="sr-only"
                    />
                    <span
                      aria-hidden="true"
                      style={{ ['--dot' as string]: `${String(size)}px` } as CSSProperties}
                      className={cn(
                        'grid size-[calc(var(--dot)*0.68)] place-items-center rounded-full border-2 sm:size-[var(--dot)]',
                        'transition-all duration-200 group-active:scale-95 motion-reduce:transition-none',
                        checked ? DOT_ACTIVE[tone] : DOT_IDLE[tone],
                      )}
                    >
                      {checked && <Check className="size-1/2" strokeWidth={3} aria-hidden="true" />}
                    </span>
                    {withInlineLabels && (
                      // `aria-hidden` YO'Q — bu matn radio tugmaning ochiq nomi (accessible
                      // name) manbai, shu sabab yuqorida `aria-label` berilmaydi.
                      <span
                        className={cn(
                          'text-center text-[13px] font-bold',
                          checked ? 'text-ink' : 'text-ink-soft',
                        )}
                      >
                        {choice.label}
                      </span>
                    )}
                  </label>
                );
              })}
            </div>

            {/* Ikki qutb yorlig'i — doiralar qatorining ma'nosini bir qarashda tushuntiradi. */}
            {!withInlineLabels && firstLabel && lastLabel && (
              <div
                aria-hidden="true"
                className="mt-3 flex items-start justify-between gap-4 text-[11px] font-bold sm:text-xs"
              >
                <span className="max-w-[45%] text-left text-binafsha-600">{firstLabel}</span>
                <span className="max-w-[45%] text-right text-firuza-700">{lastLabel}</span>
              </div>
            )}
          </>
        )}
      </div>

      {invalid && (
        <p
          id={errorId}
          role="alert"
          className="mt-4 text-center text-sm font-medium text-terakota-700"
        >
          {t('test.question.requiredError')}
        </p>
      )}
    </fieldset>
  );
}
