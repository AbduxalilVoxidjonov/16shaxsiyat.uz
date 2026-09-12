import { useEffect, useRef, useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ErrorState, Skeleton } from '@/shared/ui';
import { GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { PUBLIC_SPACE_SLUG } from '@/shared/config/publicSpace';
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
      <Skeleton className="size-24 rounded-[26%] bg-line/70" />
      <Skeleton className="h-7 w-64 rounded-full bg-line/70" />
      <Skeleton className="h-4 w-48 rounded-full bg-line/70" />
    </div>
  );
}

/**
 * Sekin aylanuvchi girih yulduzi — sahifaning "vazmin kutish" belgisi. Sof dekorativ
 * (`GirihStar` o'zi `aria-hidden`), holat matni har doim yonida yozuv bilan beriladi.
 */
function WaitingMark() {
  return (
    <span className="relative grid size-28 shrink-0 place-items-center">
      <GirihStar className="absolute inset-0 animate-spin-slow text-firuza-200" strokeWidth={1.5} />
      <GirihStar
        className="absolute inset-[22%] animate-float text-firuza-400"
        strokeWidth={2}
        withCircle={false}
      />
    </span>
  );
}

/**
 * "Nafas oluvchi" uch nuqta — kutish davom etayotganini bildiradi. Matn emas, dekor:
 * holatning o'zi yonidagi paragrafda so'z bilan aytiladi.
 */
