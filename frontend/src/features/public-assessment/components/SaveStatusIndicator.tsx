import { Check, Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { AutosaveStatus } from '../hooks/useAutosave';

/** "Saqlandi ✓" / "Saqlanmoqda…" — autosave holati indikatori (docs/11 E-3). */
export function SaveStatusIndicator({ status }: { status: AutosaveStatus }) {
  const { t } = useTranslation();

  return (
    <p role="status" className="flex items-center gap-1.5 text-xs text-neutral-500">
      {status === 'saving' ? (
        <>
          <Loader2 size={14} className="animate-spin motion-reduce:animate-none" aria-hidden="true" />
          {t('test.saveStatus.saving')}
        </>
      ) : (
        <>
          <Check size={14} className="text-success-600" aria-hidden="true" />
          {t('test.saveStatus.saved')}
        </>
      )}
    </p>
  );
}
