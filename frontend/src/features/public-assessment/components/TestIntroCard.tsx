import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
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
    <Card className="flex flex-col gap-1">
      <h3 className="text-sm font-semibold text-neutral-900">{name}</h3>
      {description && <p className="text-sm text-neutral-600">{description}</p>}
      <p className="text-xs text-neutral-500">
        {t('pages.landing.testMeta', { count: questionCount, minutes: estimatedMinutes })}
      </p>
    </Card>
  );
}
