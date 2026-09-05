import { useEffect } from 'react';
import { Navigate, useLocation, useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, ErrorState, Skeleton } from '@/shared/ui';
import { GirihStar } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import type { PublicTestSummary } from '@/shared/api/types';
import { useSessionState } from '../api/useSessionState';
import { useSessionStore } from '../store/sessionStore';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import { publicButtonClass } from '../components/publicStyles';

interface TestCompleteLocationState {
  nextTestCode?: string | null;
  /**
   * `TestPage`dan uzatilgan sessiya test ro'yxati (`PublicTestSummaryDto[]`, `name`/
   * `estimatedMinutes` bilan) — qo'shimcha `GET /sessions/me` so'rovisiz "qolgan bloklar"
   * ro'yxatini qurish uchun (P36). Yo'q bo'lsa (to'g'ridan-to'g'ri havola/sahifa yangilanishi)
   * pastda sessiya holatidan qayta so'raladi.
   */
  tests?: PublicTestSummary[];
}

function TestCompleteSkeleton() {
  return (
    <div className="flex flex-col items-center gap-4 py-10" aria-hidden="true">
      <Skeleton className="size-20 rounded-full" />
      <Skeleton className="h-8 w-56" />
      <Skeleton className="h-24 w-full rounded-3xl" />
      <Skeleton className="h-14 w-full rounded-full" />
    </div>
  );
}

/**
 * E-4 Blok yakuni (`/t/:slug/test/:testCode/done`) — docs/11 E-4.
 * `TestPage` bu sahifaga `navigate(..., { state: { nextTestCode, tests } })` bilan keladi
 * (`allTestsCompleted: false` bo'lganda). Sahifa yangilansa/to'g'ridan-to'g'ri ochilsa
 * `location.state` yo'qoladi — bu holda `GET /sessions/me` orqali qayta hisoblanadi
 * (docs/07 1.3-bo'lim). `tests` — sessiyaning HAQIQIY dasturiga tegishli ro'yxat (P36:
 * bir nechta dastur bo'lganda maktabning BARCHA testlari EMAS), shu sabab har doim shu
 * manbadan olinadi — alohida "katalog" (`sessionStore`) YO'Q.
 */
export default function TestCompletePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { slug = '' } = useParams<{ slug: string }>();

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const hasSession = Boolean(sessionToken) && storedSlug === slug;

  const handleSessionExpired = useSessionExpiredGuard(slug);

  const locationState = location.state as TestCompleteLocationState | null;
  const stateNextTestCode = locationState?.nextTestCode;
  const stateTests = locationState?.tests;
  const needsSessionFetch = stateNextTestCode === undefined || stateTests === undefined;
  const sessionStateQuery = useSessionState(hasSession && needsSessionFetch);

  usePageTitle(t('pages.testDone.title'));

  useEffect(() => {
    if (sessionStateQuery.error instanceof AppError && sessionStateQuery.error.status === 410) {
      handleSessionExpired();
    }
  }, [sessionStateQuery.error, handleSessionExpired]);

  if (!hasSession) {
    return <Navigate to={ROUTES.public.landing(slug)} replace />;
  }

  let nextTestCode: string | null = stateNextTestCode ?? null;
  let remainingSourceTests: PublicTestSummary[] = stateTests ?? [];
  if (needsSessionFetch) {
    if (sessionStateQuery.isPending) {
      return <TestCompleteSkeleton />;
    }
    if (sessionStateQuery.isError) {
      return <ErrorState onRetry={() => void sessionStateQuery.refetch()} />;
    }
    const data = sessionStateQuery.data;
    if (!data) {
      return <TestCompleteSkeleton />;
    }
    nextTestCode = data.currentTestCode ?? pickNextTestCode(data.tests);
    remainingSourceTests = data.tests;
  }

  if (!nextTestCode) {
    // Qolgan test yo'q — `TestPage` bu holatda to'g'ridan-to'g'ri `finish`ga yo'naltiradi,
    // lekin to'g'ridan-to'g'ri havola/eskirgan holat uchun himoya sifatida qoldirilgan.
    return <Navigate to={ROUTES.public.finish(slug)} replace />;
  }
  const resolvedNextTestCode = nextTestCode;

  const nextCatalogItem = remainingSourceTests.find((item) => item.code === resolvedNextTestCode);
  const remainingCatalogItems = nextCatalogItem
    ? remainingSourceTests.filter((item) => item.order >= nextCatalogItem.order)
    : [];
  const remainingMinutes = remainingCatalogItems.reduce(
    (sum, item) => sum + item.estimatedMinutes,
    0,
  );

  return (
    <div className="flex animate-fade-up flex-col items-center gap-6 py-10 text-center motion-reduce:animate-none">
      {/*
        Bayram belgisi emoji o'rniga girih yulduzi: brend tiliga mos va "vazmin" qoladi.
        Dekorativ — `GirihStar` o'zi `aria-hidden`, sarlavha ma'noni to'liq yetkazadi.
      */}
      <span className="relative grid size-20 place-items-center rounded-full bg-firuza-50 ring-1 ring-firuza-100">
        <GirihStar className="size-11 text-firuza-500" strokeWidth={2.5} />
      </span>

      <h1 className="font-display text-2xl font-extrabold tracking-tight text-balance text-ink">
        {t('pages.testDone.heading')}
      </h1>

      {remainingCatalogItems.length > 0 && (
        <div className="card w-full rounded-3xl p-5 text-left">
          <p className="mb-3 text-sm font-semibold text-ink">
            {t('pages.testDone.remainingHeading', {
              count: remainingCatalogItems.length,
              minutes: remainingMinutes,
            })}
          </p>
          <ul className="flex flex-col gap-2 text-sm text-ink-soft">
            {remainingCatalogItems.map((item) => (
              <li key={item.code} className="flex items-center gap-2.5">
                <span aria-hidden="true" className="size-1.5 shrink-0 rounded-full bg-firuza-400" />
                {item.name}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="flex w-full flex-col items-stretch gap-3">
        <Button
          size="lg"
          className={publicButtonClass('primary', 'lg', 'w-full')}
          onClick={() => navigate(ROUTES.public.test(slug, resolvedNextTestCode))}
        >
          {t('pages.testDone.continueCta')}
        </Button>
        <Button
          variant="ghost"
          className={publicButtonClass('ghost', 'md', 'w-full')}
          onClick={() => navigate(ROUTES.public.landing(slug))}
        >
          {t('pages.testDone.laterCta')}
        </Button>
      </div>
    </div>
  );
}
