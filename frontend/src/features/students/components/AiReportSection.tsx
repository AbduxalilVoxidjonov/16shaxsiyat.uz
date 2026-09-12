import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, ChevronDown, RefreshCw, Sparkles } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { Button } from '@/shared/ui/Button';
import { AiReportView } from '@/widgets/AiReportView';
import { formatDate } from '@/shared/lib/formatDate';
import { resolveAiAnalysisViewState } from '@/shared/lib/aiAnalysisState';
import type { AssessmentStatus, ReliabilityFlag } from '../model/enums';
import type { AiAnalysisDto, AiAnalysisHistoryItemDto } from '../model/profileTypes';

export interface AiReportSectionProps {
  assessmentStatus: AssessmentStatus | null;
  aiAnalysis: AiAnalysisDto | null | undefined;
  aiHistory: AiAnalysisHistoryItemDto[] | null | undefined;
  reliabilityFlag: ReliabilityFlag | null;
  /** O'quvchida umuman sessiya bormi. */
  hasAssessment: boolean;
  /**
   * Sessiyada AI tahlilga tayanadigan ballanadigan shaxsiyat metodikasi bormi
   * (`latestAssessment.hasPersonalityBattery`, P52 jonli xato tuzatish, 2026-09-12).
   * `false` — faqat so'rovnoma topshirilgan: "Tahlilni ishga tushirish" chaqiruvi
   * ko'rsatilmaydi, o'rniga tushuntirish matni chiqadi.
   */
  hasPersonalityBattery: boolean;
  /**
   * Tahlilni ishga tushirish. `requiresConfirmation` — mavjud hisobot ustiga yozilyaptimi:
   * `true` bo'lsa sahifa avval `RerunAnalysisDialog` ni ochadi, `false` bo'lsa darhol
   * so'rov yuboradi (birinchi tahlilda yo'qotiladigan narsa yo'q).
   */
  onRunAnalysis: (options: { requiresConfirmation: boolean }) => void;
  /** Tugmani yuklanish holatiga o'tkazadi (tasdiqsiz birinchi tahlil uchun). */
  isStartingAnalysis: boolean;
  /** Natijani kutish cheklovi tugadi — sahifani yangilash kerak. */
  pollTimedOut: boolean;
  /** `null` — noma'lum; `false` — birorta faol provayder sozlanmagan. */
  hasConfiguredProvider: boolean | null;
  onOpenHistoryItem: (id: string) => void;
}

/**
 * AI hisobot bloki — docs/11 A-5, P25 4/5-band.
 *
 * Qaysi holatda nima ko'rinishi `shared/lib/aiAnalysisState.ts` da hal qilinadi (sessiya
 * detali sahifasi bilan BITTA mantiq). Shu faylning vazifasi — o'sha qarorni ekranga
 * chiqarish:
 *
 * | Holat | Ekranda |
 * |---|---|
 * | `Completed`, hisobot yo'q | "AI tahlil qilish" tugmasi + tushuntirish |
 * | `Analyzed`/`Completed`, hisobot bor | hisobot + "Qayta tahlil qilish" |
 * | `Analyzing`, hisobot bor | **eski hisobot JOYIDA** + "yangi tahlil tayyorlanmoqda" banneri |
 * | `Analyzing`, hisobot yo'q | skelet (birinchi tahlil) |
 * | `AnalysisFailed` | qizil karta: sabab + "Qayta urinish" |
 * | `Draft`/`InProgress`/`Abandoned` | tugma YO'Q, o'rniga sabab |
 *
 * `isFallbackReport` → shablon hisobot ogohlantirishi; `isModerated` → moderatsiya
 * ogohlantirishi; `Unreliable` → sariq banner. `AiAnalysisDto` `AiReportSections`ni
 * kengaytiradi, shu sabab to'g'ridan-to'g'ri `AiReportView`ga uzatiladi — ikkilanma
 * mapping yo'q.
 */
