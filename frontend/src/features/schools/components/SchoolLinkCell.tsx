import { Copy, QrCode } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useToast } from '@/shared/ui/useToast';

export interface SchoolLinkCellProps {
  publicUrl: string;
  onShowQr: () => void;
}

/** URL'dagi protokolni olib tashlaydi (`https://`) — jadvalda qisqaroq ko'rinish uchun. */
function displayUrl(url: string): string {
  return url.replace(/^https?:\/\//, '');
}

/**
 * Jadvaldagi "Havola" ustuni — CLAUDE.md "MAXSUS DIQQAT" 3: to'liq havola ko'rinmaydi
 * (uzun), qisqartirilgan matn + nusxalash tugmasi (toast) + QR ikonkasi.
 */
export function SchoolLinkCell({ publicUrl, onShowQr }: SchoolLinkCellProps) {
  const { t } = useTranslation();
  const toast = useToast();

  async function handleCopy() {
    try {
      if (!navigator.clipboard) {
        throw new Error('clipboard API mavjud emas');
      }
      await navigator.clipboard.writeText(publicUrl);
      toast.show({ variant: 'success', title: t('schools.linkCopied') });
    } catch {
      // Eski brauzer yoki HTTPS bo'lmagan kontekst — `navigator.clipboard` yo'q/rad etadi.
      // Jimgina yiqilmaydi: tushunarli xato + havolani qo'lda nusxalash uchun matn.
      toast.show({
        variant: 'danger',
        title: t('schools.linkCopyErrorTitle'),
        description: t('schools.linkCopyErrorDescription', { url: publicUrl }),
        duration: 0,
      });
    }
  }

  return (
    <div className="flex max-w-56 items-center gap-1.5">
      <span className="truncate text-sm text-neutral-700" title={publicUrl}>
        {displayUrl(publicUrl)}
      </span>
      <button
        type="button"
        onClick={() => void handleCopy()}
        aria-label={t('schools.actions.copyLink')}
        className="shrink-0 rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900"
      >
        <Copy size={16} aria-hidden="true" />
      </button>
      <button
        type="button"
        onClick={onShowQr}
        aria-label={t('schools.actions.showQr')}
        className="shrink-0 rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900"
      >
        <QrCode size={16} aria-hidden="true" />
      </button>
    </div>
  );
}
