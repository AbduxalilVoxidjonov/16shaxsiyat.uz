import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { EmptyState } from '@/shared/ui/EmptyState';
import { buildFunnelSteps } from '../model/funnel';
import type { DashboardFunnel } from '../model/types';
import { VisuallyHidden } from '@/shared/ui/VisuallyHidden';

export interface FunnelSectionProps {
  /**
   * Backend endi bu maydonni har doim qaytaradi (PM tasdig'i 2026-09-02: "funnel/
   * schoolBreakdown — endi ular doim keladi", `schema.d.ts`: `AdminDashboardStatsDto.
   * funnel` majburiy) — shu sabab `| undefined` YO'Q. Tanlangan sana oralig'ida faollik
   * bo'lmasa (barcha bosqichlar `0`) haqiqiy holat sifatida ko'rsatiladi (pastga qarang).
   */
  funnel: DashboardFunnel;
}

/**
 * Voronka — egasining asosiy savoli (vazifa ko'rsatmasi 2-band): "havola ochilgan →
 * ro'yxatdan o'tgan → boshlagan → yakunlagan → tahlil tayyor". Har bosqichda **son
 * birinchi darajali, foiz ikkinchi darajali** (kichikroq, kulrang matn). Rang bitta
 * neytral tusda — docs/11, 1-bo'lim: "Voronka bosqichlari neytral palitrada", bu
 * o'quvchilar haqida hukm emas, o'lchov.
 */
export function FunnelSection({ funnel }: FunnelSectionProps) {
  const { t } = useTranslation();
  const headingId = useId();
  const steps = buildFunnelSteps(funnel);

  // Barcha bosqichlar 0 — tanlangan sana oralig'ida umuman faollik yo'q. Nol bilan to'la
  // bar ro'yxati o'rniga tushunarli bo'sh holat (CLAUDE.md "MAXSUS DIQQAT": "Nol bilan
  // to'la jadval emas").
  const hasActivity = steps.some((step) => step.count > 0);
  if (!hasActivity) {
    return (
      <Card title={t('dashboard.funnel.heading')}>
        <EmptyState
          title={t('dashboard.funnel.emptyTitle')}
          description={t('dashboard.funnel.emptyDescription')}
        />
      </Card>
    );
  }

  const maxCount = Math.max(...steps.map((step) => step.count), 1);

  return (
    <Card title={t('dashboard.funnel.heading')}>
      <p className="mb-4 text-sm text-neutral-500">{t('dashboard.funnel.description')}</p>

      <ol data-testid="funnel-steps" className="flex flex-col gap-4">
        {steps.map((step) => {
          const stepLabel = t(`dashboard.funnel.steps.${step.key}`);
          const barPct = Math.min(100, Math.max(0, (step.count / maxCount) * 100));
          const percentText =
            step.percentOfPrevious === null
              ? t('dashboard.funnel.noPreviousData')
              : t('dashboard.funnel.percentOfPrevious', {
                  value: step.percentOfPrevious.toFixed(0),
                });
          const ariaLabel = t('dashboard.funnel.stepAriaLabel', {
            label: stepLabel,
            count: step.count,
            percent: percentText,
          });

          return (
            <li key={step.key} data-testid={`funnel-step-${step.key}`}>
              <div className="mb-1 flex items-baseline justify-between gap-2">
                <span id={`${headingId}-${step.key}`} className="text-sm font-medium text-neutral-700">
                  {stepLabel}
                </span>
                <span className="flex items-baseline gap-2">
                  <span className="text-lg font-bold text-neutral-900">{step.count}</span>
                  <span className="text-xs text-neutral-500">{percentText}</span>
                </span>
              </div>
              <div
                role="img"
                aria-label={ariaLabel}
                className="h-3 w-full overflow-hidden rounded-full bg-neutral-200"
              >
                <div
                  className="h-full rounded-full bg-neutral-600"
                  style={{ width: `${String(barPct)}%` }}
                />
              </div>
            </li>
          );
        })}
      </ol>

      {/* Ekran o'quvchisi uchun yashirin jadval alternativi — docs/11, 4-bo'lim. */}
      <VisuallyHidden>
        <table data-testid="funnel-table">
          <caption>{t('dashboard.funnel.tableCaption')}</caption>
          <thead>
            <tr>
              <th scope="col">{t('dashboard.funnel.tableStepColumn')}</th>
              <th scope="col">{t('dashboard.funnel.tableCountColumn')}</th>
              <th scope="col">{t('dashboard.funnel.tablePercentColumn')}</th>
            </tr>
          </thead>
          <tbody>
            {steps.map((step) => (
              <tr key={step.key}>
                <td>{t(`dashboard.funnel.steps.${step.key}`)}</td>
                <td>{step.count}</td>
                <td>
                  {step.percentOfPrevious === null
                    ? t('dashboard.funnel.noPreviousData')
                    : t('dashboard.funnel.percentOfPrevious', {
                        value: step.percentOfPrevious.toFixed(0),
                      })}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </VisuallyHidden>
    </Card>
  );
}
