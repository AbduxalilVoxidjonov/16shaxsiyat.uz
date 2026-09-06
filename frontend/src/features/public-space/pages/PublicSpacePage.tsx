import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/Skeleton';
import { AppError } from '@/shared/api/AppError';
import { usePublicSpaceQuery } from '../api/usePublicSpaceQuery';
import { PublicSpaceAvailabilityAlert } from '../components/PublicSpaceAvailabilityAlert';
import { PublicSpaceLinkCard } from '../components/PublicSpaceLinkCard';
import { PublicSpaceProgramsCard } from '../components/PublicSpaceProgramsCard';
import { PublicSpaceShowResultCard } from '../components/PublicSpaceShowResultCard';
import { PublicSpaceStatsCards } from '../components/PublicSpaceStatsCards';
import { PublicSpaceStatusCard } from '../components/PublicSpaceStatusCard';

/** Backend `ProblemCodes.PublicSpaceNotConfigured` — seed bajarilmagan baza. */
const NOT_CONFIGURED_CODE = 'PUBLIC_SPACE_NOT_CONFIGURED';

function PublicSpaceSkeleton() {
  return (
    <div className="flex flex-col gap-4" data-testid="public-space-skeleton">
      <Skeleton className="h-16 w-full rounded-2xl" />
      <Skeleton className="h-32 w-full rounded-2xl" />
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        {Array.from({ length: 6 }, (_, index) => (
          <Skeleton key={index} className="h-24 w-full rounded-2xl" />
        ))}
      </div>
    </div>
  );
}

/**
 * `/admin/ommaviy` — ommaviy makon bo'limi (P48).
 *
 * Maktablar ro'yxatidan TO'LIQ ajratilgan: `GET /api/admin/schools` endi bu yozuvni umuman
 * qaytarmaydi (`Kind = School` filtri), shu sabab makonni boshqarishning yagona joyi shu
 * sahifa. "Hammasi bir admin panelda" — egasining talabi (2026-09-05).
 *
 * Bu yerda "maktab" atamasi ishlatilmaydi: makon, foydalanuvchilar, sessiyalar, havola.
 */
export default function PublicSpacePage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.publicSpace.title'));

  const spaceQuery = usePublicSpaceQuery();

  const isNotConfigured =
    spaceQuery.error instanceof AppError && spaceQuery.error.code === NOT_CONFIGURED_CODE;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <h1 className="text-xl font-semibold text-neutral-900">{t('pages.publicSpace.title')}</h1>
        <p className="max-w-3xl text-sm text-neutral-600">{t('publicSpace.subtitle')}</p>
      </div>

      {spaceQuery.isError && (
        <ErrorState
          title={
            isNotConfigured
              ? t('publicSpace.error.notConfiguredTitle')
              : t('publicSpace.error.title')
          }
          description={
            isNotConfigured
              ? t('publicSpace.error.notConfiguredDescription')
              : t('publicSpace.error.description')
          }
          onRetry={isNotConfigured ? undefined : () => void spaceQuery.refetch()}
        />
      )}

      {!spaceQuery.isError && spaceQuery.isPending && <PublicSpaceSkeleton />}

      {!spaceQuery.isError && !spaceQuery.isPending && spaceQuery.data && (
        <>
          {/*
            Signal ENG YUQORIDA (KPI/statistikadan ham oldin): 2026-09-03 hodisasida oqim
            jimgina o'lgan edi va panelda hech qanday belgi yo'q edi.
          */}
          <PublicSpaceAvailabilityAlert
            availability={spaceQuery.data.availability}
            programs={spaceQuery.data.programs}
          />

          <PublicSpaceStatusCard space={spaceQuery.data} />

          <PublicSpaceStatsCards stats={spaceQuery.data.stats} />

          <PublicSpaceProgramsCard programs={spaceQuery.data.programs} />

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <PublicSpaceShowResultCard
              showResultToStudent={spaceQuery.data.showResultToStudent}
            />
            <PublicSpaceLinkCard publicUrl={spaceQuery.data.publicUrl} />
          </div>
        </>
      )}
    </div>
  );
}
