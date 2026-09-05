import { useEffect } from 'react';
import { Navigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ErrorState } from '@/shared/ui';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import {
  StudentResultNotice,
  StudentResultSkeleton,
  StudentResultView,
} from '@/widgets/StudentResultView';
import { useStudentResult } from '../api/useStudentResult';
import { useSessionState } from '../api/useSessionState';
import { useSessionStore } from '../store/sessionStore';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';

/**
 * E-6 Qisqa natija (`/t/:slug/result`) — docs/11 E-6, docs/07 1.9-bo'lim.
 *
 * Bu sahifa faqat HOLATNI aniqlaydi (sessiya bormi, batareya bormi, `202`/`403`/`410`);
 * ko'rinishning o'zi `widgets/StudentResultView` da va kabinet natija sahifasi bilan
 * UMUMIY (P47) — `docs/07` §5.3 bo'yicha javob shakli ikkalasida aynan bir xil, shu sabab
 * markap nusxa ko'chirilmaydi.
 *
 * **Ko'rsatilmaydi:** aktivlik ballari, `NeedsAttention`, xom ballar, to'liq AI hisobot
 * (CLAUDE.md 9-qoida, MAXSUS DIQQAT 6-band).
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
      <StudentResultNotice
        title={t('publicAssessment.noBattery.title')}
        description={t('publicAssessment.noBattery.description')}
      />
    );
  }

  if (resultQuery.isPending) {
    return <StudentResultSkeleton />;
  }

  if (resultQuery.isError) {
    const error = resultQuery.error;

    // `202` — tahlil hali tayyor emas (docs/07 1.9-bo'lim) — CLAUDE.md MAXSUS DIQQAT 8-band.
    if (error instanceof AppError && error.status === 202) {
      return (
        <StudentResultNotice
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
        <StudentResultNotice
          title={t('pages.result.forbiddenTitle')}
          description={t('pages.result.forbiddenDescription')}
        />
      );
    }

    if (error instanceof AppError && error.status === 410) {
      return <StudentResultSkeleton />; // `handleSessionExpired` effekt orqali navigatsiya qiladi
    }

    return <ErrorState onRetry={() => void resultQuery.refetch()} />;
  }

  const result = resultQuery.data;
  if (!result) {
    return <StudentResultSkeleton />;
  }

  // Ikkinchi himoya qatlami — `docs/06` 8-bo'lim, CLAUDE.md MAXSUS DIQQAT 3-band: batareyasiz
  // dasturda `GetStudentResultQueryHandler` baribir `200` qaytaradi, lekin
  // `personalityType`/`typeName`/`shortDescription` BO'SH QATOR bo'ladi — bo'sh tip kartasi
  // ko'rsatish "0" ko'rsatish bilan bir xil xato (soxta xulosa). Shu sabab bo'sh
  // `personalityType` — "natija yo'q" holati, xato EMAS.
  if (!result.personalityType) {
    return (
      <StudentResultNotice
        title={t('publicAssessment.noBattery.title')}
        description={t('publicAssessment.noBattery.description')}
      />
    );
  }

  return <StudentResultView result={result} />;
}
