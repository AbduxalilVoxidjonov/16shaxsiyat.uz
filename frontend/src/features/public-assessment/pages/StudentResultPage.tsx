import { useEffect } from 'react';
import { Navigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, Card, EmptyState, ErrorState, Skeleton } from '@/shared/ui';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useStudentResult } from '../api/useStudentResult';
import { useSessionStore } from '../store/sessionStore';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';

function ResultSkeleton() {
  return (
    <div className="flex flex-col gap-5" aria-hidden="true">
      <Skeleton className="h-32 w-full rounded-xl" />
      <Skeleton className="h-28 w-full rounded-xl" />
      <Skeleton className="h-28 w-full rounded-xl" />
    </div>
  );
}

/**
 * E-6 Qisqa natija (`/t/:slug/result`) — docs/11 E-6, docs/07 1.9-bo'lim.
 * **Ko'rsatilmaydi:** aktivlik ballari, `NeedsAttention`, xom ballar, to'liq AI hisobot
 * (CLAUDE.md 9-qoida, MAXSUS DIQQAT 6-band) — `StudentResultResponse` (`shared/api/types.ts`)
 * shartnomasi ham shu maydonlarni umuman o'z ichiga olmaydi.
 */
export default function StudentResultPage() {
  const { t } = useTranslation();
  const { slug = '' } = useParams<{ slug: string }>();

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const hasSession = Boolean(sessionToken) && storedSlug === slug;
  const handleSessionExpired = useSessionExpiredGuard(slug);

  const resultQuery = useStudentResult(hasSession);

  usePageTitle(t('pages.result.title'));

  useEffect(() => {
    if (resultQuery.error instanceof AppError && resultQuery.error.status === 410) {
      handleSessionExpired();
    }
  }, [resultQuery.error, handleSessionExpired]);

  if (!hasSession) {
    return <Navigate to={ROUTES.public.landing(slug)} replace />;
  }

  if (resultQuery.isPending) {
    return <ResultSkeleton />;
  }

  if (resultQuery.isError) {
    const error = resultQuery.error;

    // `202` — tahlil hali tayyor emas (docs/07 1.9-bo'lim) — CLAUDE.md MAXSUS DIQQAT 8-band.
    if (error instanceof AppError && error.status === 202) {
      return (
        <EmptyState
          title={t('pages.result.notReadyTitle')}
          description={t('pages.result.notReadyDescription')}
          action={
            <Button variant="outline" size="sm" onClick={() => void resultQuery.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      );
    }

    if (error instanceof AppError && error.status === 403) {
      return (
        <EmptyState
          title={t('pages.result.forbiddenTitle')}
          description={t('pages.result.forbiddenDescription')}
        />
      );
    }

    if (error instanceof AppError && error.status === 410) {
      return <ResultSkeleton />; // `handleSessionExpired` effekt orqali navigatsiya qiladi
    }

    return <ErrorState onRetry={() => void resultQuery.refetch()} />;
  }

  const result = resultQuery.data;
  if (!result) {
    return <ResultSkeleton />;
  }

  // `docs/06` 8-bo'lim, CLAUDE.md MAXSUS DIQQAT 3-band: dasturda shaxsiyat batareyasi
  // (MBTI16) bo'lmasa `GetStudentResultQueryHandler` baribir `200` qaytaradi, lekin
  // `personalityType`/`typeName`/`shortDescription` BO'SH QATOR bo'ladi — bo'sh tip kartasi
  // ko'rsatish "0" ko'rsatish bilan bir xil xato (soxta xulosa). Shu sabab bo'sh
  // `personalityType` — "natija yo'q" holati, xato EMAS.
  if (!result.personalityType) {
    return (
      <EmptyState
        title={t('publicAssessment.noBattery.title')}
        description={t('publicAssessment.noBattery.description')}
      />
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <Card className="flex flex-col items-center gap-1 text-center">
        <p className="text-3xl font-bold text-primary-700">{result.personalityType}</p>
        <p className="text-lg font-semibold text-neutral-900">{result.typeName}</p>
        <p className="mt-2 text-sm text-neutral-600">{result.shortDescription}</p>
      </Card>

      {result.topStrengths.length > 0 && (
        <Card title={t('pages.result.strengthsHeading')}>
          <ul className="flex flex-col gap-1.5 text-sm text-neutral-700">
            {result.topStrengths.map((strength) => (
              <li key={strength} className="flex items-start gap-2">
                <span aria-hidden="true">•</span>
                {strength}
              </li>
            ))}
          </ul>
        </Card>
      )}

      {result.careerFields.length > 0 && (
        <Card title={t('pages.result.careerFieldsHeading')}>
          <ul className="flex flex-col gap-1.5 text-sm text-neutral-700">
            {result.careerFields.map((field) => (
              <li key={field} className="flex items-start gap-2">
                <span aria-hidden="true">•</span>
                {field}
              </li>
            ))}
          </ul>
        </Card>
      )}

      <p className="text-center text-xs text-neutral-500">{result.note}</p>
    </div>
  );
}
