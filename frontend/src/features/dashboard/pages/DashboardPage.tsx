import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Button } from '@/shared/ui/Button';
import { ROUTES } from '@/shared/config/routes';
import { useDashboardStatsQuery } from '../api/useDashboardStatsQuery';
import { readDashboardDateRange } from '../model/dateRangeFilters';
import { DateRangeFilter } from '../components/DateRangeFilter';
import { KpiCards } from '../components/KpiCards';
import { Last30DaysSection } from '../components/Last30DaysSection';
import { FunnelSection } from '../components/FunnelSection';
import { SchoolBreakdownTable } from '../components/SchoolBreakdownTable';
import { PersonalityDistributionChart } from '../components/PersonalityDistributionChart';
import { ActivityDistributionChart } from '../components/ActivityDistributionChart';
import { HollandTopChart } from '../components/HollandTopChart';
import { RecentAssessmentsList } from '../components/RecentAssessmentsList';

/** Yuqori qismning skelet holati — so'rov birinchi marta yuklanayotganda. */
function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-4" data-testid="dashboard-skeleton">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        {Array.from({ length: 5 }, (_, index) => (
          <Skeleton key={index} className="h-24 w-full rounded-xl" />
        ))}
      </div>
      <Skeleton className="h-32 w-full rounded-xl" />
      <Skeleton className="h-64 w-full rounded-xl" />
    </div>
  );
}

/**
 * Boshqaruv paneli (dashboard) — `/admin`. Vazifa ko'rsatmasi ("SAHIFA TARKIBI"):
 * KPI kartalar → voronka → maktablar kesimi → taqsimotlar → so'nggi sessiyalar → sana
 * filtri. `GET /api/admin/dashboard/stats?from=&to=` — docs/07, 3.6-bo'lim.
 *
 * Bitta so'rov butun sahifani boshqaradi (docs/10, 5.2-bo'lim: "dashboard 60s"
 * `staleTime`) — yuklanish/xato holati sahifa darajasida, chunki barcha bo'limlar bitta
 * javobdan keladi. `funnel`/`schoolBreakdown` ixtiyoriy (backend hali parallel
 * yozilmoqda) — ularning yo'qligi butun sahifani yiqitmaydi, faqat tegishli bo'lim
 * o'zining "hali mavjud emas" holatini ko'rsatadi (`FunnelSection`/`SchoolBreakdownTable`).
 */
export default function DashboardPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.dashboard.title'));
  const navigate = useNavigate();

  const [searchParams] = useSearchParams();
  const dateRange = readDashboardDateRange(searchParams);

  const statsQuery = useDashboardStatsQuery({
    from: dateRange.from || undefined,
    to: dateRange.to || undefined,
  });

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-xl font-semibold text-neutral-900">{t('pages.dashboard.title')}</h1>
      </div>

      <DateRangeFilter />

      {statsQuery.isError && (
        <ErrorState
          title={t('dashboard.error.title')}
          description={t('dashboard.error.description')}
          onRetry={() => void statsQuery.refetch()}
        />
      )}

      {!statsQuery.isError && statsQuery.isPending && <DashboardSkeleton />}

      {!statsQuery.isError && !statsQuery.isPending && statsQuery.data && (
        <>
          {statsQuery.data.totals.schools === 0 ? (
            <EmptyState
              title={t('dashboard.emptySchools.title')}
              description={t('dashboard.emptySchools.description')}
              action={
                <Button size="sm" onClick={() => navigate(ROUTES.admin.schools)}>
                  {t('dashboard.emptySchools.cta')}
                </Button>
              }
            />
          ) : (
            <>
              <KpiCards totals={statsQuery.data.totals} isLoading={false} />

              <Last30DaysSection data={statsQuery.data.last30Days} isLoading={false} />

              <FunnelSection funnel={statsQuery.data.funnel} />

              <SchoolBreakdownTable schoolBreakdown={statsQuery.data.schoolBreakdown} />

              <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
                <PersonalityDistributionChart items={statsQuery.data.personalityDistribution} />
                <ActivityDistributionChart items={statsQuery.data.activityDistribution} />
                <HollandTopChart items={statsQuery.data.hollandTop} />
              </div>

              <RecentAssessmentsList items={statsQuery.data.recentAssessments} />
            </>
          )}
        </>
      )}
    </div>
  );
}
