import { Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

export interface SpinnerProps {
  className?: string;
  size?: number;
}

/** Yuklanish indikatori — ekran o'quvchisi uchun matn bilan e'lon qilinadi. */
export function Spinner({ className, size = 20 }: SpinnerProps) {
  const { t } = useTranslation();
  return (
    <span role="status" className="inline-flex items-center gap-2">
      <Loader2
        className={cn('animate-spin text-neutral-500', className)}
        size={size}
        aria-hidden="true"
      />
      <span className="sr-only">{t('common.loading')}</span>
    </span>
  );
}
