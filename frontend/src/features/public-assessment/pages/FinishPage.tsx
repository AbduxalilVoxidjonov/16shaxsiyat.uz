import { useEffect, useRef, useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, ErrorState, Skeleton, Spinner } from '@/shared/ui';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useSessionState } from '../api/useSessionState';
import { useCompleteSession } from '../api/useCompleteSession';
import { useSessionStore } from '../store/sessionStore';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';

/** docs/11 E-5: "Ruxsat berilgan bo'lsa 10–30 s ichida natija tugmasi faollashadi" — pastki chegara. */
const RESULT_BUTTON_DELAY_MS = 10_000;

function FinishSkeleton() {
  return (
    <div className="flex flex-col items-center gap-4 py-16" aria-hidden="true">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="size-8 rounded-full" />
      <Skeleton className="h-4 w-48" />
    </div>
  );
}

/**
 * E-5 Yakuniy ekran (`/t/:slug/finish`) — docs/11 E-5, docs/07 1.8-bo'lim.
 * `POST /sessions/complete` boshqa ommaviy `POST`lar bilan bir xil idempotentlik taxminiga
 * tayanib har mount'da (shu jumladan sahifa yangilanganda) chaqiriladi — `useCompleteSession.ts`
 * izohiga qarang (P12 tayyor bo'lgach tasdiqlanishi kerak).
 */
export default function FinishPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { slug = '' } = useParams<{ slug: string }>();

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const hasSession = Boolean(sessionToken) && storedSlug === slug;
  const handleSessionExpired = useSessionExpiredGuard(slug);

  const sessionStateQuery = useSessionState(hasSession);
  const completeSession = useCompleteSession();
  const calledRef = useRef(false);
  const [resultButtonReady, setResultButtonReady] = useState(false);

  usePageTitle(t('pages.finish.title'));

  useEffect(() => {
    if (sessionStateQuery.error instanceof AppError && sessionStateQuery.error.status === 410) {
      handleSessionExpired();
    }
  }, [sessionStateQuery.error, handleSessionExpired]);

  useEffect(() => {
    if (calledRef.current || !sessionStateQuery.data) return;
    if (sessionStateQuery.data.currentTestCode) return; // hali tugallanmagan — pastdagi guard yo'naltiradi
    calledRef.current = true;
    completeSession.mutate(undefined, {
      onError: (error) => {
        if (error instanceof AppError && error.status === 410) {
          handleSessionExpired();
        }
      },
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps -- `completeSession`/`handleSessionExpired` barqaror emas, `calledRef` bilan qo'lda himoyalangan
  }, [sessionStateQuery.data]);

  useEffect(() => {
    if (!completeSession.data?.showResultToStudent) return;
    const timer = window.setTimeout(() => setResultButtonReady(true), RESULT_BUTTON_DELAY_MS);
    return () => window.clearTimeout(timer);
  }, [completeSession.data?.showResultToStudent]);

  if (!hasSession) {
    return <Navigate to={ROUTES.public.landing(slug)} replace />;
  }

  if (sessionStateQuery.isPending) {
    return <FinishSkeleton />;
  }

  if (sessionStateQuery.isError) {
    return <ErrorState onRetry={() => void sessionStateQuery.refetch()} />;
  }

  const sessionState = sessionStateQuery.data;
  if (!sessionState) {
    return <FinishSkeleton />;
  }

  if (sessionState.currentTestCode) {
    return <Navigate to={ROUTES.public.test(slug, sessionState.currentTestCode)} replace />;
  }

  if (completeSession.isError) {
    const error = completeSession.error;
    if (!(error instanceof AppError && error.status === 410)) {
      return (
        <ErrorState
          onRetry={() => {
            calledRef.current = false;
            completeSession.mutate(undefined);
          }}
        />
      );
    }
    return <FinishSkeleton />; // 410 — `handleSessionExpired` effekt orqali navigatsiya qiladi
  }

  if (!completeSession.data) {
    return (
      <div className="flex flex-col items-center gap-4 py-16 text-center">
        <Spinner size={32} />
        <p className="text-neutral-600">{t('pages.finish.analyzing')}</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-4 py-16 text-center">
      <h1 className="text-xl font-bold text-neutral-900">{t('pages.finish.heading')}</h1>
      <Spinner size={32} />
      <p className="text-neutral-600">{t('pages.finish.analyzing')}</p>
      {completeSession.data.showResultToStudent ? (
        <Button
          size="lg"
          disabled={!resultButtonReady}
          onClick={() => navigate(ROUTES.public.result(slug))}
        >
          {t('pages.finish.viewResultCta')}
        </Button>
      ) : (
        <p className="text-sm text-neutral-500">{t('pages.finish.sentToPsychologist')}</p>
      )}
    </div>
  );
}
