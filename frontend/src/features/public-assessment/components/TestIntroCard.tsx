import { useTranslation } from 'react-i18next';
import { GirihStar } from '@/shared/ui/brand';
import type { PublicTestCatalogItem } from '@/shared/api/types';

export type TestIntroCardProps = PublicTestCatalogItem;

/**
 * Landing (E-1) test kartasi: nomi, nima o'lchaydi, savol soni, vaqti (`docs/11` E-1).
 * "Nima o'lchaydi" ta'rifi API'da yo'q (`docs/07` 1.1 javob shaklida faqat kod/nom/son bor) —
 * shu sabab test kodi bo'yicha i18n matni bilan to'ldiriladi; noma'lum kod (masalan superadmin
 * yaratgan `Custom` anketalar) uchun izoh qatori chiqmaydi (`defaultValue: ''`).
 */
export function TestIntroCard({ code, name, questionCount, estimatedMinutes }: TestIntroCardProps) {
  const { t } = useTranslation();
  const description = t(`pages.landing.testDescriptions.${code}`, { defaultValue: '' });

  return (
    <div className="card card-hover flex items-start gap-4 rounded-3xl p-5">
      {/* Girih nishoni — har bir blokni vizual ravishda "kartochka" qiladi (sof dekor). */}
      <span className="relative grid size-11 shrink-0 place-items-center rounded-[28%] bg-linear-to-br from-firuza-50 to-firuza-100">
        <GirihStar className="absolute inset-[20%] text-firuza-500/70" strokeWidth={2.5} />
      </span>
      <div className="flex min-w-0 flex-col gap-1">
        <h3 className="font-display text-[15px] font-bold text-ink">{name}</h3>
        {description && <p className="text-sm leading-relaxed text-ink-soft">{description}</p>}
        <p className="text-xs font-semibold text-ink-soft">
          {t('pages.landing.testMeta', { count: questionCount, minutes: estimatedMinutes })}
        </p>
      </div>
    </div>
  );
}
