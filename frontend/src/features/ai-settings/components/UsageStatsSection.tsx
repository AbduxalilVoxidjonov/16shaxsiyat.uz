import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { useAiUsageQuery } from '../api/useAiUsageQuery';

function currentMonthRange(): { from: string; to: string } {
  const now = new Date();
  const from = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1));
  return { from: from.toISOString().slice(0, 10), to: now.toISOString().slice(0, 10) };
}

function formatCost(value: number | null): string {
  return value === null ? '—' : `$${value.toFixed(2)}`;
}

/**
 * "Foydalanish statistikasi" bloki — `docs/11` A-7: "joriy oy bo'yicha chaqiruvlar,
 * tokenlar, taxminiy xarajat" + provayder kesimidagi taqsimot (kichik bar chart).
 * Diagramma rang darajani baholamaydi — har qatorda raqamli qiymat va `aria-label`
 * (docs/11, 4-bo'lim), pastda ekran o'quvchisi uchun jadval alternativi.
 */
export function UsageStatsSection() {
  const { t } = useTranslation();
  const { from, to } = currentMonthRange();
  const usageQuery = useAiUsageQuery(from, to);

  return (
    <Card title={t('aiSettings.usage.heading')}>
      {usageQuery.isPending && (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      )}

      {usageQuery.isError && <ErrorState onRetry={() => void usageQuery.refetch()} />}

      {usageQuery.data && (
        <div className="flex flex-col gap-4">
          <dl className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div className="rounded-lg bg-neutral-50 p-3">
              <dt className="text-xs text-neutral-500">{t('aiSettings.usage.totalCalls')}</dt>
              <dd className="text-lg font-semibold text-neutral-900">{usageQuery.data.totalCalls}</dd>
            </div>
            <div className="rounded-lg bg-neutral-50 p-3">
              <dt className="text-xs text-neutral-500">{t('aiSettings.usage.inputTokens')}</dt>
              <dd className="text-lg font-semibold text-neutral-900">
                {usageQuery.data.totalInputTokens}
              </dd>
            </div>
            <div className="rounded-lg bg-neutral-50 p-3">
              <dt className="text-xs text-neutral-500">{t('aiSettings.usage.outputTokens')}</dt>
              <dd className="text-lg font-semibold text-neutral-900">
                {usageQuery.data.totalOutputTokens}
              </dd>
            </div>
            <div className="rounded-lg bg-neutral-50 p-3">
              <dt className="text-xs text-neutral-500">{t('aiSettings.usage.estimatedCost')}</dt>
              <dd className="text-lg font-semibold text-neutral-900">
                {formatCost(usageQuery.data.estimatedCostUsd)}
              </dd>
            </div>
          </dl>

          {usageQuery.data.byProvider.length === 0 ? (
            <EmptyState
              title={t('aiSettings.usage.emptyTitle')}
              description={t('aiSettings.usage.emptyDescription')}
            />
          ) : (
            <div role="img" aria-label={t('aiSettings.usage.breakdownAriaLabel')} className="flex flex-col gap-2.5">
              {usageQuery.data.byProvider.map((row) => {
                const byProvider = usageQuery.data?.byProvider ?? [];
                const maxCalls = Math.max(...byProvider.map((item) => item.calls), 1);
                const pct = Math.min(100, Math.max(0, (row.calls / maxCalls) * 100));
                return (
                  <div key={row.provider} className="flex flex-col gap-1">
                    <div className="flex items-baseline justify-between gap-2 text-sm">
                      <span className="font-medium text-neutral-700">
                        {t(`aiSettings.provider.names.${row.provider}`)}
                      </span>
                      <span className="font-semibold text-neutral-900">
                        {t('aiSettings.usage.callsCount', { count: row.calls })}
                      </span>
                    </div>
                    <div className="h-2.5 w-full overflow-hidden rounded-full bg-neutral-200">
                      <div className="h-full rounded-full bg-primary-500" style={{ width: `${String(pct)}%` }} />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </Card>
  );
}