function WaitingDots() {
  return (
    <span className="flex items-center gap-1.5" aria-hidden="true">
      {[0, 1, 2].map((index) => (
        <span
          key={index}
          className="size-1.5 animate-pulse rounded-full bg-firuza-400"
          style={{ animationDelay: `${index * 220}ms` }}
        />
      ))}
    </span>
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

  /**
   * P52-B (egasining talabi): Telegram orqali kirgan ommaviy makon foydalanuvchisini
   * (`slug === PUBLIC_SPACE_SLUG`) saytning tanishtiruv sahifasiga (`/`) yuborish ma'nosiz —
   * uning allaqachon kabineti va butun test tarixi bor. Maktab o'quvchisini esa o'z maktab
   * sahifasiga (`/t/:slug`) qaytarish ham noto'g'ri: u yerda yana ro'yxatdan o'tish taklif
   * qilinadi, lekin takror topshirish baribir bloklanadi (`docs/07` §1.4) — shu sabab u
   * saytning umumiy bosh sahifasiga (`/`) yuboriladi.
   */
  const isPublicSpaceUser = slug === PUBLIC_SPACE_SLUG;
  const secondaryDestination = isPublicSpaceUser ? ROUTES.account.home : ROUTES.marketing.home;
  const secondaryLabel = isPublicSpaceUser
    ? t('pages.finish.backToAccountCta')
    : t('pages.finish.backToHomeCta');

  // `refetchOnMount: 'always'` — ESKI keshga tayanib qaror qabul qilmaslik uchun.
  // `queryClient` da `staleTime: 30_000`: tez o'quvchi barcha bloklarni 30 soniyadan tez
  // yechsa `sessions/me` bir marta ham qayta so'ralmasdi va pastdagi `currentTestCode`
  // qorovuli TESTNING BOSHIDAGI suratga qarab allaqachon tugallangan blokka qaytarib
  // yuborardi (u yerda `startTest` → `409` → bo'sh skelet, chiqib bo'lmaydigan halqa).
  const sessionStateQuery = useSessionState(hasSession, { refetchOnMount: 'always' });
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

  // `isFetching` ham kutiladi: mount'dagi qayta so'rov TUGAMAGUNCHA quyidagi
  // `currentTestCode` qorovuli eski ma'lumot bo'yicha yo'naltirib yuborishi mumkin edi.
  if (sessionStateQuery.isPending || sessionStateQuery.isFetching) {
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
      <section className="relative overflow-hidden rounded-5xl border border-line bg-paper-card px-6 py-14 text-center shadow-soft">
        <PatternBackdrop className="opacity-25" />
        <div className="relative flex flex-col items-center gap-6 animate-fade-in">
          <WaitingMark />
          <p role="status" className="flex items-center gap-2.5 text-[15px] text-ink-soft">
            {t('pages.finish.analyzing')}
            <WaitingDots />
          </p>
        </div>
      </section>
    );
  }

  // `docs/06` 8-bo'lim, CLAUDE.md MAXSUS DIQQAT 2/3-band: shaxsiyat batareyasisiz (yoki faqat
  // `Survey` blokli) dasturda ball/tip HECH QACHON hisoblanmaydi — shu sabab natija ekraniga
  // umuman taklif qilinmaydi: "bo'sh joy/0" o'rniga "javoblaringiz saqlandi" ko'rsatiladi.
  //
  // Manba — backend BAYROG'I (`GET /sessions/me` → `hasPersonalityBattery`, `docs/07` 1.3-bo'lim),
  // metodika kodi EMAS. Ilgari bu yerda `tests.some(t => t.code === 'MBTI16')` turardi va ikki
  // holatda JIMGINA buzilardi: (1) `Custom` dastur boshqa kodli metodika ishlatsa; (2) kod
  // o'zgarsa/versiyalansa. Bayroq domen qoidasi (`Domain.Catalog.PersonalityBattery`) bilan
  // hisoblanadi. `StudentResultPage` o'zi ham (to'g'ridan-to'g'ri havola bilan kirilsa) xuddi
  // shu bayroqni tekshiradi — himoya ikki qatlamda.
  if (!sessionState.hasPersonalityBattery) {
    return (
      <section className="relative overflow-hidden rounded-5xl border border-line bg-paper-card px-6 py-14 text-center shadow-soft">
        <PatternBackdrop className="opacity-25" />
        <div className="relative flex flex-col items-center gap-5 animate-fade-up">
          <WaitingMark />
          <h1 className="font-display text-2xl font-extrabold tracking-tight text-ink balance sm:text-3xl">
            {t('publicAssessment.survey.thanksHeading')}
          </h1>
          <p className="max-w-sm text-[15px] leading-relaxed text-ink-soft">
            {t('publicAssessment.survey.thanksMessage')}
          </p>
        </div>
      </section>
    );
  }

  return (
    <section className="relative overflow-hidden rounded-5xl border border-line bg-paper-card px-6 py-14 text-center shadow-soft">
      <PatternBackdrop className="opacity-25" />

      <div className="relative flex flex-col items-center gap-5 animate-fade-up">
        <WaitingMark />

        <p className="eyebrow">{t('pages.finish.title')}</p>
        <h1 className="font-display text-2xl font-extrabold tracking-tight text-ink balance sm:text-3xl">
          {t('pages.finish.heading')}
        </h1>

        <p
          role="status"
          className="flex items-center gap-2.5 text-[15px] leading-relaxed text-ink-soft"
        >
          {t('pages.finish.analyzing')}
          <WaitingDots />
        </p>

        {completeSession.data.showResultToStudent ? (
          <button
            type="button"
            className="btn btn-lg btn-primary mt-2"
            disabled={!resultButtonReady}
            onClick={() => navigate(ROUTES.public.result(slug))}
          >
            {t('pages.finish.viewResultCta')}
          </button>
        ) : (
          <p className="mt-1 max-w-sm rounded-4xl bg-paper-deep px-5 py-3 text-sm leading-relaxed text-ink-soft">
            {t('pages.finish.sentToPsychologist')}
          </p>
        )}

        {/* Ikkilamchi navigatsiya — natija tugmasidagi kechikishga BOG'LIQ EMAS, darhol
            faol (egasining talabi: kechikish faqat AI tahliliga tegishli). */}
        <button
          type="button"
          className="btn btn-lg btn-ghost"
          onClick={() => navigate(secondaryDestination)}
        >
          {secondaryLabel}
        </button>
      </div>
    </section>
  );
}
