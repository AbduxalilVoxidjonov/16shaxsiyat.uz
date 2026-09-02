import { WifiOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';

/** Offline banner — docs/10 4.2-bo'lim, docs/11 E-3. */
export function OfflineBanner() {
  const { t } = useTranslation();
  return (
    <div
      role="status"
      className="flex items-center gap-2 rounded-lg border border-warning-300 bg-warning-50 px-3 py-2 text-sm text-warning-700"
    >
      <WifiOff size={16} className="shrink-0" aria-hidden="true" />
      {t('test.offlineBanner')}
    </div>
  );
}
