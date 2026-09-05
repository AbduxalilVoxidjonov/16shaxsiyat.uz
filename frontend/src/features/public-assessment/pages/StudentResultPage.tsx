import { useEffect, type ReactNode } from 'react';
import { Navigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Info } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ErrorState, Skeleton } from '@/shared/ui';
import { Divider, GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useStudentResult } from '../api/useStudentResult';
import { useSessionState } from '../api/useSessionState';
import { useSessionStore } from '../store/sessionStore';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';

function ResultSkeleton() {
  return (
    <div className="flex flex-col gap-5" aria-hidden="true">
      <Skeleton className="h-56 w-full rounded-5xl bg-line/70" />
      <Skeleton className="h-28 w-full rounded-4xl bg-line/70" />
      <Skeleton className="h-28 w-full rounded-4xl bg-line/70" />
    </div>
  );
}

interface NoticeCardProps {
  title: string;
  description: string;
  /** Girih nishoni sekin aylansa — "kutish" ma'nosi; aylanmasa — yakuniy, tinch holat. */
  spinning?: boolean;
  action?: ReactNode;
}

/**
 * Natija ekranining "kartasiz" holatlari — tayyorlanmoqda / ko'rsatilmaydi / bu dasturda yo'q.
 * Uchchalasi ham xato EMAS (odatiy oqim), shu sabab qizil "xato" uslubi ataylab ishlatilmagan:
 * bir xil vazmin qog'oz karta, girih nishoni va tushuntirish matni.
 */
function NoticeCard({ title, description, spinning = false, action }: NoticeCardProps) {
  return (
    <section className="relative overflow-hidden rounded-5xl border border-line bg-paper-card px-6 py-14 text-center shadow-soft">
      <PatternBackdrop className="opacity-25" />
      <div className="relative flex flex-col items-center gap-4 animate-fade-up">
        <span className="relative grid size-24 place-items-center">
          <GirihStar
            className={
              spinning
                ? 'absolute inset-0 animate-spin-slow text-firuza-200'
                : 'absolute inset-0 text-line-strong'
            }
            strokeWidth={1.5}
          />
          <GirihStar
            className={
              spinning
                ? 'absolute inset-[24%] text-firuza-400'
                : 'absolute inset-[24%] text-ink-faint'
            }
            strokeWidth={2}
            withCircle={false}
          />
        </span>
        <h1 className="font-display text-xl font-extrabold tracking-tight text-ink balance sm:text-2xl">
          {title}
        </h1>
        <p className="max-w-sm text-[15px] leading-relaxed text-ink-soft">{description}</p>
        {action}
      </div>
    </section>
  );
}

/**
 * E-6 Qisqa natija (`/t/:slug/result`) — docs/11 E-6, docs/07 1.9-bo'lim.
 * **Ko'rsatilmaydi:** aktivlik ballari, `NeedsAttention`, xom ballar, to'liq AI hisobot
 * (CLAUDE.md 9-qoida, MAXSUS DIQQAT 6-band) — `StudentResultResponse` (`shared/api/types.ts`)
 * shartnomasi ham shu maydonlarni umuman o'z ichiga olmaydi.
 *
 * Dizayn eslatmasi: backend o'lcham/ball/diagramma qaytarmagani uchun bu ekranda hech qanday
 * progress-bar yoki foiz KO'RSATILMAYDI — mavjud bo'lmagan ma'lumotni "chizib qo'yish"
 * soxta xulosa bo'lardi (docs/06 8-bo'lim). Vizual og'irlik emblema, tipografiya va
 * ro'yxatlar orqali beriladi.
 */
