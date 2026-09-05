import { WifiOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';

/**
 * Offline banner — docs/10 4.2-bo'lim, docs/11 E-3.
 * Rangi `zarhal` (sariq-oltin): ogohlantirish, lekin xato emas — javoblar yo'qolmaydi.
 */
export function OfflineBanner() {
  const { t } = useTranslation();
  return (
    <div
      role="status"
      className="flex items-start gap-2.5 rounded-2xl border border-zarhal-200 bg-zarhal-50 px-4 py-3 text-sm text-zarhal-800"
    >
      <WifiOff size={16} className="mt-0.5 shrink-0" aria-hidden="true" />
      {t('test.offlineBanner')}
    </div>
  );
}
