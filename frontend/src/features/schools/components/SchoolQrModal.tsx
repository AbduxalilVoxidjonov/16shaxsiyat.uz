import { useTranslation } from 'react-i18next';
import { Printer, Download } from 'lucide-react';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { cn } from '@/shared/lib/cn';

export interface SchoolQrModalData {
  schoolName: string;
  slug: string;
  publicUrl: string;
  qrCodeBase64: string;
}

export interface SchoolQrModalProps {
  open: boolean;
  onClose: () => void;
  data: SchoolQrModalData | null;
  isLoading?: boolean;
  isError?: boolean;
  onRetry?: () => void;
}

/**
 * QR modal — CLAUDE.md "MAXSUS DIQQAT" 2: katta QR (backenddan base64 PNG), maktab nomi,
 * havola matni, "PNG yuklab olish" (ma'noli fayl nomi bilan) va "Chop etish" (A5, `index.css`
 * dagi `.print-only-area` global qoidasi).
 */
export function SchoolQrModal({ open, onClose, data, isLoading = false, isError = false, onRetry }: SchoolQrModalProps) {
  const { t } = useTranslation();

  const fileName = data ? `salohiyat-${data.slug}-qr.png` : 'salohiyat-qr.png';
  const dataUrl = data ? `data:image/png;base64,${data.qrCodeBase64}` : undefined;

  return (
    <Dialog open={open} onClose={onClose} title={t('schools.qrModal.title')} className="max-w-sm">
      {isLoading && (
        <div className="flex flex-col items-center gap-3 py-4">
          <Skeleton className="size-56" />
          <Skeleton className="h-4 w-40" />
        </div>
      )}

      {!isLoading && isError && <ErrorState onRetry={onRetry} description={t('schools.qrModal.loadError')} />}

      {!isLoading && !isError && data && (
        <div className="flex flex-col items-center gap-4">
          <div className="print-only-area flex flex-col items-center gap-3">
            {dataUrl && (
              <img
                src={dataUrl}
                alt={t('schools.qrModal.title')}
                className="size-56 rounded-lg border border-neutral-200 p-2"
              />
            )}
            <p className="text-center text-base font-semibold text-neutral-900">{data.schoolName}</p>
            <p className="max-w-xs text-center text-sm break-all text-neutral-600">
              <span className="font-medium">{t('schools.qrModal.linkLabel')}:</span> {data.publicUrl}
            </p>
          </div>

          <div className="flex w-full gap-2">
            {/* `Button` har doim `<button>` render qiladi — `<a download>` ichiga solib
                bo'lmaydi (interaktiv ichida interaktiv, HTML noto'g'ri bo'lardi), shu sabab
                havola tashqi ko'rinishi qo'lda `Button`ning "outline" uslubiga moslab
                takrorlangan. */}
            <a
              href={dataUrl}
              download={fileName}
              className={cn(
                'inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-lg border border-neutral-300 px-4 text-sm font-medium text-neutral-900',
                'hover:bg-neutral-50',
                'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600',
              )}
            >
              <Download size={16} aria-hidden="true" />
              {t('schools.qrModal.downloadCta')}
            </a>
            <Button variant="secondary" className="flex-1" onClick={() => window.print()}>
              <Printer size={16} aria-hidden="true" />
              {t('schools.qrModal.printCta')}
            </Button>
          </div>
        </div>
      )}
    </Dialog>
  );
}
