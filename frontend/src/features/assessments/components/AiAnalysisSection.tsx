import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Bot, ChevronDown, RefreshCw, Sparkles } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { AiReportView } from '@/widgets/AiReportView';
import { resolveAiAnalysisViewState } from '@/shared/lib/aiAnalysisState';
import { formatDateTime } from '../model/sessionMeta';
import type {
  AssessmentAiAnalysisDto,
  AssessmentAiHistoryItemDto,
  AssessmentStatus,
} from '../model/types';

export interface AiAnalysisSectionProps {
  analysis: AssessmentAiAnalysisDto | null;
  history: AssessmentAiHistoryItemDto[];
  /** `null` — holat noma'lum: serverning qarori taxmin qilinmaydi, tugma ochiq qoladi. */
  assessmentStatus: AssessmentStatus | null;
  /**
   * Tahlilni ishga tushirish. `requiresConfirmation` — mavjud hisobot ustiga yozilyaptimi:
   * `true` bo'lsa sahifa `RerunAnalysisDialog` ni ochadi, `false` bo'lsa darhol yuboradi.
   */
  onRunAnalysis: (options: { requiresConfirmation: boolean }) => void;
  /** Tugmani yuklanish holatiga o'tkazadi (tasdiqsiz birinchi tahlil uchun). */
  isStartingAnalysis: boolean;
  /** Shu sahifada endigina navbatga qo'yilgan bo'lsa — natija hali yangilanmaganini tushuntiradi. */
  rerunQueued: boolean;
  /** Natijani kutish cheklovi tugadi — sahifani yangilash kerak. */
  pollTimedOut: boolean;
  /** `null` — noma'lum; `false` — birorta faol provayder sozlanmagan. */
  hasConfiguredProvider: boolean | null;
  /**
   * Shu sessiyada ilmiy shaxsiyat batareyasi bormi (`GET /api/admin/assessments/{id}`
   * → `hasPersonalityBattery`). `false` bo'lsa tahlil chaqiruvi KO'RSATILMAYDI: so'rovnoma
   * javoblari AI tahliliga umuman kirmaydi (`CompleteSessionCommandHandler` `Survey`
   * bloklarini chiqarib tashlaydi). Bu — mijoz tomonidagi UX qulayligi (sababni oldindan
   * ko'rsatish); haqiqiy himoya serverda: `RerunAnalysisCommandHandler` shu holatda `409
   * ASSESSMENT_NO_PERSONALITY_BATTERY` qaytaradi (`AssessmentDetailPage` shu kodni alohida
   * xabarga moslaydi, P52 jonli xato tuzatish, 2026-09-14). O'quvchi profilidagi
   * `AiReportSection` bilan bir xil qoida.
   */
  hasPersonalityBattery?: boolean;
}

/**
 * AI tahlili bloki — `docs/11` A-5/A-6.
 *
 * Qaysi holatda nima ko'rinishi `shared/lib/aiAnalysisState.ts` da hal qilinadi — o'quvchi
 * profili (`features/students/components/AiReportSection`) bilan AYNAN bir xil mantiq,
 * ikki nusxa emas.
 *
 * Muhim: `Analyzing` paytida mavjud hisobot ekranda QOLADI (ustida "yangi tahlil
 * tayyorlanmoqda" banneri bilan); skelet faqat hech qachon tahlil qilinmagan sessiyada
 * chiqadi. Shuningdek to'rt holat aniq ajratiladi: hisobot BOR, hali YO'Q, XATO va
 * **avtomatik shablon hisobot** (`isFallbackReport` — `docs/09` 11-bo'lim: matnni AI
 * yozmagan). Oxirgisi jimgina yorliq bilan emas, sabab va keyingi qadam bilan ko'rsatiladi.
 */
