import { AlertTriangle } from 'lucide-react';
import { useTranslation } from 'react-i18next';

/**
 * `prompts/28` "Cheklovlar": "Hech bir provider faol bo'lmasa — sahifa yuqorisida
 * ogohlantirish banner: 'AI tahlil ishlamaydi — kamida bitta provayder sozlanishi kerak.'"
 */
export function NoActiveProviderBanner() {
  const { t } = useTranslation();
  return (
    <div
      role="alert"
      className="flex items-center gap-2 rounded-lg border border-warning-200 bg-warning-50 px-4 py-3 text-sm font-medium text-warning-800"
    >
      <AlertTriangle size={18} aria-hidden="true" />
      {t('aiSettings.noActiveProviderBanner')}
    </div>
  );
}
