import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useAiReadinessQuery } from '@/shared/api/useAiReadinessQuery';
import { AI_RUNNABLE_STATUSES, hasAiReport } from '@/shared/lib/aiAnalysisState';
import { ROUTES } from '@/shared/config/routes';
import { useStudentProfileQuery } from '../api/useStudentProfileQuery';
import { useDownloadReportPdfMutation } from '../api/useDownloadReportPdfMutation';
import { useRerunAnalysisMutation } from '../api/useRerunAnalysisMutation';
import { useDeleteStudentMutation } from '../api/useDeleteStudentMutation';
import { StudentProfileHeader } from '../components/StudentProfileHeader';
import { StudentSummaryCards } from '../components/StudentSummaryCards';
import { StudentDiagramsSection } from '../components/StudentDiagramsSection';
import { AiReportSection } from '../components/AiReportSection';
import { AssessmentHistoryTable } from '../components/AssessmentHistoryTable';
import { AnswersSection } from '@/widgets/AnswersSection';
import { RerunAnalysisDialog } from '../components/RerunAnalysisDialog';
import type { AiProvider } from '../model/profileTypes';
import { buildPresentTestCodes } from '../model/testBattery';

/** Sarlavha va yig'ma kartalar yuklanayotganda ko'rsatiladigan skelet — 3s ichida bosqichma-bosqich (CLAUDE.md cheklovi). */
function ProfileSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-5 w-96" />
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[0, 1, 2, 3].map((index) => (
          <Skeleton key={index} className="h-24 w-full" />
        ))}
      </div>
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Skeleton className="h-64 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    </div>
  );
}

/**
 * `/admin/students/:id` — superadmin eng ko'p ishlatadigan ekran (docs/11 A-5, `prompts/25`).
 * P26 widget'laridan (`widgets/`) foydalanadi; AI qismi P16–P18 hali yozilmagani uchun
 * bo'sh/`null` ma'lumotga chidamli (`AiReportSection`/`AiReportView` — har biri mustaqil
 * himoyalangan).
 */
