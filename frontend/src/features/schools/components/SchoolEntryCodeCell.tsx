import { Copy, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useToast } from '@/shared/ui/useToast';

export interface SchoolEntryCodeCellProps {
  /** Formatlangan kod (`XXXX-XXXX`, backend beradi) yoki `null` (ommaviy makon). */
  entryCode: string | null | undefined;
  onRegenerate: () => void;
}

/**
 * Maktab kodi (`School.EntryCode`) — `/kirish` → "Maktab uchun" yo'lining siri. Havola/QR
 * bilan bir xil darajada tarqatiladi, shu sabab `SchoolLinkCell` naqshi: kod + nusxalash
 * (toast) + qayta yaratish (tasdiq dialogi chaqiruvchida). `accessCode` (sinf kodi) EMAS.
 */
export function SchoolEntryCodeCell({ entryCode, onRegenerate }: SchoolEntryCodeCellProps) {
  const { t } = useTranslation();
  const toast = useToast();

  if (!entryCode) {
    return <span className="text-sm text-neutral-700">—</span>;
  }

  async function handleCopy() {
    try {
      if (!navigator.clipboard) {
        throw new Error('clipboard API mavjud emas');
      }
      await navigator.clipboard.writeText(entryCode ?? '');
      toast.show({ variant: 'success', title: t('schools.entryCodeCopied') });
    } catch {
      // Eski brauzer yoki HTTPS bo'lmagan kontekst — `SchoolLinkCell` bilan bir xil yechim:
      // tushunarli xato + kodni qo'lda nusxalash uchun matn.
      toast.show({
        variant: 'danger',
        title: t('schools.linkCopyErrorTitle'),
        description: t('schools.linkCopyErrorDescription', { url: entryCode }),
        duration: 0,
      });
    }
  }

  return (
    <div className="flex items-center gap-1.5">
      <span className="font-mono text-sm font-semibold tracking-widest text-neutral-900">{entryCode}</span>
      <button
        type="button"
        onClick={() => void handleCopy()}
        aria-label={t('schools.actions.copyEntryCode')}
        className="shrink-0 rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900"
      >
        <Copy size={16} aria-hidden="true" />
      </button>
      <button
        type="button"
        onClick={onRegenerate}
        className="inline-flex shrink-0 items-center gap-1 rounded-md px-1.5 py-1 text-xs font-medium text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
      >
        <RefreshCw size={14} aria-hidden="true" />
        {t('schools.actions.regenerateEntryCode')}
      </button>
    </div>
  );
}
