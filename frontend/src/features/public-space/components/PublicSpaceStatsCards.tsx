import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { formatDate } from '@/shared/lib/formatDate';
import type { PublicSpaceStatsDto } from '../model/types';

export interface PublicSpaceStatsCardsProps {
  stats: PublicSpaceStatsDto;
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <Card className="flex flex-col gap-1">
      <span className="text-xs font-medium text-neutral-500">{label}</span>
      <span className="text-2xl font-bold text-neutral-900">{value}</span>
    </Card>
  );
}

/**
 * Ishtirok statistikasi. Hisoblagichlar DOIM son (hech kim ro'yxatdan o'tmagan bo'lsa `0`),
 * `lastActivityAt` esa ma'lumot yo'q bo'lsa `—` — `docs/06` §8 qoidasi ("ma'lumot yo'q ≠ 0").
 */
export function PublicSpaceStatsCards({ stats }: PublicSpaceStatsCardsProps) {
  const { t } = useTranslation();

  return (
    <section aria-label={t('publicSpace.stats.title')} className="flex flex-col gap-2">
      <h2 className="font-display text-base font-bold text-neutral-900">
        {t('publicSpace.stats.title')}
      </h2>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        <StatCard label={t('publicSpace.stats.userCount')} value={String(stats.userCount)} />
        <StatCard
          label={t('publicSpace.stats.totalAssessments')}
          value={String(stats.totalAssessments)}
        />
        <StatCard
          label={t('publicSpace.stats.inProgressCount')}
          value={String(stats.inProgressCount)}
        />
        <StatCard
          label={t('publicSpace.stats.completedCount')}
          value={String(stats.completedCount)}
        />
        <StatCard
          label={t('publicSpace.stats.analyzedCount')}
          value={String(stats.analyzedCount)}
        />
        <StatCard
          label={t('publicSpace.stats.lastActivityAt')}
          value={formatDate(stats.lastActivityAt)}
        />
      </div>
    </section>
  );
}
