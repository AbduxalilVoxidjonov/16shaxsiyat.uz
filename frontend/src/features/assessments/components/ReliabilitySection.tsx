import { useTranslation } from 'react-i18next';
import { Shield } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { ReliabilityBadge } from '@/widgets/ReliabilityBadge';
import type { ReliabilityFlag } from '../model/types';

export interface ReliabilitySectionProps {
  score: number | null;
  flag: ReliabilityFlag | null;
}

/**
 * Ishonchlilik indeksi (`Assessment.ReliabilityScore`, 0–100) va bayrog'i — `docs/03`
 * 6-bo'lim. Bu — ma'lumot SIFATI signali: past ball natijaning "yomonligini" emas,
 * javoblarga ishonib bo'lmasligini bildiradi, shu sabab yonida qisqa izoh doim turadi.
 *
 * Ball hisoblanmagan bo'lsa `0` KO'RSATILMAYDI (aks holda "eng yomon ishonchlilik" deb
 * o'qilardi) — `docs/06` qarorlar jurnali, 2026-09-02.
 */
export function ReliabilitySection({ score, flag }: ReliabilitySectionProps) {
  const { t } = useTranslation();
  const hasScore = typeof score === 'number' && Number.isFinite(score);

  return (
    <Card title={t('assessmentDetail.reliability.heading')}>
      {hasScore || flag ? (
        <div className="flex flex-col gap-3">
          <div className="flex flex-wrap items-baseline gap-3">
            {hasScore ? (
              <span className="text-3xl font-semibold text-neutral-900">
                {score.toFixed(1)}
                <span className="ml-1 text-base font-normal text-neutral-500">
                  {t('assessmentDetail.reliability.outOf')}
                </span>
              </span>
            ) : (
              <span className="text-sm text-neutral-500">
                {t('assessmentDetail.reliability.scoreUnknown')}
              </span>
            )}
            {flag && <ReliabilityBadge flag={flag} />}
          </div>
          <p className="text-sm text-neutral-600">
            {t('assessmentDetail.reliability.explanation')}
          </p>
          {flag && (
            <p className="text-sm text-neutral-700">
              {t(`assessmentDetail.reliability.flagAdvice.${flag}`)}
            </p>
          )}
        </div>
      ) : (
        <div className="flex flex-col items-start gap-2">
          <span className="text-neutral-400" aria-hidden="true">
            <Shield size={24} />
          </span>
          <p className="text-sm font-medium text-neutral-900">
            {t('assessmentDetail.reliability.notCalculatedTitle')}
          </p>
          <p className="text-sm text-neutral-600">
            {t('assessmentDetail.reliability.notCalculatedDescription')}
          </p>
        </div>
      )}
    </Card>
  );
}
