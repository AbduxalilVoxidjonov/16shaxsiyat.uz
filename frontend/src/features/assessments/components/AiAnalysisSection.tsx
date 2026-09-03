import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Bot, ChevronDown, RefreshCw } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { AiReportView } from '@/widgets/AiReportView';
import { formatDateTime } from '../model/sessionMeta';
import type {
  AssessmentAiAnalysisDto,
  AssessmentAiHistoryItemDto,
  AssessmentStatus,
} from '../model/types';

export interface AiAnalysisSectionProps {
  analysis: AssessmentAiAnalysisDto | null;
  history: AssessmentAiHistoryItemDto[];
  assessmentStatus: AssessmentStatus | null;
  /** Sessiya holati qayta tahlilga ruxsat beradimi (noma'lum bo'lsa `true` — serverni taxmin qilmaymiz). */
  canRerun: boolean;
  onRerun: () => void;
  /** Shu sahifada endigina navbatga qo'yilgan bo'lsa — natija hali yangilanmaganini tushuntiradi. */
  rerunQueued: boolean;
}

/**
 * AI tahlili bloki — `docs/11` A-5/A-6. To'rt holat aniq ajratiladi: hisobot BOR, hali
 * YO'Q, XATO va **avtomatik shablon hisobot** (`isFallbackReport` — `docs/09` 11-bo'lim:
 * matnni AI yozmagan). Oxirgisi jimgina yorliq bilan emas, sabab va keyingi qadam bilan
 * ko'rsatiladi — admin uni shaxsiy tahlil deb o'qimasligi shart.
 */
export function AiAnalysisSection({
  analysis,
  history,
  assessmentStatus,
  canRerun,
  onRerun,
  rerunQueued,
}: AiAnalysisSectionProps) {
  const { t } = useTranslation();
  const [historyOpen, setHistoryOpen] = useState(false);

  const inFlight =
    assessmentStatus === 'Analyzing' ||
    analysis?.status === 'Pending' ||
    analysis?.status === 'Running';
  const failed = analysis?.status === 'Failed' || assessmentStatus === 'AnalysisFailed';

  return (
    <Card>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-base font-semibold text-neutral-900">
          <Bot size={18} aria-hidden="true" /> {t('assessmentDetail.ai.heading')}
        </h2>
        <div className="flex flex-wrap items-center gap-3">
          {analysis && (
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
          <Button variant="outline" size="sm" onClick={onRerun} disabled={!canRerun}>
            <RefreshCw size={14} aria-hidden="true" />
            {t('assessmentDetail.ai.rerunCta')}
          </Button>
        </div>
      </div>

      <p className="mb-3 text-sm text-neutral-600">{t('assessmentDetail.ai.rerunHint')}</p>

      {!canRerun && (
        <p className="mb-3 text-sm text-neutral-500">
          {t('assessmentDetail.ai.rerunDisabledHint')}
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

      {inFlight ? (
        <div role="status" className="flex flex-col gap-3">
          <p className="font-medium text-neutral-900">{t('assessmentDetail.ai.analyzingTitle')}</p>
          <p className="text-sm text-neutral-600">
            {t('assessmentDetail.ai.analyzingDescription')}
          </p>
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      ) : failed ? (
        <div role="alert" className="rounded-xl border border-danger-300 bg-danger-50 p-4">
          <p className="font-medium text-danger-800">{t('assessmentDetail.ai.failedTitle')}</p>
          <p className="mt-1 text-sm text-danger-700">
            {analysis?.errorMessage ?? t('assessmentDetail.ai.failedDescription')}
          </p>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {analysis?.isFallbackReport === true && (
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
          {analysis?.isModerated === true && (
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
          {analysis ? (
            <AiReportView sections={analysis} />
          ) : (
            <p className="text-sm text-neutral-500">{t('assessmentDetail.ai.noAnalysisYet')}</p>
          )}
        </div>
      )}
    </Card>
  );
}