export function AiReportSection({
  assessmentStatus,
  aiAnalysis,
  aiHistory,
  reliabilityFlag,
  hasAssessment,
  hasPersonalityBattery,
  onRunAnalysis,
  isStartingAnalysis,
  pollTimedOut,
  hasConfiguredProvider,
  onOpenHistoryItem,
}: AiReportSectionProps) {
  const { t } = useTranslation();
  const [historyOpen, setHistoryOpen] = useState(false);
  const history = aiHistory ?? [];

  const view = resolveAiAnalysisViewState({
    status: assessmentStatus,
    analysis: aiAnalysis,
    hasAssessment,
    hasPersonalityBattery,
  });

  const actionLabel =
    view.action === 'run'
      ? t('studentProfile.ai.runCta')
      : view.action === 'rerun'
        ? t('studentProfile.ai.rerunCta')
        : t('studentProfile.ai.retryCta');

  const runAnalysis = () => {
    onRunAnalysis({ requiresConfirmation: view.requiresConfirmation });
  };

  // Provayder sozlanmagani — tugma bosilishidan OLDIN aytiladigan ogohlantirish. `null`
  // (noma'lum) bo'lsa hech narsa da'vo qilinmaydi.
  const providerWarning = view.action !== null && hasConfiguredProvider === false && (
    <p
      role="alert"
      className="rounded-lg border border-warning-300 bg-warning-50 p-3 text-sm text-warning-800"
    >
      {t('studentProfile.ai.providerMissingWarning')}
    </p>
  );

  return (
    <Card>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <h3 className="flex items-center gap-2 text-base font-semibold text-neutral-900">
          <span aria-hidden="true">🤖</span> {t('studentProfile.ai.heading')}
        </h3>
        <div className="flex items-center gap-3">
          {view.showReport && aiAnalysis && (
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

      <div className="flex flex-col gap-3">
        {view.showAnalyzingBanner && (
          // `Analyzing` — eski hisobot ustidagi banner. Mavjud tahlil PASTDA joyida qoladi:
          // qayta tahlil bosilgan zahoti admin ekrandagi hisobotni YO'QOTMASLIGI kerak.
          <div
            role="status"
            className="rounded-xl border border-primary-200 bg-primary-50 p-3 text-sm text-primary-800"
          >
            <p className="font-medium">
              {view.showReport
                ? t('studentProfile.ai.analyzingWithReportTitle')
                : t('studentProfile.ai.analyzingTitle')}
            </p>
            <p className="mt-1">
              {view.showReport
                ? t('studentProfile.ai.analyzingWithReportDescription')
                : t('studentProfile.ai.analyzingDescription')}
            </p>
          </div>
        )}

        {pollTimedOut && (
          <p
            role="alert"
            className="rounded-xl border border-warning-300 bg-warning-50 p-3 text-sm text-warning-800"
          >
            {t('studentProfile.ai.pollTimeout')}
          </p>
        )}

        {view.showFailure && (
          <div role="alert" className="rounded-xl border border-danger-300 bg-danger-50 p-4">
            <p className="font-medium text-danger-800">{t('studentProfile.ai.failedTitle')}</p>
            <p className="mt-1 text-sm text-danger-700">
              {aiAnalysis?.errorMessage ?? t('studentProfile.ai.failedDescription')}
            </p>
            <Button
              variant="danger"
              size="sm"
              className="mt-3"
              isLoading={isStartingAnalysis}
              onClick={runAnalysis}
            >
              {t('studentProfile.ai.retryCta')}
            </Button>
          </div>
        )}

        {view.blockedReason !== null && (
          // Tugmani ko'rsatib turib `409` ga urib yuborish yomon UX — sabab OLDINDAN.
          <p className="text-sm text-neutral-500">
            {view.blockedReason === 'noAssessment'
              ? t('studentProfile.ai.noAssessment')
              : view.blockedReason === 'noPersonalityBattery'
                ? t('studentProfile.ai.noPersonalityBattery')
                : t('studentProfile.ai.blockedNotCompleted', {
                    status:
                      assessmentStatus === null
                        ? ''
                        : t(`students.enums.status.${assessmentStatus}`),
                  })}
          </p>
        )}

        {view.action !== null && !view.showFailure && (
          <div className="flex flex-col gap-2">
            {providerWarning}
            <div className="flex flex-wrap items-center gap-3">
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
              <span className="text-sm text-neutral-500">
                {view.action === 'run'
                  ? t('studentProfile.ai.runHint')
                  : t('studentProfile.ai.rerunHint')}
              </span>
            </div>
          </div>
        )}

        {view.showFailure && providerWarning}

        {reliabilityFlag === 'Unreliable' && view.showReport && (
          <p
            role="alert"
            className="rounded-lg border border-warning-300 bg-warning-50 p-3 text-sm text-warning-800"
          >
            {t('studentProfile.ai.unreliableBanner')}
          </p>
        )}

        {view.showReport && aiAnalysis?.isFallbackReport === true && (
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

        {view.showReport && aiAnalysis?.isModerated === true && (
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

        {view.showReport && aiAnalysis && <AiReportView sections={aiAnalysis} />}

        {view.showSkeleton && (
          // Skelet FAQAT hech qachon tahlil qilinmagan sessiyada — mavjud hisobot ustiga
          // hech qachon chiqmaydi.
          <div role="status" className="flex flex-col gap-3">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-24 w-full" />
          </div>
        )}

        {!view.showReport && !view.showSkeleton && !view.showFailure && view.blockedReason === null && (
          // "Hech qachon tahlil qilinmagan" — "tahlil yiqilgan" dan ANIQ farq qiladi.
          <p className="text-sm text-neutral-500">{t('studentProfile.ai.noAnalysisYet')}</p>
        )}
      </div>
    </Card>
  );
}