export default function StudentResultPage() {
  const { t } = useTranslation();
  const { slug = '' } = useParams<{ slug: string }>();

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const hasSession = Boolean(sessionToken) && storedSlug === slug;
  const handleSessionExpired = useSessionExpiredGuard(slug);

  const resultQuery = useStudentResult(hasSession);
  // Batareya bayrog'i sessiya holatidan keladi (`docs/07` 1.3-bo'lim). `FinishPage` allaqachon
  // shu so'rovni bajargan — bir xil `queryKey`, ya'ni odatda keshdan olinadi.
  const sessionStateQuery = useSessionState(hasSession);

  usePageTitle(t('pages.result.title'));

  useEffect(() => {
    if (resultQuery.error instanceof AppError && resultQuery.error.status === 410) {
      handleSessionExpired();
    }
  }, [resultQuery.error, handleSessionExpired]);

  if (!hasSession) {
    return <Navigate to={ROUTES.public.landing(slug)} replace />;
  }

  // `docs/06` 8-bo'lim (2026-09-02 qaror) + qarorlar jurnali: "ma'lumot yo'q" `0`/bo'sh karta
  // bilan almashtirilmaydi. Dasturda shaxsiyat batareyasi bo'lmasa tip HECH QACHON hisoblanmaydi,
  // shu sabab shaxsiyat widget'lari umuman render qilinmaydi — bayroq bo'yicha, metodika KODI
  // bo'yicha emas (`hasPersonalityBattery`, `Domain.Catalog.PersonalityBattery`). `undefined`
  // (holat hali kelmagan/xato) — "yo'q" DEGANI EMAS, shu sabab qat'iy `=== false`.
  if (sessionStateQuery.data?.hasPersonalityBattery === false) {
    return (
      <NoticeCard
        title={t('publicAssessment.noBattery.title')}
        description={t('publicAssessment.noBattery.description')}
      />
    );
  }

  if (resultQuery.isPending) {
    return <ResultSkeleton />;
  }

  if (resultQuery.isError) {
    const error = resultQuery.error;

    // `202` — tahlil hali tayyor emas (docs/07 1.9-bo'lim) — CLAUDE.md MAXSUS DIQQAT 8-band.
    if (error instanceof AppError && error.status === 202) {
      return (
        <NoticeCard
          spinning
          title={t('pages.result.notReadyTitle')}
          description={t('pages.result.notReadyDescription')}
          action={
            <button
              type="button"
              className="btn btn-md btn-ghost mt-2"
              onClick={() => void resultQuery.refetch()}
            >
              {t('common.retry')}
            </button>
          }
        />
      );
    }

    // `403` — `App:ShowResultToStudent` o'chirilgan (STANDART sozlama, docs/07 8-bo'lim).
    // Bu ODATIY holat, nosozlik emas: shu sabab "xato" emas, xushmuomala tushuntirish.
    if (error instanceof AppError && error.status === 403) {
      return (
        <NoticeCard
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

  // Ikkinchi himoya qatlami — `docs/06` 8-bo'lim, CLAUDE.md MAXSUS DIQQAT 3-band: batareyasiz
  // dasturda `GetStudentResultQueryHandler` baribir `200` qaytaradi, lekin
  // `personalityType`/`typeName`/`shortDescription` BO'SH QATOR bo'ladi — bo'sh tip kartasi
  // ko'rsatish "0" ko'rsatish bilan bir xil xato (soxta xulosa). Shu sabab bo'sh
  // `personalityType` — "natija yo'q" holati, xato EMAS.
  if (!result.personalityType) {
    return (
      <NoticeCard
        title={t('publicAssessment.noBattery.title')}
        description={t('publicAssessment.noBattery.description')}
      />
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {/*
        Emblema rangi ATAYLAB har doim brend firuzasi: kodni guruh/oila rangiga bog'lash
        begona tasniflash tizimini takrorlash bo'lardi (CLAUDE.md 6a — huquqiy qoida).
        Kod matni oq, `firuza-500 → firuza-800` gradient ustida va `text-3xl` (yirik, qalin)
        — eng och burchakda ham WCAG AA (yirik matn uchun 3:1) chegarasidan yuqori.
      */}
      <section className="relative overflow-hidden rounded-5xl border border-line bg-firuza-50/70 px-6 py-10 text-center shadow-soft sm:px-8 sm:py-12">
        <PatternBackdrop className="opacity-30" />
        <GirihStar
          className="pointer-events-none absolute -top-20 -right-20 size-64 animate-spin-slow text-firuza-200/60"
          strokeWidth={1.5}
        />

        <div className="relative flex flex-col items-center gap-5 animate-fade-up">
          <span className="relative grid size-28 shrink-0 place-items-center rounded-[26%] bg-linear-to-br from-firuza-500 to-firuza-800 shadow-glow sm:size-32">
            <GirihStar className="absolute inset-[10%] text-white/30" strokeWidth={1.5} />
            <GirihStar
              className="absolute inset-[26%] text-white/20"
              strokeWidth={1.5}
              withCircle={false}
            />
            <span className="relative font-display text-3xl font-extrabold tracking-[0.14em] text-white">
              {result.personalityType}
            </span>
          </span>

          <div>
            <p className="eyebrow">{t('pages.result.title')}</p>
            <h1 className="mt-2 font-display text-3xl font-extrabold tracking-tight text-ink balance sm:text-4xl">
              {result.typeName}
            </h1>
          </div>

          {result.shortDescription && (
            <p className="max-w-prose text-[15px] leading-relaxed text-ink-soft sm:text-base">
              {result.shortDescription}
            </p>
          )}
        </div>
      </section>

      {(result.topStrengths.length > 0 || result.careerFields.length > 0) && <Divider />}

      {result.topStrengths.length > 0 && (
        <section className="card p-6 sm:p-7">
          <h2 className="font-display text-lg font-extrabold tracking-tight text-ink">
            {t('pages.result.strengthsHeading')}
          </h2>
          <ul className="mt-4 flex flex-col gap-3">
            {result.topStrengths.map((strength) => (
              <li key={strength} className="flex gap-3 text-[15px] leading-relaxed text-ink-soft">
                <span
                  aria-hidden="true"
                  className="mt-2 size-1.5 shrink-0 rounded-full bg-firuza-500"
                />
                {strength}
              </li>
            ))}
          </ul>
        </section>
      )}

      {result.careerFields.length > 0 && (
        <section className="card p-6 sm:p-7">
          <h2 className="font-display text-lg font-extrabold tracking-tight text-ink">
            {t('pages.result.careerFieldsHeading')}
          </h2>
          <ul className="mt-4 flex flex-wrap gap-2">
            {result.careerFields.map((field) => (
              <li key={field} className="chip bg-lojuvard-50 text-lojuvard-700">
                {field}
              </li>
            ))}
          </ul>
        </section>
      )}

      {/*
        CLAUDE.md 6-qoidasi: "AI tashxis qo'ymaydi" izohi natija sahifasida KO'RINARLI joyda
        bo'lishi shart — shu sabab u kichik kulrang izoh emas, alohida rangli panel.
      */}
      <aside className="flex items-start gap-3 rounded-4xl border border-zarhal-200 bg-zarhal-50 px-5 py-4">
        <Info className="mt-0.5 size-5 shrink-0 text-zarhal-700" aria-hidden="true" />
        <p className="text-sm leading-relaxed text-ink-soft">{result.note}</p>
      </aside>
    </div>
  );
}