export default function StudentProfilePage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  usePageTitle(t('pages.studentProfile.title'));

  const { query: profileQuery, pollTimedOut } = useStudentProfileQuery(id);
  const downloadPdfMutation = useDownloadReportPdfMutation();
  const rerunMutation = useRerunAnalysisMutation();
  const deleteMutation = useDeleteStudentMutation();

  const [rerunOpen, setRerunOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  // Provayder sozlanmagani haqidagi ogohlantirish FAQAT tahlil tugmasi chiqadigan
  // holatlarda kerak — boshqa holatda ortiqcha so'rov yuborilmaydi.
  const latestStatus =
    profileQuery.data?.assessments.find((assessment) => assessment.isLatest)?.status ?? null;
  const { hasConfiguredProvider } = useAiReadinessQuery(
    latestStatus !== null && AI_RUNNABLE_STATUSES.includes(latestStatus),
  );

  if (profileQuery.isPending) {
    return <ProfileSkeleton />;
  }

  if (profileQuery.isError) {
    const isNotFound = profileQuery.error instanceof AppError && profileQuery.error.status === 404;
    if (isNotFound) {
      return (
        <EmptyState
          title={t('studentProfile.notFoundTitle')}
          description={t('studentProfile.notFoundDescription')}
        />
      );
    }
    return (
      <ErrorState
        description={t('studentProfile.loadError')}
        onRetry={() => void profileQuery.refetch()}
      />
    );
  }

  const { student, assessments, latestAssessment } = profileQuery.data;
  const latestSummary = assessments.find((assessment) => assessment.isLatest) ?? null;
  const results = latestAssessment?.results;

  // P52 jonli xato tuzatish (2026-09-12): "Hali natija yo'q" faqat metodika SESSIYADA BOR-yu
  // hisoblanmagan holatda o'rinli — metodika dasturda umuman yo'q bo'lsa (masalan faqat
  // so'rovnoma topshirilgan) karta/diagramma umuman chizilmaydi (`model/testBattery.ts`).
  const presentTests = buildPresentTestCodes(latestAssessment?.tests);
  const hasAnyPersonalityTest = presentTests.size > 0;

  async function handleDownloadPdf() {
    if (!latestAssessment) return;
    try {
      await downloadPdfMutation.mutateAsync(latestAssessment.id);
    } catch {
      toast.show({ variant: 'danger', title: t('studentProfile.pdf.error') });
    }
  }

  /**
   * `POST /api/admin/assessments/{id}/rerun-analysis` — `docs/07` 3.3. Domen qo'riqchisi
   * (`Assessment.MarkAnalyzing`) `Completed` dan ham ruxsat beradi, ya'ni BIRINCHI tahlil
   * ham aynan shu endpoint orqali ishga tushadi.
   *
   * @param viaDialog Tasdiq oynasidan chaqirildimi — xato o'sha oynada ko'rsatiladi;
   *   tasdiqsiz (birinchi) tahlilda esa xato toast bilan aytiladi, aks holda bosish
   *   javobsiz qolardi.
   */
  async function runAnalysis(provider: AiProvider | null, viaDialog: boolean) {
    if (!id || !latestAssessment) return;
    try {
      await rerunMutation.mutateAsync({ studentId: id, assessmentId: latestAssessment.id, provider });
      toast.show({ variant: 'success', title: t('studentProfile.rerunDialog.success') });
      setRerunOpen(false);
    } catch {
      if (!viaDialog) {
        toast.show({ variant: 'danger', title: t('studentProfile.rerunDialog.error') });
      }
      // Tasdiq oynasidagi xato `RerunAnalysisDialog`da `error` orqali ko'rsatiladi —
      // dialog ochiq qoladi.
    }
  }

  /**
   * Tasdiq FAQAT mavjud hisobot ustiga yozilganda so'raladi. Birinchi tahlilda
   * yo'qotiladigan narsa yo'q — so'rov darhol ketadi, tugma yuklanish holatiga o'tadi.
   */
  function handleRunAnalysis({ requiresConfirmation }: { requiresConfirmation: boolean }) {
    if (requiresConfirmation) {
      setRerunOpen(true);
      return;
    }
    void runAnalysis(null, false);
  }

  async function handleDelete() {
    if (!id) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.show({ variant: 'success', title: t('studentProfile.deleteDialog.success') });
      navigate(ROUTES.admin.students);
    } catch {
      // Xato `ConfirmDialog`da `error` orqali ko'rsatiladi — dialog ochiq qoladi.
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <StudentProfileHeader
        student={student}
        latestAssessmentStatus={latestSummary?.status ?? null}
        reliabilityFlag={latestSummary?.reliabilityFlag ?? null}
        reliabilityScore={latestSummary?.reliabilityScore ?? null}
        onDownloadPdf={() => void handleDownloadPdf()}
        isDownloadingPdf={downloadPdfMutation.isPending}
        onRerunAnalysis={() =>
          handleRunAnalysis({ requiresConfirmation: hasAiReport(latestAssessment?.aiAnalysis) })
        }
        onDelete={() => setDeleteOpen(true)}
        hasLatestAssessment={Boolean(latestAssessment)}
      />

      {/* sr-only bo'lim sarlavhalari — h1 dan keyin to'g'ridan-to'g'ri Card'larning ichki
          h3'iga (`StudentDiagramsSection`) o'tib ketmasligi uchun (`axe` `heading-order`
          qoidasi: darajalar bittadan ko'p sakramaydi). Vizual dizaynda alohida sarlavha
          ko'rsatilmaydi (docs/11 A-5 maketida yo'q) — faqat ekran o'quvchisi uchun tuzilma.
          Bironta ham metodika sessiyada yo'q bo'lsa (faqat so'rovnoma) BUTUN bo'lim —
          sarlavha ham — chizilmaydi (P52 jonli xato tuzatish, 2026-09-12). */}
      {hasAnyPersonalityTest && (
        <>
          <h2 className="sr-only">{t('studentProfile.summarySectionHeading')}</h2>
          <StudentSummaryCards
            mbti16={results?.MBTI16}
            bigFive={results?.BIG5}
            riasec={results?.RIASEC}
            activityIndex={results?.ACTIVITY?.activityIndex}
            activityLevelText={
              results?.ACTIVITY
                ? t(`students.enums.activityLevel.${results.ACTIVITY.activityLevel}`)
                : null
            }
            presentTests={presentTests}
          />

          <h2 className="sr-only">{t('studentProfile.diagramsSectionHeading')}</h2>
          <StudentDiagramsSection
            mbti16={results?.MBTI16}
            bigFive={results?.BIG5}
            riasec={results?.RIASEC}
            activity={results?.ACTIVITY}
            presentTests={presentTests}
          />
        </>
      )}

      <AiReportSection
        assessmentStatus={latestSummary?.status ?? null}
        aiAnalysis={latestAssessment?.aiAnalysis}
        aiHistory={latestAssessment?.aiHistory}
        reliabilityFlag={latestSummary?.reliabilityFlag ?? null}
        hasAssessment={Boolean(latestAssessment)}
        hasPersonalityBattery={latestAssessment?.hasPersonalityBattery ?? true}
        onRunAnalysis={handleRunAnalysis}
        isStartingAnalysis={rerunMutation.isPending && !rerunOpen}
        pollTimedOut={pollTimedOut}
        hasConfiguredProvider={hasConfiguredProvider}
        onOpenHistoryItem={() => {
          // Tarixdagi eski tahlilni ochish — hozircha faqat ro'yxatda ko'rsatiladi; to'liq
          // ko'rish (eski versiyani body sifatida yuklash) uchun alohida endpoint kerak,
          // docs/07 3.3 da yo'q — PM bilan aniqlanishi kerak bo'lgan bo'shliq (hisobotda bor).
        }}
      />

      {/* Savolma-savol javoblar — OYNA emas, profilning o'z bo'limi (egasining talabi,
          2026-09-03): dialog ichida turgani uchun bu ma'lumot umuman topilmagan edi. */}
      <AnswersSection assessmentId={latestAssessment?.id ?? null} />

      <AssessmentHistoryTable assessments={assessments} />

      <RerunAnalysisDialog
        open={rerunOpen}
        onClose={() => setRerunOpen(false)}
        onConfirm={(provider) => void runAnalysis(provider, true)}
        isSubmitting={rerunMutation.isPending}
        error={rerunMutation.isError ? t('studentProfile.rerunDialog.error') : undefined}
      />

      <ConfirmDialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        onConfirm={() => void handleDelete()}
        title={t('studentProfile.deleteDialog.title')}
        description={t('studentProfile.deleteDialog.description')}
        confirmLabel={t('studentProfile.deleteDialog.confirmCta')}
        isConfirming={deleteMutation.isPending}
        error={deleteMutation.isError ? t('studentProfile.deleteDialog.error') : undefined}
      />
    </div>
  );
}
