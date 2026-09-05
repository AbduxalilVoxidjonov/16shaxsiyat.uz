import { Check, Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { AutosaveStatus } from '../hooks/useAutosave';

/**
 * "Saqlandi ✓" / "Saqlanmoqda…" — autosave holati indikatori (docs/11 E-3).
 * Kichik "chip" ko'rinishida: holat sezilarli, lekin savollardan diqqatni tortmaydi.
 */
export function SaveStatusIndicator({ status }: { status: AutosaveStatus }) {
  const { t } = useTranslation();

  return (
    <p
      role="status"
      className="chip border border-line bg-paper-card text-[11px] text-ink-soft shadow-soft"
    >
      {status === 'saving' ? (
        <>
          <Loader2
            size={13}
            className="animate-spin text-ink-faint motion-reduce:animate-none"
            aria-hidden="true"
          />
          {t('test.saveStatus.saving')}
        </>
      ) : (
        <>
          <Check size={13} className="text-zumrad-600" aria-hidden="true" />
          {t('test.saveStatus.saved')}
        </>
      )}
    </p>
  );
}
