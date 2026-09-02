import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Eye } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatDate } from '@/shared/lib/formatDate';
import { useAiPromptsQuery } from '../api/useAiPromptsQuery';
import type { AiPromptTemplateDto } from '../model/types';
import { PromptViewDialog } from './PromptViewDialog';

/** "Promptlar" bo'limi — `docs/11` A-7: versiyalar ro'yxati, faol versiya, ko'rish. */
export function PromptVersionsSection() {
  const { t } = useTranslation();
  const promptsQuery = useAiPromptsQuery();
  const [viewing, setViewing] = useState<AiPromptTemplateDto | null>(null);

  return (
    <Card title={t('aiSettings.prompts.heading')}>
      {promptsQuery.isPending && (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      )}

      {promptsQuery.isError && <ErrorState onRetry={() => void promptsQuery.refetch()} />}

      {promptsQuery.data && promptsQuery.data.length === 0 && (
        <EmptyState
          title={t('aiSettings.prompts.emptyTitle')}
          description={t('aiSettings.prompts.emptyDescription')}
        />
      )}

      {promptsQuery.data && promptsQuery.data.length > 0 && (
        <ul className="flex flex-col gap-2">
          {promptsQuery.data.map((prompt) => (
            <li
              key={`${prompt.key}-${String(prompt.version)}`}
              className="flex items-center justify-between gap-3 rounded-lg border border-neutral-200 p-3"
            >
              <div>
                <p className="text-sm font-medium text-neutral-900">
                  {prompt.key} · v{prompt.version}
                  {prompt.isActive && (
                    <Badge variant="success" className="ml-2">
                      {t('aiSettings.prompts.activeBadge')}
                    </Badge>
                  )}
                </p>
                <p className="text-xs text-neutral-500">
                  {t('aiSettings.prompts.createdAt', { date: formatDate(prompt.createdAt) })}
                </p>
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setViewing(prompt)}
                aria-label={t('aiSettings.prompts.viewCta')}
              >
                <Eye size={14} aria-hidden="true" />
                {t('aiSettings.prompts.viewCta')}
              </Button>
            </li>
          ))}
        </ul>
      )}

      <PromptViewDialog prompt={viewing} onClose={() => setViewing(null)} />
    </Card>
  );
}
