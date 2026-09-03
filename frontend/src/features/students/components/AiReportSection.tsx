import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, ChevronDown } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { Button } from '@/shared/ui/Button';
import { AiReportView } from '@/widgets/AiReportView';
import { formatDate } from '@/shared/lib/formatDate';
import type { AssessmentStatus, ReliabilityFlag } from '../model/enums';
import type { AiAnalysisDto, AiAnalysisHistoryItemDto } from '../model/profileTypes';

export interface AiReportSectionProps {
  assessmentStatus: AssessmentStatus | null;
  aiAnalysis: AiAnalysisDto | null | undefined;
  aiHistory: AiAnalysisHistoryItemDto[] | null | undefined;
  reliabilityFlag: ReliabilityFlag | null;
  onRerunAnalysis: () => void;
  onOpenHistoryItem: (id: string) => void;
}

/**
 * AI hisobot bloki — docs/11 A-5, P25 4/5-band. Holatlar: `Analyzing` → skeleton
 * + `refetchInterval` (yuklovchi sahifada, bu komponent faqat holatga qarab render qiladi);
 * `AnalysisFailed` → sabab + "Qayta urinish"; `isFallbackReport` → shablon hisobot belgisi;
 * `Unreliable` → sariq banner. `AiAnalysisDto` `AiReportSections`ni kengaytiradi, shu sabab
 * to'g'ridan-to'g'ri `AiReportView`ga uzatiladi — ikkilanma mapping yo'q.
 */
export function AiReportSection({
  assessmentStatus,
  aiAnalysis,
  aiHistory,
  reliabilityFlag,
  onRerunAnalysis,
  onOpenHistoryItem,
}: AiReportSectionProps) {
  const { t } = useTranslation();
  const [historyOpen, setHistoryOpen] = useState(false);
  const history = aiHistory ?? [];

  return (
    <Card>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <h3 className="flex items-center gap-2 text-base font-semibold text-neutral-900">
          <span aria-hidden="true">🤖</span> {t('studentProfile.ai.heading')}
        </h3>
        <div className="flex items-center gap-3">
          {aiAnalysis && (
            <span className="text-sm text-neutral-500">
              {t('studentProfile.ai.providerMeta', {
                provider: aiAnalysis.provider,
                promptVersion: aiAnalysis.promptVersion,
                date: formatDate(aiAnalysis.createdAt),
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
              {t('studentProfile.ai.historyToggle')}
              <ChevronDown size={14} aria-hidden="true" />
            </Button>
          )}
        </div>
      </div>

      {historyOpen && history.length > 0 && (
        <ul className="mb-3 flex flex-col gap-1 rounded-lg border border-neutral-200 p-2">
          {history.map((item) => (
            <li key={item.id}>
              <button
                type="button"
                onClick={() => onOpenHistoryItem(item.id)}
                className="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm text-neutral-700 hover:bg-neutral-100"
              >
                <span>
                  {item.provider} · {formatDate(item.createdAt)}
                  {item.isCurrent && (
                    <span className="ml-2 text-xs text-primary-600">
                      {t('studentProfile.aiHistory.current')}
                    </span>
                  )}
                </span>
                <span className="text-xs text-neutral-500">
                  {t(`studentProfile.aiHistory.status.${item.status}`)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      {assessmentStatus === 'Analyzing' && (
        <div role="status" className="flex flex-col gap-3">
          <p className="font-medium text-neutral-900">{t('studentProfile.ai.analyzingTitle')}</p>
          <p className="text-sm text-neutral-600">{t('studentProfile.ai.analyzingDescription')}</p>
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-5/6" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}

      {assessmentStatus === 'AnalysisFailed' && (
        <div role="alert" className="rounded-xl border border-danger-300 bg-danger-50 p-4">
          <p className="font-medium text-danger-800">{t('studentProfile.ai.failedTitle')}</p>
          <p className="mt-1 text-sm text-danger-700">
            {aiAnalysis?.errorMessage ?? t('studentProfile.ai.failedDescription')}
          </p>
          <Button variant="danger" size="sm" className="mt-3" onClick={onRerunAnalysis}>
            {t('studentProfile.ai.retryCta')}
          </Button>
        </div>
      )}

      {assessmentStatus !== 'Analyzing' && assessmentStatus !== 'AnalysisFailed' && (
        <div className="flex flex-col gap-3">
          {reliabilityFlag === 'Unreliable' && (
            <p role="alert" className="rounded-lg border border-warning-300 bg-warning-50 p-3 text-sm text-warning-800">
              {t('studentProfile.ai.unreliableBanner')}
            </p>
          )}
          {aiAnalysis?.isFallbackReport === true && (
            // `docs/09` 11-bo'lim: barcha provayder yiqilgach yoziladigan SHABLON hisobot.
            // Kichik "badge" yetarli emas — admin buni shaxsiy AI tahlili deb o'qimasligi
            // uchun sabab va keyingi qadam bilan ochiq ogohlantirish (P28 ko'rsatmasi).
            <div
              role="alert"
              className="flex flex-col gap-1 rounded-xl border border-warning-300 bg-warning-50 p-3"
            >
              <p className="flex items-center gap-1.5 text-sm font-semibold text-warning-800">
                <AlertTriangle size={16} aria-hidden="true" />
                {t('studentProfile.ai.fallbackBadge')}
              </p>
              <p className="text-sm text-warning-800">{t('studentProfile.ai.fallbackNotice')}</p>
            </div>
          )}
          {aiAnalysis?.isModerated === true && (
            // `docs/09` 6-bo'lim (3-band): taqiqlangan atama ikkinchi urinishda ham topilgan —
            // javob "moderatsiya qilindi" belgisi bilan saqlangan. Bu matn post-filtrdan TOZA
            // holda o'tmagan, shuning uchun admin uni oddiy tahlil deb o'qimasligi kerak
            // (`CLAUDE.md` 6-qoida: "AI tashxis qo'ymaydi").
            <div
              role="alert"
              className="flex flex-col gap-1 rounded-xl border border-danger-300 bg-danger-50 p-3"
            >
              <p className="flex items-center gap-1.5 text-sm font-semibold text-danger-800">
                <AlertTriangle size={16} aria-hidden="true" />
                {t('studentProfile.ai.moderatedBadge')}
              </p>
              <p className="text-sm text-danger-800">{t('studentProfile.ai.moderatedNotice')}</p>
            </div>
          )}
          {aiAnalysis ? (
            <AiReportView sections={aiAnalysis} />
          ) : (
            <p className="text-sm text-neutral-500">{t('studentProfile.ai.noAnalysisYet')}</p>
          )}
        </div>
      )}
    </Card>
  );
}