export function AiAnalysisSection({
  analysis,
  history,
  assessmentStatus,
  onRunAnalysis,
  isStartingAnalysis,
  rerunQueued,
  pollTimedOut,
  hasConfiguredProvider,
  hasPersonalityBattery = true,
}: AiAnalysisSectionProps) {
  const { t } = useTranslation();
  const [historyOpen, setHistoryOpen] = useState(false);

  const view = resolveAiAnalysisViewState({
    hasPersonalityBattery,
    // `aiAnalysis.status` `Pending`/`Running` bo'lsa sessiya holati hali `Analyzing` ga
    // yozilmagan bo'lishi mumkin — foydalanuvchi uchun bu bir xil holat.
    status:
      analysis?.status === 'Pending' || analysis?.status === 'Running'
        ? 'Analyzing'
        : assessmentStatus,
    analysis,
  });

  // `rerun` va `retry` bir xil matn ishlatadi ("Tahlilni qayta ishga tushirish") — farq
  // tasdiq oynasida: `retry` da odatda yo'qotiladigan hisobot yo'q.
  const actionLabel =
    view.action === 'run'
      ? t('assessmentDetail.ai.runCta')
      : t('assessmentDetail.ai.rerunCta');

  const runAnalysis = () => {
    onRunAnalysis({ requiresConfirmation: view.requiresConfirmation });
  };

  return (
    <Card>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-base font-semibold text-neutral-900">
          <Bot size={18} aria-hidden="true" /> {t('assessmentDetail.ai.heading')}
        </h2>
        <div className="flex flex-wrap items-center gap-3">
          {view.showReport && analysis && (
            <span className="text-sm text-neutral-500">
              {t('assessmentDetail.ai.providerMeta', {
                provider: analysis.provider,
                model: analysis.model,
                promptVersion: analysis.promptVersion,
                date: formatDateTime(analysis.createdAt) ?? t('assessmentDetail.meta.unknown'),
              })}
            </span>
          )}
          {history.length > 0 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setHistoryOpen((open) => !open)}
              aria-expanded={historyOpen}
            >
              {t('assessmentDetail.ai.historyToggle', { count: history.length })}
              <ChevronDown size={14} aria-hidden="true" />
            </Button>
          )}
          {view.action !== null && (
            <Button
              variant={view.action === 'run' ? 'primary' : 'outline'}
              size="sm"
              isLoading={isStartingAnalysis}
              onClick={runAnalysis}
            >
              {view.action === 'run' ? (
                <Sparkles size={14} aria-hidden="true" />
              ) : (
                <RefreshCw size={14} aria-hidden="true" />
              )}
              {actionLabel}
            </Button>
          )}
        </div>
      </div>

      <p className="mb-3 text-sm text-neutral-600">
        {view.action === 'run'
          ? t('assessmentDetail.ai.runHint')
          : t('assessmentDetail.ai.rerunHint')}
      </p>

      {view.blockedReason !== null && (
        // Tugmani ko'rsatib turib `409` ga urib yuborish yomon UX — sabab OLDINDAN.
        <p className="mb-3 text-sm text-neutral-500">
          {view.blockedReason === 'noPersonalityBattery'
            ? t('studentProfile.ai.noPersonalityBattery')
            : t('assessmentDetail.ai.rerunDisabledHint')}
        </p>
      )}

      {view.action !== null && hasConfiguredProvider === false && (
        <p
          role="alert"
          className="mb-3 rounded-lg border border-warning-300 bg-warning-50 p-3 text-sm text-warning-800"
        >
          {t('assessmentDetail.ai.providerMissingWarning')}
        </p>
      )}

      {rerunQueued && (
        <p
          role="status"
          className="mb-3 rounded-lg border border-primary-200 bg-primary-50 p-3 text-sm text-primary-800"
        >
          {t('assessmentDetail.ai.queuedNotice')}
        </p>
      )}

      {pollTimedOut && (
        <p
          role="alert"
          className="mb-3 rounded-lg border border-warning-300 bg-warning-50 p-3 text-sm text-warning-800"
        >
          {t('assessmentDetail.ai.pollTimeout')}
        </p>
      )}

      {historyOpen && history.length > 0 && (
        <ul className="mb-3 flex flex-col gap-1 rounded-lg border border-neutral-200 p-2">
          {history.map((item) => (
            <li
              key={item.id}
              className="flex flex-wrap items-center justify-between gap-2 px-2 py-1.5 text-sm text-neutral-700"
            >
              <span>
                {item.provider} ·{' '}
                {formatDateTime(item.createdAt) ?? t('assessmentDetail.meta.unknown')}
                {item.isCurrent && (
                  <span className="ml-2 text-xs text-primary-600">
                    {t('assessmentDetail.ai.historyCurrent')}
                  </span>
                )}
              </span>
              <span className="text-xs text-neutral-500">
                {t(`assessmentDetail.enums.aiStatus.${item.status}`)}
              </span>
            </li>
          ))}
        </ul>
      )}

      <div className="flex flex-col gap-3">
        {view.showAnalyzingBanner && (
          // Mavjud hisobot PASTDA joyida qoladi — qayta tahlil bosilgan zahoti admin
          // ekrandagi hisobotni YO'QOTMAYDI.
          <div
            role="status"
            className="rounded-xl border border-primary-200 bg-primary-50 p-3 text-sm text-primary-800"
          >
            <p className="font-medium">
              {view.showReport
                ? t('assessmentDetail.ai.analyzingWithReportTitle')
                : t('assessmentDetail.ai.analyzingTitle')}
            </p>
            <p className="mt-1">
              {view.showReport
                ? t('assessmentDetail.ai.analyzingWithReportDescription')
                : t('assessmentDetail.ai.analyzingDescription')}
            </p>
          </div>
        )}

        {view.showFailure && (
          <div role="alert" className="rounded-xl border border-danger-300 bg-danger-50 p-4">
            <p className="font-medium text-danger-800">{t('assessmentDetail.ai.failedTitle')}</p>
            <p className="mt-1 text-sm text-danger-700">
              {analysis?.errorMessage ?? t('assessmentDetail.ai.failedDescription')}
            </p>
          </div>
        )}

        {view.showReport && analysis?.isFallbackReport === true && (
          <div
            role="alert"
            className="flex flex-col gap-1 rounded-xl border border-warning-300 bg-warning-50 p-3"
          >
            <p className="flex items-center gap-1.5 text-sm font-semibold text-warning-800">
              <AlertTriangle size={16} aria-hidden="true" />
              {t('assessmentDetail.ai.fallbackBadge')}
            </p>
            <p className="text-sm text-warning-800">{t('assessmentDetail.ai.fallbackNotice')}</p>
          </div>
        )}

        {view.showReport && analysis?.isModerated === true && (
          <div
            role="alert"
            className="flex flex-col gap-1 rounded-xl border border-danger-300 bg-danger-50 p-3"
          >
            <p className="flex items-center gap-1.5 text-sm font-semibold text-danger-800">
              <AlertTriangle size={16} aria-hidden="true" />
              {t('assessmentDetail.ai.moderatedBadge')}
            </p>
            <p className="text-sm text-danger-800">{t('assessmentDetail.ai.moderatedNotice')}</p>
          </div>
        )}

        {view.showReport && analysis && <AiReportView sections={analysis} />}

        {view.showSkeleton && (
          // Skelet FAQAT hech qachon tahlil qilinmagan sessiyada.
          <div role="status" className="flex flex-col gap-3">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-24 w-full" />
          </div>
        )}

        {!view.showReport && !view.showSkeleton && !view.showFailure && (
          // "Hech qachon tahlil qilinmagan" — "tahlil yiqilgan" dan ANIQ farq qiladi.
          <p className="text-sm text-neutral-500">{t('assessmentDetail.ai.noAnalysisYet')}</p>
        )}
      </div>
    </Card>
  );
}
