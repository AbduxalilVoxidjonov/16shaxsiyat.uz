import { Copy, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { useToast } from '@/shared/ui/useToast';

export interface SchoolEntryCodeCellProps {
  /** Formatlangan kod (`XXXX-XXXX`, backend beradi) yoki `null` (ommaviy makon). */
  entryCode: string | null | undefined;
  /**
   * "Kodni qayta yaratish" tugmasi — berilmasa tugma CHIQMAYDI (zich joylarda faqat kod +
   * nusxalash; qayta yaratish detal sahifasida qoladi).
   */
  onRegenerate?: () => void;
  /**
   * `lg` — detal sahifasidagi ko'zga tashlanadigan variant (egasi kodni topa olmagan edi):
   * katta shrift. `md` (standart) — jadval katakchasi / zich qator.
   */
  size?: 'md' | 'lg';
}

/**
 * Maktab kodi (`School.EntryCode`) — `/kirish` → "Maktab uchun" yo'lining siri. Havola/QR
 * bilan bir xil darajada tarqatiladi, shu sabab `SchoolLinkCell` naqshi: kod + nusxalash
 * (toast) + qayta yaratish (tasdiq dialogi chaqiruvchida). `accessCode` (sinf kodi) EMAS.
 */
export function SchoolEntryCodeCell({ entryCode, onRegenerate, size = 'md' }: SchoolEntryCodeCellProps) {
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

  const isLarge = size === 'lg';

  return (
    <div className={cn('flex flex-wrap items-center', isLarge ? 'gap-2' : 'gap-1.5')}>
      <span
        className={cn(
          'font-mono font-semibold tracking-widest text-neutral-900',
          isLarge ? 'text-2xl font-bold sm:text-3xl' : 'text-sm',
        )}
      >
        {entryCode}
      </span>
      <button
        type="button"
        onClick={() => void handleCopy()}
        aria-label={t('schools.actions.copyEntryCode')}
        className="shrink-0 rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900"
      >
        <Copy size={isLarge ? 20 : 16} aria-hidden="true" />
      </button>
      {onRegenerate && (
        <button
          type="button"
          onClick={onRegenerate}
          className="inline-flex shrink-0 items-center gap-1 rounded-md px-1.5 py-1 text-xs font-medium text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
        >
          <RefreshCw size={14} aria-hidden="true" />
          {t('schools.actions.regenerateEntryCode')}
        </button>
      )}
    </div>
  );
}
