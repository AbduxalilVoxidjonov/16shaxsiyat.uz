import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

export interface RequiredMarkProps {
  className?: string;
}

/**
 * Majburiy maydon/savol yorlig'i oxiridagi qizil yulduzcha (egasining talabi, 2026-09-23).
 *
 * Rang — xato/diqqat tokeni `terakota-600` (admin "Sozlamalar" oldindan ko'rishidagi
 * `RegistrationFormPreview` bilan bir xil naqsh). Belgining o'zi i18n'dan
 * (`common.requiredMark`) keladi.
 *
 * Accessible name O'ZGARMAYDI — ikki qatlam:
 * - `aria-hidden` — screen reader va `getByRole(..., { name })`/Playwright `getByLabel`
 *   yulduzchani nomga qo'shmaydi; majburiylik maydondagi `aria-required` orqali yetkaziladi.
 * - Belgi DOM matni EMAS, CSS `::after` (`content: attr(data-mark)`) — `getByLabelText`
 *   `label.textContent`ni `aria-hidden`ga qaramay solishtiradi; matn tugun bo'lsa
 *   "F.I.Sh.*" bo'lib, mavjud aniq-matnli qidiruvlar buzilardi.
 */
export function RequiredMark({ className }: RequiredMarkProps) {
  const { t } = useTranslation();
  return (
    <span
      aria-hidden="true"
      data-required-mark=""
      data-mark={t('common.requiredMark')}
      className={cn('ml-0.5 text-terakota-600 after:content-[attr(data-mark)]', className)}
    />
  );
}
