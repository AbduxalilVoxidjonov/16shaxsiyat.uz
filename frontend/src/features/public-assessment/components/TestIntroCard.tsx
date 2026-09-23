import { useTranslation } from 'react-i18next';
import { GirihStar } from '@/shared/ui/brand';
import type { PublicTestCatalogItem } from '@/shared/api/types';

export type TestIntroCardProps = PublicTestCatalogItem;

/**
 * Landing (E-1) test kartasi: nomi, nima o'lchaydi, savol soni, vaqti (`docs/11` E-1).
 *
 * "Nima o'lchaydi" izohi manbalari (ustunlik tartibida):
 * 1. `description` — katalogdagi anketa tavsifi (`TestDefinition.DescriptionUz`, `docs/07` 1.1);
 *    superadmin anketa sozlamalarida to'ldiradi.
 * 2. Test kodi bo'yicha i18n matni (`pages.landing.testDescriptions.<code>`) — faqat zaxira,
 *    tizim metodikalari uchun.
 * 3. Ikkalasi ham bo'lmasa (masalan superadmin yaratgan `Custom` anketa tavsifsiz) — izoh qatori
 *    UMUMAN chiqmaydi.
 *
 * Kalit mavjudligi `i18n.exists` bilan tekshiriladi: `defaultValue: ''` ishlamaydi, chunki
 * `returnEmptyString: false` (`shared/lib/i18n.ts`) bo'sh qiymatni "yo'q" deb hisoblaydi va
 * i18next xom kalitni (`pages.landing.testDescriptions.INTELLECT-SURVEY`) qaytaradi.
 */
export function TestIntroCard({
  code,
  name,
  description: catalogDescription,
  questionCount,
  estimatedMinutes,
}: TestIntroCardProps) {
  const { t, i18n } = useTranslation();
  const descriptionKey = `pages.landing.testDescriptions.${code}`;
  const description =
    catalogDescription?.trim() || (i18n.exists(descriptionKey) ? t(descriptionKey) : null);

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
