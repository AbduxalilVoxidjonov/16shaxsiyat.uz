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
import { ROUTES } from '@/shared/config/routes';
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
import { RERUNNABLE_STATUSES, type AiProvider } from '../model/types';

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

  const detailQuery = useAssessmentDetailQuery(id);
  const rerunMutation = useRerunAnalysisMutation();

  const [rerunOpen, setRerunOpen] = useState(false);
  const [rerunQueued, setRerunQueued] = useState(false);

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
  // Holat NOMA'LUM bo'lsa tugma ochiq qoladi: serverning qarorini oldindan taxmin qilmaymiz —
  // ruxsat etilmagan o'tishda backend `409` bilan aniq sabab qaytaradi.
  const canRerun = meta.status === null || RERUNNABLE_STATUSES.includes(meta.status);

  const rerunError = !rerunMutation.isError
    ? undefined
    : rerunMutation.error instanceof AppError && rerunMutation.error.status === 409
      ? t('assessmentDetail.rerunDialog.conflictError')
      : t('assessmentDetail.rerunDialog.error');

  async function handleRerun(provider: AiProvider | null) {
    if (!id) return;
    try {
      await rerunMutation.mutateAsync({ assessmentId: id, provider });
      toast.show({ variant: 'success', title: t('assessmentDetail.rerunDialog.success') });
      setRerunOpen(false);
      setRerunQueued(true);
    } catch {
      // Xato dialog ichida `error` orqali ko'rsatiladi — oyna ochiq qoladi.
    }
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
        canRerun={canRerun}
        onRerun={() => setRerunOpen(true)}
        rerunQueued={rerunQueued}
      />

      <RerunAnalysisDialog
        open={rerunOpen}
        onClose={() => setRerunOpen(false)}
        onConfirm={(provider) => void handleRerun(provider)}
        isSubmitting={rerunMutation.isPending}
        error={rerunError}
      />
    </div>
  );
}
