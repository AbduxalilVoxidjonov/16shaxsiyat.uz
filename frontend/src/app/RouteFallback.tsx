import { useTranslation } from 'react-i18next';
import { GirihStar } from '@/shared/ui/brand';

/**
 * `lazy()` bilan yuklanayotgan route'lar uchun `<Suspense>` fallback'i.
 *
 * Fon ATAYLAB berilmagan: bu skelet ham ommaviy (`bg-paper`), ham admin (`bg-neutral-50`)
 * qatlami ichida ko'rinadi — faqat neytral `line` rangidagi joy egallovchilar chiziladi.
 */
export function RouteFallback() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center gap-6" role="status">
      <GirihStar className="size-10 animate-spin-slow text-line-strong" strokeWidth={2.5} />

      <div className="flex w-full max-w-sm flex-col items-center gap-3" aria-hidden="true">
        <span className="h-3 w-2/3 animate-pulse rounded-full bg-line" />
        <span className="h-3 w-1/2 animate-pulse rounded-full bg-line" />
      </div>

      <span className="sr-only">{t('common.loading')}</span>
    </div>
  );
}
