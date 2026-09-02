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
        'flex min-h-11 flex-col gap-1 rounded-xl border p-4 text-left shadow-sm transition-colors',
        selected
          ? 'border-primary-600 bg-primary-50'
          : 'border-neutral-200 bg-white hover:border-primary-300',
      )}
    >
      <h3 className="text-sm font-semibold text-neutral-900">{nameUz}</h3>
      {descriptionUz && <p className="text-sm text-neutral-600">{descriptionUz}</p>}
      <p className="text-xs text-neutral-500">
        {t('publicAssessment.programSelect.meta', {
          tests: testCount,
          questions: questionCount,
          minutes: estimatedMinutes,
        })}
      </p>
      <span className="sr-only">{code}</span>
    </button>
  );
}
