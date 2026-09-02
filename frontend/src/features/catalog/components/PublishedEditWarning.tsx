import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';

/**
 * Nashr qilingan (`Published`) anketani tahrirlashda ko'rsatiladigan ogohlantirish.
 * Matn ataylab aniq: **ballar o'zgarmaydi** (scoring `scale`/`weight` ga tayanadi, ular
 * tahrirlanmaydi), lekin savol matni jonli sessiyalarda DARHOL ko'rinadi.
 */
export function PublishedEditWarning() {
  const { t } = useTranslation();

  return (
    <p
      role="status"
      className="flex items-start gap-2 rounded-lg bg-warning-50 p-3 text-sm text-warning-700"
    >
      <AlertTriangle size={16} className="mt-0.5 shrink-0" aria-hidden="true" />
      {t('catalog.detail.publishedEditWarning')}
    </p>
  );
}
