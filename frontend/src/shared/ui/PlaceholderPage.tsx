import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';

export interface PlaceholderPageProps {
  /** i18n kalit — sahifa sarlavhasi. */
  titleKey: string;
  /** i18n kalit — sarlavha ostidagi izoh. Berilmasa umumiy "qurilmoqda" matni ko'rsatiladi. */
  descriptionKey?: string;
}

/**
 * P19 skelet bosqichi uchun joy egallovchi sahifa — har bir route shu komponentni render
 * qiladi, mantiq keyingi promptlarda (`P20+`) qo'shiladi.
 */
export function PlaceholderPage({ titleKey, descriptionKey }: PlaceholderPageProps) {
  const { t } = useTranslation();
  const title = t(titleKey);
  usePageTitle(title);

  return (
    <section className="flex min-h-[50vh] flex-col items-center justify-center gap-2 p-6 text-center">
      <h1 className="font-display text-xl font-extrabold text-ink">{title}</h1>
      <p className="text-sm text-ink-soft">{t(descriptionKey ?? 'pages.placeholderNotice')}</p>
    </section>
  );
}
