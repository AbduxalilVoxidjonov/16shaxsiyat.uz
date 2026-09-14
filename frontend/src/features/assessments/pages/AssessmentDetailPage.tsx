import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useParams } from 'react-router';
import { ArrowLeft } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useAiReadinessQuery } from '@/shared/api/useAiReadinessQuery';
import { AI_RUNNABLE_STATUSES } from '@/shared/lib/aiAnalysisState';
import { ROUTES } from '@/shared/config/routes';
import { AnswersSection } from '@/widgets/AnswersSection';
import { useAssessmentDetailQuery } from '../api/useAssessmentDetailQuery';
import { useRerunAnalysisMutation } from '../api/useRerunAnalysisMutation';
import { AiAnalysisSection } from '../components/AiAnalysisSection';
import { ReliabilitySection } from '../components/ReliabilitySection';
import { RerunAnalysisDialog } from '../components/RerunAnalysisDialog';
import { SessionMetaCard } from '../components/SessionMetaCard';
import { TestResultsSummary } from '../components/TestResultsSummary';
import {
  isSessionMetaEmpty,
  parseAssessmentLocationState,
  resolveSessionMeta,
} from '../model/sessionMeta';
import { buildTestSummaryRows, normalizeTestResults } from '../model/testSummary';
import { type AiProvider } from '../model/types';

function DetailSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-40 w-full" />
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Skeleton className="h-48 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
      <Skeleton className="h-64 w-full" />
    </div>
  );
}

/**
 * `/admin/assessments/:id` — bitta sessiyaning to'liq ko'rinishi (`docs/11` A-6,
 * `docs/07` 3.3-bo'lim).
 *
 * Ma'lumot manbalari: `GET /api/admin/assessments/{id}` (natijalar va AI tahlili) hamda —
 * sarlavha maydonlari uchun — sessiyalar ro'yxatidan kelgan navigatsiya holati. Detal
 * endpointi hozircha `{id, results, aiAnalysis, aiHistory}` dan iborat (`docs/07` 3.3:
 * "yuqoridagi `latestAssessment` shakli"), shu sabab holat/vaqt/maktab/o'quvchi to'g'ridan-
 * to'g'ri kelmaydi. Ular topilmasa sahifa "ma'lumot yo'q" deb ANIQ yozadi va sababini
 * tushuntiradi — `0` yoki bo'sh satr KO'RSATILMAYDI (`docs/06` qarorlar jurnali, 2026-09-02).
 */
