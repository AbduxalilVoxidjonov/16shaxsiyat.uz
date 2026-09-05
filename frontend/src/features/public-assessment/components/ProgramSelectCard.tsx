import { Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import type { PublicProgramSummary } from '@/shared/api/types';

export interface ProgramSelectCardProps extends PublicProgramSummary {
  selected: boolean;
  onSelect: () => void;
}

/**
 * Landing (E-1) dastur tanlov kartasi — `docs/06` 8-bo'lim, `prompts/36`. Maktabda bir nechta
 * dastur bo'lgandagina ko'rsatiladi (`LandingPage`). `role="radio"` + o'rab turuvchi
 * `role="radiogroup"` — bitta tugma orqali tanlov, klaviatura bilan to'liq ishlaydi (Tab +
 * Enter/Space, boshqa qo'shimcha o'q-tugma logikasi shart emas — sensor nishon ≥ 44px).
 */
export function ProgramSelectCard({
  code,
  nameUz,
  descriptionUz,
  testCount,
  questionCount,
  estimatedMinutes,
  selected,
  onSelect,
}: ProgramSelectCardProps) {
  const { t } = useTranslation();

  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      onClick={onSelect}
      className={cn(
        'card flex min-h-11 items-start gap-4 rounded-3xl p-5 text-left transition-all duration-200',
        'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-500',
        selected
          ? 'border-firuza-500 bg-firuza-50 shadow-lift'
          : 'hover:-translate-y-0.5 hover:border-firuza-300 hover:shadow-lift motion-reduce:hover:translate-y-0',
      )}
    >
      {/* Tanlov belgisi — rangdan tashqari SHAKL bilan ham farqlanadi (a11y, docs/11 4-bo'lim). */}
      <span
        aria-hidden="true"
        className={cn(
          'mt-0.5 grid size-5 shrink-0 place-items-center rounded-full border-2 transition-colors',
          selected ? 'border-firuza-500 bg-firuza-500 text-white' : 'border-line-strong',
        )}
      >
        {selected && <Check className="size-3" strokeWidth={3} aria-hidden="true" />}
      </span>
      <span className="flex min-w-0 flex-col gap-1">
        <h3 className="font-display text-[15px] font-bold text-ink">{nameUz}</h3>
        {descriptionUz && <p className="text-sm leading-relaxed text-ink-soft">{descriptionUz}</p>}
        <p className="text-xs font-semibold text-ink-soft">
          {t('publicAssessment.programSelect.meta', {
            tests: testCount,
            questions: questionCount,
            minutes: estimatedMinutes,
          })}
        </p>
      </span>
      <span className="sr-only">{code}</span>
    </button>
  );
}
