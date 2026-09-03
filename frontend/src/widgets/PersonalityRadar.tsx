import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import {
  PolarAngleAxis,
  PolarGrid,
  PolarRadiusAxis,
  Radar,
  RadarChart,
  ResponsiveContainer,
} from 'recharts';
import { usePrefersReducedMotion } from '@/shared/hooks/usePrefersReducedMotion';
import { classifyBigFiveLevel } from './personalityRadarLevel';
import { VisuallyHidden } from '@/shared/ui/VisuallyHidden';

export interface PersonalityRadarFactor {
  pct: number;
  level: string;
}

export interface PersonalityRadarProps {
  openness: PersonalityRadarFactor;
  conscientiousness: PersonalityRadarFactor;
  extraversion: PersonalityRadarFactor;
  agreeableness: PersonalityRadarFactor;
  /**
   * `100 − N_pct` — backend `BIG5.stabilityPct` (docs/03, 3.2-bo'lim). CLAUDE.md "MAXSUS
   * DIQQAT": neyrotizm o'rniga har doim "Emotsional barqarorlik" ko'rsatiladi, "neyrotizm
   * yuqori" kabi salbiy talqin qilinadigan matn hech qachon chiqmaydi.
   */
  stabilityPct: number;
  className?: string;
}

/**
 * Big Five 5 burchakli radar — docs/11 A-5, P26 2-band. `N` o'rniga "Emotsional barqarorlik"
 * ko'rsatiladi. Rang bitta neytral/asosiy tusda — omillar orasidagi farq faqat shakl va
 * yonidagi raqamda ko'rinadi (docs/11, 1-bo'lim: "ranglar darajani baholamaydi").
 */
export function PersonalityRadar({
  openness,
  conscientiousness,
  extraversion,
  agreeableness,
  stabilityPct,
  className,
}: PersonalityRadarProps) {
  const { t } = useTranslation();
  const titleId = useId();
  const reducedMotion = usePrefersReducedMotion();
  const clampedStability = Math.min(100, Math.max(0, stabilityPct));

  const factors: Array<{ key: string; label: string; pct: number; level: string }> = [
    { key: 'O', label: t('widgets.personalityRadar.factor.O'), pct: openness.pct, level: openness.level },
    {
      key: 'C',
      label: t('widgets.personalityRadar.factor.C'),
      pct: conscientiousness.pct,
      level: conscientiousness.level,
    },
    {
      key: 'E',
      label: t('widgets.personalityRadar.factor.E'),
      pct: extraversion.pct,
      level: extraversion.level,
    },
    {
      key: 'A',
      label: t('widgets.personalityRadar.factor.A'),
      pct: agreeableness.pct,
      level: agreeableness.level,
    },
    {
      key: 'Stability',
      label: t('widgets.personalityRadar.factor.Stability'),
      pct: clampedStability,
      level: classifyBigFiveLevel(clampedStability, t),
    },
  ];

  const chartData = factors.map((factor) => ({ label: factor.label, value: factor.pct }));
  const ariaLabel = t('widgets.personalityRadar.ariaLabel', {
    values: factors.map((factor) => `${factor.label} ${factor.pct.toFixed(1)}%`).join(', '),
  });

  return (
    <div className={className}>
      <h4 id={titleId} className="sr-only">
        {t('widgets.personalityRadar.title')}
      </h4>
      <div role="img" aria-label={ariaLabel} className="h-64 w-full min-w-0">
        <ResponsiveContainer width="100%" height="100%">
          <RadarChart data={chartData} outerRadius="75%">
            <PolarGrid stroke="var(--color-neutral-300)" />
            <PolarAngleAxis
              dataKey="label"
              tick={{ fill: 'var(--color-neutral-600)', fontSize: 12 }}
            />
            <PolarRadiusAxis
              angle={90}
              domain={[0, 100]}
              tick={{ fill: 'var(--color-neutral-400)', fontSize: 10 }}
            />
            <Radar
              name={t('widgets.personalityRadar.title')}
              dataKey="value"
              stroke="var(--color-primary-600)"
              fill="var(--color-primary-400)"
              fillOpacity={0.35}
              isAnimationActive={!reducedMotion}
            />
          </RadarChart>
        </ResponsiveContainer>
      </div>

      {/* Har omil yonidagi raqamli qiymat va daraja matni — docs/11, 4-bo'lim. */}
      <dl
        data-testid="personality-radar-summary"
        className="mt-3 grid grid-cols-2 gap-x-4 gap-y-2 text-sm sm:grid-cols-5"
      >
        {factors.map((factor) => (
          <div key={factor.key} className="flex flex-col">
            <dt className="text-neutral-500">{factor.label}</dt>
            <dd className="font-semibold text-neutral-900">
              {factor.pct.toFixed(1)}%{' '}
              <span className="font-normal text-neutral-500">({factor.level})</span>
            </dd>
          </div>
        ))}
      </dl>

      <VisuallyHidden>
        <table data-testid="personality-radar-table">
          <caption>{ariaLabel}</caption>
          <thead>
            <tr>
              <th scope="col">{t('widgets.personalityRadar.tableFactorColumn')}</th>
              <th scope="col">{t('widgets.personalityRadar.tableValueColumn')}</th>
              <th scope="col">{t('widgets.personalityRadar.tableLevelColumn')}</th>
            </tr>
          </thead>
          <tbody>
            {factors.map((factor) => (
              <tr key={factor.key}>
                <td>{factor.label}</td>
                <td>{factor.pct.toFixed(1)}%</td>
                <td>{factor.level}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </VisuallyHidden>
    </div>
  );
}