export default function AssessmentDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const toast = useToast();
  usePageTitle(t('pages.assessmentDetail.title'));

  const { query: detailQuery, pollTimedOut } = useAssessmentDetailQuery(id);
  const rerunMutation = useRerunAnalysisMutation();

  const [rerunOpen, setRerunOpen] = useState(false);
  const [rerunQueued, setRerunQueued] = useState(false);

  // Provayder sozlanmagani haqidagi ogohlantirish FAQAT tahlil tugmasi chiqadigan
  // holatlarda kerak — boshqa holatda ortiqcha so'rov yuborilmaydi.
  const detailStatus = detailQuery.data?.status ?? null;
  const { hasConfiguredProvider } = useAiReadinessQuery(
    detailStatus !== null && AI_RUNNABLE_STATUSES.includes(detailStatus),
  );

  const backLink = (
    <Link
      to={ROUTES.admin.assessments}
      className="flex w-fit items-center gap-1.5 text-sm text-neutral-500 hover:text-neutral-900"
    >
      <ArrowLeft size={16} aria-hidden="true" />
      {t('assessmentDetail.backCta')}
    </Link>
  );

  if (detailQuery.isPending) {
    return <DetailSkeleton />;
  }

  if (detailQuery.isError) {
    const isNotFound = detailQuery.error instanceof AppError && detailQuery.error.status === 404;
    return (
      <div className="flex flex-col gap-4">
        {backLink}
        {isNotFound ? (
          <EmptyState
            title={t('assessmentDetail.notFoundTitle')}
            description={t('assessmentDetail.notFoundDescription')}
          />
        ) : (
          <ErrorState
            description={t('assessmentDetail.loadError')}
            onRetry={() => void detailQuery.refetch()}
          />
        )}
      </div>
    );
  }

  const detail = detailQuery.data;
  const meta = resolveSessionMeta(detail, parseAssessmentLocationState(location.state));
  const results = normalizeTestResults(detail.results);
  const rows = buildTestSummaryRows(results, detail.tests);
  // `noPersonalityBattery` bloklaganda tugma allaqachon ko'rinmaydi (mijoz tomonidagi UX
  // qoidasi), lekin server har doim ham qo'riqchi — eskirgan sahifa holati yoki boshqa
  // yo'l bilan chaqiruv yuborilsa, `RerunAnalysisCommandHandler` `409
  // ASSESSMENT_NO_PERSONALITY_BATTERY` qaytaradi (haqiqiy himoya). Shu kod uchun alohida,
  // aniqroq xabar — qolgan `409` (`ASSESSMENT_INVALID_TRANSITION`) umumiy xabarda qoladi.
  const rerunError = !rerunMutation.isError
    ? undefined
    : rerunMutation.error instanceof AppError && rerunMutation.error.code === 'ASSESSMENT_NO_PERSONALITY_BATTERY'
      ? t('assessmentDetail.rerunDialog.noPersonalityBatteryError')
      : rerunMutation.error instanceof AppError && rerunMutation.error.status === 409
        ? t('assessmentDetail.rerunDialog.conflictError')
        : t('assessmentDetail.rerunDialog.error');

  /**
   * `POST /api/admin/assessments/{id}/rerun-analysis` — `docs/07` 3.3. Domen qo'riqchisi
   * `Completed` dan ham ruxsat beradi, ya'ni BIRINCHI tahlil ham shu endpoint orqali ketadi.
   *
   * @param viaDialog Tasdiq oynasidan chaqirildimi — xato o'sha oynada ko'rsatiladi;
   *   tasdiqsiz (birinchi) tahlilda xato toast bilan aytiladi.
   */
  async function runAnalysis(provider: AiProvider | null, viaDialog: boolean) {
    if (!id) return;
    try {
      await rerunMutation.mutateAsync({ assessmentId: id, provider });
      toast.show({ variant: 'success', title: t('assessmentDetail.rerunDialog.success') });
      setRerunOpen(false);
      setRerunQueued(true);
    } catch {
      if (!viaDialog) {
        toast.show({ variant: 'danger', title: rerunError ?? t('assessmentDetail.rerunDialog.error') });
      }
      // Tasdiq oynasidagi xato dialog ichida `error` orqali ko'rsatiladi — oyna ochiq qoladi.
    }
  }

  /** Tasdiq FAQAT mavjud hisobot ustiga yozilganda so'raladi (birinchi tahlilda yo'q). */
  function handleRunAnalysis({ requiresConfirmation }: { requiresConfirmation: boolean }) {
    if (requiresConfirmation) {
      setRerunOpen(true);
      return;
    }
    void runAnalysis(null, false);
  }

  return (
    <div className="flex flex-col gap-4">
      {backLink}

      <div>
        <h1 className="text-xl font-semibold text-neutral-900">{t('assessmentDetail.heading')}</h1>
        <p className="text-sm text-neutral-500">
          {meta.student
            ? meta.student.fullName
            : t('assessmentDetail.idCaption', { id: detail.id })}
        </p>
      </div>

      {/* Ekran o'quvchisi uchun bo'lim sarlavhasi — `Card` ichidagi `h3` ga h1 dan
          to'g'ridan-to'g'ri sakramaslik uchun (axe `heading-order`). */}
      <h2 className="sr-only">{t('assessmentDetail.overviewHeading')}</h2>

      <SessionMetaCard meta={meta} showUnavailableNotice={isSessionMetaEmpty(meta)} />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ReliabilitySection score={meta.reliabilityScore} flag={meta.reliabilityFlag} />
        <TestResultsSummary rows={rows} />
      </div>

      <AiAnalysisSection
        analysis={detail.aiAnalysis}
        history={detail.aiHistory ?? []}
        assessmentStatus={meta.status}
        onRunAnalysis={handleRunAnalysis}
        isStartingAnalysis={rerunMutation.isPending && !rerunOpen}
        rerunQueued={rerunQueued}
        pollTimedOut={pollTimedOut}
        hasConfiguredProvider={hasConfiguredProvider}
        hasPersonalityBattery={detail.hasPersonalityBattery}
      />

      {/* Savolma-savol javoblar — egasining talabi (2026-09-12): tarixdagi HAR BIR
          sessiyaning javoblarini ko'rish, faqat eng so'nggisini emas (`StudentProfilePage`
          ilgari faqat `latestAssessment` uchun ochardi). Xuddi shu widget — `docs/10` §2. */}
      <AnswersSection assessmentId={detail.id} />

      <RerunAnalysisDialog
        open={rerunOpen}
        onClose={() => setRerunOpen(false)}
        onConfirm={(provider) => void runAnalysis(provider, true)}
        isSubmitting={rerunMutation.isPending}
        error={rerunError}
      />
    </div>
  );
}
