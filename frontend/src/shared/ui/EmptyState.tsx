import type { ReactNode } from 'react';
import { Inbox } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

export interface EmptyStateProps {
  className?: string;
  title?: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
}

/** Ro'yxat/jadval bo'sh bo'lganda ko'rsatiladigan holat (docs/10, 5.3-bo'lim). */
export function EmptyState({ className, title, description, icon, action }: EmptyStateProps) {
  const { t } = useTranslation();
  return (
    <div
      className={cn(
        'flex flex-col items-center gap-2 rounded-xl border border-dashed border-neutral-300 px-6 py-12 text-center',
        className,
      )}
    >
      <span className="text-neutral-400" aria-hidden="true">
        {icon ?? <Inbox size={32} />}
      </span>
      <p className="text-base font-medium text-neutral-900">{title ?? t('empty.title')}</p>
      <p className="max-w-sm text-sm text-neutral-500">{description ?? t('empty.description')}</p>
      {action}
    </div>
  );
}
