import { AlertTriangle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { Button } from './Button';

export interface ErrorStateProps {
  className?: string;
  title?: string;
  description?: string;
  /** Berilsa "Qayta urinish" tugmasi ko'rsatiladi (docs/10, 7-bo'lim). */
  onRetry?: () => void;
}

/** Sahifa/bo'lim darajasidagi xato holati — har doim qayta urinish imkoniyati bilan. */
export function ErrorState({ className, title, description, onRetry }: ErrorStateProps) {
  const { t } = useTranslation();
  return (
    <div
      role="alert"
      className={cn(
        'flex flex-col items-center gap-2 rounded-xl border border-danger-200 bg-danger-50 px-6 py-12 text-center',
        className,
      )}
    >
      <AlertTriangle className="text-danger-500" size={32} aria-hidden="true" />
      <p className="text-base font-medium text-neutral-900">{title ?? t('error.title')}</p>
      <p className="max-w-sm text-sm text-neutral-600">{description ?? t('error.generic')}</p>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry} className="mt-2">
          {t('common.retry')}
        </Button>
      )}
    </div>
  );
}
